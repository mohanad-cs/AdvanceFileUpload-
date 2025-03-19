using AdvanceFileUpload.Application;
using AdvanceFileUpload.Application.Compression;
using AdvanceFileUpload.Application.EventHandling;
using AdvanceFileUpload.Application.FileProcessing;
using AdvanceFileUpload.Application.Hubs;
using AdvanceFileUpload.Application.Settings;
using AdvanceFileUpload.Application.Validators;
using AdvanceFileUpload.Data;
using AdvanceFileUpload.Domain.Core;

namespace AdvanceFileUpload.API.ServiceConfiguration
{
    public static class ServiceConfiguration
    {
        public static void ConfigureApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigureDataServices(configuration.GetConnectionString("DefaultConnection"));
            services.Configure<UploadSetting>(configuration.GetSection(UploadSetting.SectionName));
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

        }
    }
}
