using AdvanceFileUpload.Domain;
using AdvanceFileUpload.Domain.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AdvanceFileUpload.Data
{
    /// <summary>
    /// Represents a repository for managing file upload session entities.
    /// </summary>
    public class FileUploadSessionRepository : IRepository<FileUploadSession>
    {
        private readonly ApploicationDbContext _context;
        private readonly DbSet<FileUploadSession> _dbSet;
        private readonly ILogger<FileUploadSessionRepository> _logger;
        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadSessionRepository"/> class.
        /// </summary>
        /// <param name="context">The Application Db Context</param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public FileUploadSessionRepository(ApploicationDbContext context, ILogger<FileUploadSessionRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbSet = _context.Set<FileUploadSession>();

        }
        /// <inheritdoc />
        public async Task<FileUploadSession> AddAsync(FileUploadSession entity, CancellationToken cancellationToken = default)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var result = await _dbSet.AddAsync(entity, cancellationToken);
            return result.Entity;
        }
        /// <inheritdoc />

        public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().AnyAsync(cancellationToken);
        }
        /// <inheritdoc />

        public async Task<bool> AnyAsync(Expression<Func<FileUploadSession, bool>> predicate, CancellationToken cancellationToken = default)
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return await _dbSet.AsNoTracking().AnyAsync(predicate, cancellationToken);
        }
        /// <inheritdoc />

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().CountAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<int> CountAsync(Expression<Func<FileUploadSession, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().Where(predicate).CountAsync(cancellationToken);
        }


        /// <inheritdoc />

        public async Task<IEnumerable<FileUploadSession>> FindAsync(Expression<Func<FileUploadSession, bool>> predicate, CancellationToken cancellationToken = default)
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
        }
        /// <inheritdoc />

        public async Task<IEnumerable<FileUploadSession>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
        }
        /// <inheritdoc />
        public async Task<FileUploadSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }
        /// <inheritdoc />
        public async Task<bool> RemoveAsync(FileUploadSession entity, CancellationToken cancellationToken = default)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var existingEntity = await _dbSet.FirstOrDefaultAsync(e => e.Id == entity.Id, cancellationToken);
            if (existingEntity is null)
            {
                throw new InvalidOperationException($"Entity with id {entity.Id} not found.");
            }

            _dbSet.Remove(existingEntity);
            return true;
        }
        /// <inheritdoc />

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        /// <inheritdoc />

        public async Task<FileUploadSession> UpdateAsync(FileUploadSession entity, CancellationToken cancellationToken = default)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var existingEntity = await _dbSet.FirstOrDefaultAsync(e => e.Id == entity.Id, cancellationToken);
            if (existingEntity is null)
            {
                throw new InvalidOperationException($"Entity with id {entity.Id} not found.");
            }

            _dbSet.Entry(existingEntity).CurrentValues.SetValues(entity);
            return existingEntity;
        }
    }
}
