using AdvanceFileUpload.API.Security;
using AdvanceFileUpload.Application;
using AdvanceFileUpload.Application.Compression;
using AdvanceFileUpload.Application.EventHandling;
using AdvanceFileUpload.Application.FileProcessing;
using AdvanceFileUpload.Application.Hubs;
using AdvanceFileUpload.Application.Settings;
using AdvanceFileUpload.Application.Validators;
using AdvanceFileUpload.Data;
using AdvanceFileUpload.Domain.Core;
using AdvanceFileUpload.Integration.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Threading.RateLimiting;

namespace AdvanceFileUpload.API
{
    public static class ServiceConfiguration
    {
        /// <summary>
        /// Configures the application services.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configuration">The application configuration.</param>
        public static void ConfigureApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            ConfigureCoreServices(services, configuration);
            ConfigureRateLimiting(services, configuration);
        }

        private static void ConfigureCoreServices(IServiceCollection services, IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("SessionStorage");
            services.ConfigureDataServices(connectionString);
            services.Configure<UploadSetting>(configuration.GetSection(UploadSetting.SectionName));
            services.Configure<RabbitMQOptions>(configuration.GetSection(RabbitMQOptions.SectionName));
            services.Configure<ApiKeyOptions>(configuration.GetSection(ApiKeyOptions.SectionName));

            services.AddSingleton<IChunkValidator, ChunkValidator>();
            services.AddSingleton<IFileValidator, FileValidator>();
            services.AddSingleton<IFileProcessor, FileProcessor>();
            services.AddSingleton<IFileCompressor, FileCompressor>();
            services.AddScoped<IUploadManger, UploadManger>();
            services.AddScoped<IUploadProcessNotifier, UploadProcessNotifier>();
            services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
            services.AddScoped<IIntegrationEventPublisher, RabbitMQIntegrationEventPublisher>();

            //services.AddScoped<SessionsStatusCheckerService>();
            //services.AddHostedService<SessionStatusCheckerWorker>();

            services.AddMediatR(op =>
            {
                op.RegisterServicesFromAssemblies(typeof(UploadManger).Assembly);
            });

            services.AddHealthChecks().AddCheck("APIHealth", () => HealthCheckResult.Healthy("A healthy result."));
            
        }
        public static void EnsureDbMigration(this IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService <ApploicationDbContext>();
                context.Database.Migrate();
            }
        }
        private static void ConfigureRateLimiting(IServiceCollection services, IConfiguration configuration)
        {
            ApiKeyOptions? apiKeyOptions = configuration.GetSection(ApiKeyOptions.SectionName).Get<ApiKeyOptions>();
            if (apiKeyOptions == null) return;
          
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, cancellationToken) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                    }

                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken);
                };
                if (apiKeyOptions.EnableRateLimiting)
                {
                    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    {
                        string apiKey = context.Request.Headers["X-APIKEY"].ToString() ?? "no-key";
                        var matchedApiKey = apiKeyOptions.APIKeys.FirstOrDefault(x => x.Key == apiKey);

                        return matchedApiKey != null && matchedApiKey.RateLimit?.RequestsPerMinute > 0
                            ? CreateRateLimiter(matchedApiKey.ClientId, matchedApiKey.RateLimit.RequestsPerMinute)
                            : CreateRateLimiter("DefaultRateLimiting", apiKeyOptions.DefaultMaxRequestsPerMinute > 0 ? apiKeyOptions.DefaultMaxRequestsPerMinute : 1000);
                    });
                }
               
               
                
            });
        }

        private static RateLimitPartition<string> CreateRateLimiter(string partitionKey, int requestsPerMinute)
        {
            return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = requestsPerMinute,
                Window = TimeSpan.FromMinutes(1),
                AutoReplenishment = true,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit =4,
                SegmentsPerWindow = 4,
            });
        }
    }
}
