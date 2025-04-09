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
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace AdvanceFileUpload.API.ServiceConfiguration
{
    public static class ServiceConfiguration
    {
        public static void ConfigureApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            string? c = configuration.GetConnectionString("SessionStorage");
            services.ConfigureDataServices(c);
            services.Configure<UploadSetting>(configuration.GetSection(UploadSetting.SectionName));
            services.Configure<RabbitMQOptions>(configuration.GetSection(RabbitMQOptions.SectionName));
            services.AddSingleton<IChunkValidator, ChunkValidator>();
            services.AddSingleton<IFileValidator, FileValidator>();
            services.AddSingleton<IFileProcessor, FileProcessor>();
            services.AddSingleton<IFileCompressor, FileCompressor>();
            services.AddScoped<IUploadManger, UploadManger>();
            services.AddScoped<IUploadProcessNotifier, UploadProcessNotifier>();
            services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
            services.AddMediatR((op) =>
            {
                op.RegisterServicesFromAssemblies(typeof(UploadManger).Assembly);

            });

           
            services.AddScoped<IIntegrationEventPublisher, AdvanceFileUpload.Integration.Contracts.RabbitMQIntegrationEventPublisher>();
            services.AddHealthChecks().AddCheck("APIHealth", () => HealthCheckResult.Unhealthy("A healthy result."));
            

        }
    }
}
