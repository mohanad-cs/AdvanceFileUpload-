
using AdvanceFileUpload.Application;

namespace AdvanceFileUpload.API
{
    public class SessionStatusCheckerWorker : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SessionStatusCheckerWorker> _logger;
        public SessionStatusCheckerWorker(IServiceProvider serviceProvider, ILogger<SessionStatusCheckerWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stating Session status checker worker...");
           var scope= _serviceProvider.CreateScope();
           var sessionChecker= scope.ServiceProvider.GetRequiredService<SessionsStatusCheckerService>();
           await sessionChecker.StartAsync(cancellationToken);

        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Session status checker worker stopped.");
            return Task.CompletedTask;
        }
    }
}
