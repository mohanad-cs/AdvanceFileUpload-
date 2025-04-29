
using AdvanceFileUpload.Domain;
using AdvanceFileUpload.Domain.Core;
using Microsoft.Extensions.Logging;

namespace AdvanceFileUpload.Application
{
    /// <summary>
    /// Service responsible for checking the status of file upload sessions and marking them as failed if they are older than 48 hours and not completed.
    /// </summary>
    public sealed class SessionsStatusCheckerService
    {
        private readonly IRepository<FileUploadSession> _fileUploadSessionRepository;
        private readonly ILogger<SessionsStatusCheckerService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionsStatusCheckerService"/> class.
        /// </summary>
        /// <param name="fileUploadSessionRepository">The repository for managing file upload sessions.</param>
        /// <param name="logger">The logger for logging information and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileUploadSessionRepository"/> or <paramref name="logger"/> is null.</exception>
        public SessionsStatusCheckerService(
             IRepository<FileUploadSession> fileUploadSessionRepository, ILogger<SessionsStatusCheckerService> logger)
        {
            _fileUploadSessionRepository = fileUploadSessionRepository ?? throw new ArgumentNullException(nameof(fileUploadSessionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Checks the status of file upload sessions that are older than 48 hours and not completed.
        /// Marks such sessions as failed and updates them in the repository.
        /// </summary>
        /// <param name="stoppingToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method fetches sessions with statuses <see cref="FileUploadSessionStatus.InProgress"/>, 
        /// <see cref="FileUploadSessionStatus.Paused"/>, or <see cref="FileUploadSessionStatus.PendingToComplete"/> 
        /// that started more than 48 hours ago. It marks them as failed and updates the repository.
        /// </remarks>
        public async Task CheckStatusAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("[SessionsStatusCheckerService] Fetching sessions older than 48 hours That been not Completed.");
                var sessions = await _fileUploadSessionRepository.FindAsync(
                          x => (x.Status == FileUploadSessionStatus.InProgress
                          || x.Status == FileUploadSessionStatus.Paused
                          || x.Status == FileUploadSessionStatus.PendingToComplete)
                          && x.SessionStartDate <= DateTime.Now.AddHours(-48),
                stoppingToken);

                _logger.LogInformation("[SessionsStatusCheckerService] {SessionCount} sessions found for processing.", sessions.Count());
                foreach (var session in sessions)
                {
                    _logger.LogInformation("[SessionsStatusCheckerService] Marking session {SessionId} as failed.", session.Id);
                    session.MarkAsFailed();
                    await _fileUploadSessionRepository.UpdateAsync(session, stoppingToken);
                }
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SessionsStatusCheckerService] An error occurred while processing sessions status.");
            }
        }
    }
}
