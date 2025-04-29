
using System.Threading;
using AdvanceFileUpload.Application;
using AdvanceFileUpload.Data;
using Microsoft.Extensions.DependencyInjection;

namespace AdvanceFileUpload.API
{
    /// <summary>
    /// Background service that periodically checks the status of sessions.
    /// </summary>
    public class SessionStatusCheckerWorker : BackgroundService
    {
        private readonly IPeriodicTimer _periodicTimer;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SessionStatusCheckerWorker> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStatusCheckerWorker"/> class.
        /// </summary>
        /// <param name="periodicTimer">The timer used to control the periodic execution.</param>
        /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
        /// <param name="logger">The logger for logging information and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
        public SessionStatusCheckerWorker(IPeriodicTimer periodicTimer, IServiceProvider serviceProvider, ILogger<SessionStatusCheckerWorker> logger)
        {
            _periodicTimer = periodicTimer ?? throw new ArgumentNullException(nameof(periodicTimer));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes the background service logic.
        /// </summary>
        /// <param name="stoppingToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            SessionsStatusCheckerService checker = scope.ServiceProvider.GetRequiredService<SessionsStatusCheckerService>();
            _logger.LogInformation("Session status checker Worker started and waiting for the next execution cycle. The next execution will be after {Period}h", _periodicTimer.Period.TotalHours);
            while (!stoppingToken.IsCancellationRequested && await _periodicTimer.WaitForNextTickAsync(stoppingToken))
            {
                await checker.CheckStatusAsync(stoppingToken);
                _logger.LogInformation("Session status checker Worker completed the cycle.");
                _logger.LogInformation("Session status checker Worker Waiting for the next execution cycle.");
            }
        }
    }
   
}
