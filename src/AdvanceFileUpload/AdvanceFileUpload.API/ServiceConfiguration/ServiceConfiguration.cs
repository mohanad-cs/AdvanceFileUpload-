using System.Globalization;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;
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
using AdvanceFileUpload.Integration;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AdvanceFileUpload.API
{
    public static class ServiceConfiguration
    {

        ///<summary>
        /// Configures the application services by setting up core services and rate limiting.
        /// </summary>
        /// <param name="services">The service collection to which services are added.</param>
        /// <param name="configuration">The application configuration containing settings.</param>
        public static void ConfigureApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            ConfigureCoreServices(services, configuration);
            ConfigureRateLimiting(services, configuration);
        }

        /// <summary>
        /// Configures the core services required for the application.
        /// </summary>
        /// <param name="services">The service collection to which services are added.</param>
        /// <param name="configuration">The application configuration containing settings.</param>
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
            services.AddScoped<IIntegrationEventPublisher, RabbitMQEventPublisher>();
            services.AddTransient<IPeriodicTimer, DailyPeriodicTimer>();
            services.AddScoped<SessionsStatusCheckerService>();
            services.Configure<HostOptions>(options =>
            {
                options.ServicesStartConcurrently = true;
            });

            services.AddHostedService<SessionStatusCheckerWorker>();

            services.AddMediatR(op =>
            {
                op.RegisterServicesFromAssemblies(typeof(UploadManger).Assembly);
            });
            services.AddHealthChecks().AddCheck("APIHealth", () => HealthCheckResult.Healthy("A healthy result."));
        }

        /// <summary>
        /// Ensures that the database schema is up-to-date by applying any pending migrations.
        /// This method should only be used in development environments.
        /// </summary>
        /// <param name="app">The application builder used to configure the app's request pipeline.</param>
        public static void EnsureDbMigration(this IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<ApploicationDbContext>();
                context?.Database.Migrate();
            }
        }
        /// <summary>
        /// Configures rate limiting for the application based on API key settings.
        /// </summary>
        /// <param name="services">The service collection to which services are added.</param>
        /// <param name="configuration">The application configuration containing settings.</param>
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

        /// <summary>
        /// Creates a rate limiter partition for a specific key with a defined request limit per minute.
        /// </summary>
        /// <param name="partitionKey">The key used to identify the rate limiter partition.</param>
        /// <param name="requestsPerMinute">The maximum number of requests allowed per minute.</param>
        /// <returns>A rate limiter partition configured with the specified settings.</returns>
        private static RateLimitPartition<string> CreateRateLimiter(string partitionKey, int requestsPerMinute)
        {
            return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = requestsPerMinute,
                Window = TimeSpan.FromMinutes(1),
                AutoReplenishment = true,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 4,
                SegmentsPerWindow = 4,
            });
        }
        /// <summary>
        /// Configures the upload server using Kestrel.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="webHost"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static void ConfigureUploadServer(this IHostApplicationBuilder builder, IWebHostBuilder webHost)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (webHost is null)
            {
                throw new ArgumentNullException(nameof(webHost));
            }
            builder.Configuration.GetSection("ASPNETCORE_URLS").Value = string.Empty;
            builder.Configuration.GetSection("URLS").Value = string.Empty;
            builder.Services.Configure<KestrelConfiguration>(builder.Configuration.GetSection(KestrelConfiguration.SectionName));
            KestrelConfiguration? serverConfiguration = (KestrelConfiguration?)builder.Configuration.GetRequiredSection(KestrelConfiguration.SectionName).Get<KestrelConfiguration>();
            if (serverConfiguration is null)
            {
                throw new InvalidOperationException("Kestrel Configuration have not been Configured");

            }
            ValidateConfiguration(serverConfiguration);
            // ==============================================
            // 2. Configure Kestrel Server
            // ==============================================
            webHost.UseKestrel(serverOptions =>
            {
                // ----------------------------------
                // Global Server Limits
                // ----------------------------------
                var limits = serverConfiguration.Limits;
                serverOptions.Limits.KeepAliveTimeout = limits.KeepAliveTimeout;
                serverOptions.Limits.RequestHeadersTimeout = limits.RequestHeadersTimeout;
                serverOptions.Limits.MaxConcurrentConnections = limits.MaxConcurrentConnections;
                serverOptions.Limits.MaxConcurrentUpgradedConnections = limits.MaxConcurrentUpgradedConnections;
                serverOptions.Limits.MaxRequestBodySize = limits.MaxRequestBodySize;
                serverOptions.Limits.MinRequestBodyDataRate = limits.MinRequestBodyDataRate.HasValue && limits.MinRequestBodyDataRatePeriod.HasValue
                    ? new MinDataRate(bytesPerSecond: limits.MinRequestBodyDataRate.Value, gracePeriod: limits.MinRequestBodyDataRatePeriod.Value)
                    : null;
                serverOptions.Limits.MinResponseDataRate = limits.MinResponseDataRate.HasValue && limits.MinResponseDataRatePeriod.HasValue
                    ? new MinDataRate(bytesPerSecond: limits.MinResponseDataRate.Value, gracePeriod: limits.MinResponseDataRatePeriod.Value)
                    : null;
                serverOptions.Limits.MaxRequestLineSize = limits.MaxRequestLineSize;
                serverOptions.Limits.MaxRequestBufferSize = limits.MaxRequestBufferSize;
                serverOptions.Limits.MaxResponseBufferSize = limits.MaxResponseBufferSize;
                serverOptions.Limits.MaxRequestHeadersTotalSize = limits.MaxRequestHeadersTotalSize;
                serverOptions.Limits.MaxRequestHeaderCount = limits.MaxRequestHeaderCount;
                serverOptions.AllowSynchronousIO = limits.AllowSynchronousIO;

                // ----------------------------------
                // HTTP Protocol Configuration
                // ----------------------------------
                // HTTP/2 Settings
                serverOptions.Limits.Http2.MaxStreamsPerConnection = serverConfiguration.Http2.MaxStreamsPerConnection;
                serverOptions.Limits.Http2.HeaderTableSize = serverConfiguration.Http2.HeaderTableSize;
                serverOptions.Limits.Http2.MaxFrameSize = serverConfiguration.Http2.MaxFrameSize;

                // HTTP/3 Support
                // serverOptions.EnableAltSvc = serverConfiguration.Kestrel.Http3.Enable;

                // ----------------------------------
                // Global HTTPS Defaults
                // ----------------------------------
                serverOptions.ConfigureHttpsDefaults(httpsOptions =>
                {
                    httpsOptions.SslProtocols = GetSslProtocols(serverConfiguration.Https.AllowedProtocols);
                    httpsOptions.ClientCertificateMode = Enum.Parse<ClientCertificateMode>(serverConfiguration.Https.ClientCertificateMode);
                    httpsOptions.CheckCertificateRevocation = serverConfiguration.Https.CheckCertificateRevocation;
                   
                });
                // ----------------------------------
                // Endpoint Configuration
                // ----------------------------------
                foreach (var endpoint in serverConfiguration.Endpoints)
                {
                    var protocols = GetHttpProtocols(endpoint.Value.Protocols);
                    var port = endpoint.Value.Port;
                    if (port <= 0)
                    {
                        throw new InvalidOperationException("Endpoint port can not be less or equal to zero");
                    }
                    if (endpoint.Value.Ip.ToLower().Equals("localhost"))
                    {
                        serverOptions.ListenLocalhost(port, (listenOptions) =>
                        {
                            ConfigureListenOptions(listenOptions, endpoint.Value);
                        });
                        continue;
                    }
                    if (endpoint.Value.Ip.Equals("0.0.0.0"))
                    {
                        serverOptions.ListenAnyIP(endpoint.Value.Port, (listenOptions) =>
                        {
                            ConfigureListenOptions(listenOptions, endpoint.Value);
                        });
                    }
                    else
                    {
                        if (!IPAddress.TryParse(endpoint.Value.Ip, out var iPAddress))
                        {
                            throw new InvalidOperationException($"Invalid IP address: {endpoint.Value.Ip}");
                        }
                        serverOptions.Listen(iPAddress, port, listenOptions =>
                        {
                            ConfigureListenOptions(listenOptions, endpoint.Value);

                        });
                    }

                }
            });
        }
        /// <summary>
        /// Configure the listen options for a specific endpoint.
        /// </summary>
        /// <param name="listenOptions"></param>
        /// <param name="endpoint"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private static void ConfigureListenOptions(ListenOptions listenOptions, EndpointSettings endpoint)
        {
            listenOptions.Protocols = GetHttpProtocols(endpoint.Protocols);
            if (endpoint.Https)
            {
               
                listenOptions.UseHttps(httpsOptions =>
                {
                  
                    // Certificate Loading Logic
                    if (!string.IsNullOrWhiteSpace(endpoint.Certificate.Path))
                    {
                        // Load from file
                        //var certPassword = builder.Configuration.GetValue<string>("CERT_PASSWORD");
                        httpsOptions.ServerCertificate = new X509Certificate2(
                            fileName: endpoint.Certificate.Path,
                            password: endpoint.Certificate.Password
                        );
                    }
                    else if (!string.IsNullOrEmpty(endpoint.Certificate.Subject))
                    {
                        // Load from certificate store
                        using var store = new X509Store(
                            endpoint.Certificate.Store,
                            Enum.Parse<StoreLocation>(endpoint.Certificate.Location)
                        );
                        store.Open(OpenFlags.ReadOnly);
                        var certs = store.Certificates.Where(x => x.Subject.Equals(endpoint.Certificate.Subject)).ToArray();
                        httpsOptions.ServerCertificate = certs.Length > 0
                            ? certs[0]
                            : throw new InvalidOperationException($"Certificate not found: {endpoint.Certificate.Subject}");
                    }

                    // Endpoint-specific HTTPS settings
                    httpsOptions.SslProtocols = GetSslProtocols(endpoint.SslProtocols);
                    httpsOptions.ClientCertificateMode = Enum.Parse<ClientCertificateMode>(endpoint.ClientCertificateMode);
                });
            }
        }
        /// <summary>
        /// Validate critical configuration values
        /// </summary>
        private static void ValidateConfiguration(KestrelConfiguration config)
        {
            if (config?.Endpoints == null || !config.Endpoints.Any())
            {
                throw new InvalidOperationException("Kestrel endpoints configuration is missing");
            }

            if (config.Limits == null)
            {
                throw new InvalidOperationException("Kestrel limits configuration is missing");
            }
            foreach (var endpoint in config.Endpoints)
            {
                if (endpoint.Value.Https)
                {
                    if (string.IsNullOrEmpty(endpoint.Value.Certificate.Path) &&
                        string.IsNullOrEmpty(endpoint.Value.Certificate.Subject))
                    {
                        throw new InvalidOperationException(
                            $"HTTPS endpoint '{endpoint.Key}' requires certificate configuration");
                    }
                }
            }
            // validate the serverConfiguration.Kestrel.Limits;
            if (config.Limits.MaxConcurrentConnections <= 0)
            {
                throw new InvalidOperationException("MaxConcurrentConnections must be greater than 0");
            }
            if (config.Limits.MaxConcurrentUpgradedConnections <= 0)
            {
                throw new InvalidOperationException("MaxConcurrentUpgradedConnections must be greater than 0");
            }
            if (config.Limits.MaxRequestBodySize <= 0)
            {
                throw new InvalidOperationException("MaxRequestBodySize must be greater than 0");
            }
            if (config.Limits.MaxRequestLineSize <= 0)
            {
                throw new InvalidOperationException("MaxRequestLineSize must be greater than 0");
            }
            if (config.Limits.MaxRequestBufferSize <= 0)
            {
                throw new InvalidOperationException("MaxRequestBufferSize must be greater than 0");
            }
            if (config.Limits.MaxResponseBufferSize <= 0)
            {
                throw new InvalidOperationException("MaxResponseBufferSize must be greater than 0");
            }
            if (config.Limits.MaxRequestHeadersTotalSize <= 0)
            {
                throw new InvalidOperationException("MaxRequestHeadersTotalSize must be greater than 0");
            }
            if (config.Limits.MaxRequestHeaderCount <= 0)
            {
                throw new InvalidOperationException("MaxRequestHeaderCount must be greater than 0");
            }
            if (config.Limits.MinRequestBodyDataRate.HasValue && config.Limits.MinRequestBodyDataRate.Value <= 0)
            {
                throw new InvalidOperationException("MinRequestBodyDataRate must be greater than 0");
            }
            if (config.Limits.MinResponseDataRate.HasValue && config.Limits.MinResponseDataRate.Value <= 0)
            {
                throw new InvalidOperationException("MinResponseDataRate must be greater than 0");
            }

        }

        /// <summary>
        /// Convert string list to SslProtocols enum
        /// </summary>
        private static SslProtocols GetSslProtocols(List<string> protocols)
        {
            var result = SslProtocols.None;
            foreach (var protocol in protocols)
            {
                result |= Enum.Parse<SslProtocols>(protocol);
            }
            return result;
        }

        /// <summary>
        /// Convert string list to HttpProtocols enum
        /// </summary>
        private static HttpProtocols GetHttpProtocols(List<string> protocols)
        {
            var result = HttpProtocols.None;
            foreach (var protocol in protocols)
            {
                result |= Enum.Parse<HttpProtocols>(protocol);
            }
            return result;
        }

    }
}
