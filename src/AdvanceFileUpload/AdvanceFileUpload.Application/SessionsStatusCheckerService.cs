
using AdvanceFileUpload.Domain;
using AdvanceFileUpload.Domain.Core;
using Microsoft.Extensions.Logging;

namespace AdvanceFileUpload.Application
{
    public sealed class SessionsStatusCheckerService
    {
        private readonly IRepository<FileUploadSession> _fileUploadSessionRepository;
        private readonly ILogger<SessionsStatusCheckerService> _logger;

        public SessionsStatusCheckerService(
             IRepository<FileUploadSession> fileUploadSessionRepository, ILogger<SessionsStatusCheckerService> logger)
        {
            _fileUploadSessionRepository = fileUploadSessionRepository ?? throw new ArgumentNullException(nameof(fileUploadSessionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Session status checker service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Waiting for the next execution cycle.");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

                    _logger.LogInformation("Fetching sessions older than 48 hours.");
                    var sessions = await _fileUploadSessionRepository.FindAsync(
                        x => (x.Status == FileUploadSessionStatus.InProgress
                              || x.Status == FileUploadSessionStatus.Paused
                              || x.Status == FileUploadSessionStatus.PendingToComplete)
                             && x.SessionStartDate <= DateTime.Now.AddHours(-48),
                        stoppingToken);

                    _logger.LogInformation("{SessionCount} sessions found for processing.", sessions.Count());

                    foreach (var session in sessions)
                    {
                        _logger.LogInformation("Marking session {SessionId} as failed.", session.Id);
                        session.MarkAsFailed();
                        await _fileUploadSessionRepository.UpdateAsync(session, stoppingToken);
                    }
                  

                    _logger.LogInformation("Session processing completed for this cycle.");
                }
                catch (TaskCanceledException) { }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing sessions.");
                }
            }

            _logger.LogInformation("Session status checker service stopped.");
        }
    }
}
