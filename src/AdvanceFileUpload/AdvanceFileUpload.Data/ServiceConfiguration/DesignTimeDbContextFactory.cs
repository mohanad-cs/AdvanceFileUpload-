using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AdvanceFileUpload.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApploicationDbContext>
    {
        public ApploicationDbContext CreateDbContext(string[] args)
        {


            var builder = new DbContextOptionsBuilder<ApploicationDbContext>();
            // var connectionString = configuration.GetConnectionString("SessionStorage");

            builder.UseSqlServer("Server=MOHANAD-OFFICE\\SQLEXPRESS;Database=SessionStorageDb;TrustServerCertificate=true;Trusted_Connection=True;");

            return new ApploicationDbContext(builder.Options);
        }
    }
}
