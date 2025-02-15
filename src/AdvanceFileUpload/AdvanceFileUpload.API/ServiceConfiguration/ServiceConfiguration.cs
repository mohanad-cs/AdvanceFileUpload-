using AdvanceFileUpload.Application;
using AdvanceFileUpload.Application.Settings;
using AdvanceFileUpload.Application.Validators;
using AdvanceFileUpload.Data;

namespace AdvanceFileUpload.API.ServiceConfiguration
{
    public static class ServiceConfiguration
    {
        public static void ConfigureApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigureDataServices(configuration.GetConnectionString("DefaultConnection"));
            services.Configure<IUploadSetting>(configuration.GetSection(UploadSetting.SectionName));
            services.AddSingleton<IChunkValidator, ChunkValidator>();
            services.AddSingleton<IFileValidator, FileValidator>();
            services.AddScoped<IUploadManger, UploadManger>();

        }
    }
}
