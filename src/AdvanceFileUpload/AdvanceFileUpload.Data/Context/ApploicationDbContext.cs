using AdvanceFileUpload.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdvanceFileUpload.Data
{
    public class ApploicationDbContext : DbContext
    {
        public ApploicationDbContext(DbContextOptions<ApploicationDbContext> options) : base(options)
        {
        }
        public DbSet<FileUploadSession> FileUploadSessions { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // file upload session entity builder
            var fileUploadSessionBuilder = modelBuilder.Entity<FileUploadSession>();
            fileUploadSessionBuilder.ToTable("FileUploadSessions").HasKey(f => f.Id).HasName("PK_FileUploadSession");
            fileUploadSessionBuilder
            .HasMany(f => f.ChunkFiles)
            .WithOne()
            .HasForeignKey(c => c.SessionId).HasConstraintName("FK_FileUploadSession_ChunkFiles")
            .OnDelete(DeleteBehavior.Cascade).IsRequired();

            fileUploadSessionBuilder.Property(f => f.FileName).IsRequired().HasMaxLength(200);
            // Windows max path length as per https://docs.microsoft.com/en-us/windows/win32/fileio/naming-a-file
            fileUploadSessionBuilder.Property(f => f.SavingDirectory).IsRequired().HasMaxLength(256);
            fileUploadSessionBuilder.Property(f => f.FileExtension).IsRequired().HasMaxLength(10);
            fileUploadSessionBuilder.Property(f => f.FileSize).IsRequired();
            fileUploadSessionBuilder.Property(f => f.CompressedFileSize).IsRequired(false);
            fileUploadSessionBuilder.Property(f => f.CompressionAlgorithm).IsRequired(false);
            fileUploadSessionBuilder.Property(f => f.CompressionLevel).IsRequired(false);
            fileUploadSessionBuilder.Property(f => f.CurrentHubConnectionId).IsRequired(false);
            fileUploadSessionBuilder.Property(f => f.MaxChunkSize).IsRequired();
            fileUploadSessionBuilder.Property(f => f.Status).IsRequired();
            fileUploadSessionBuilder.Property(f => f.SessionStartDate).IsRequired();
            fileUploadSessionBuilder.Property(f => f.SessionEndDate).IsRequired(false);
            fileUploadSessionBuilder.Ignore(f => f.TotalChunksToUpload);
            fileUploadSessionBuilder.Ignore(f => f.TotalUploadedChunks);
            fileUploadSessionBuilder.Ignore(f => f.ProgressPercentage);
            fileUploadSessionBuilder.Ignore(f => f.DomainEvents);


            // chunk file entity builder
            var chunkFileBuilder = modelBuilder.Entity<ChunkFile>();
            chunkFileBuilder.ToTable("ChunkFiles").HasKey(c => new { c.SessionId, c.ChunkIndex }).HasName("PK_ChunkFile");
            chunkFileBuilder.Property(c => c.SessionId).IsRequired();
            chunkFileBuilder.Property(c => c.ChunkIndex).IsRequired();
            chunkFileBuilder.Property(c => c.ChunkPath).IsRequired().HasMaxLength(256);
            chunkFileBuilder.Property(c => c.ChunkSize).IsRequired();

        }
    }
}
