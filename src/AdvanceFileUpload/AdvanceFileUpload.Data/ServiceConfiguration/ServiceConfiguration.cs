using AdvanceFileUpload.Domain;
using AdvanceFileUpload.Domain.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace AdvanceFileUpload.Data
{
    /// <summary>
    /// Provides methods to configure data services for the application.
    /// </summary>
    public static class ServiceConfiguration
    {
        /// <summary>
        /// Configures the data services for the application.
        /// </summary>
        /// <param name="services">The service collection to add the services to.</param>
        /// <param name="connectionString">The connection string for the database.</param>
        public static void ConfigureDataServices(this IServiceCollection services, string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or whitespace.", nameof(connectionString));
            }
            services.AddDbContext<ApploicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("AppMemoryDb");
                options.LogTo(Console.WriteLine);

            });
            services.AddScoped<IRepository<FileUploadSession>, FileUploadSessionRepository>();
        }
    }
}
