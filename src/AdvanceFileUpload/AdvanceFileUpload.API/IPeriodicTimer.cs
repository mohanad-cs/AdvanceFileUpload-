namespace AdvanceFileUpload.API
{
    /// <summary>
    /// Represents a timer that provides periodic ticks for executing tasks.
    /// </summary>
    public interface IPeriodicTimer : IDisposable
    {
        /// <summary>
        /// Waits asynchronously for the next tick of the timer.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that resolves to <see langword="true"/> if the timer ticked, or <see langword="false"/> if the wait was canceled.</returns>
        ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the period of the timer.
        /// </summary>
        TimeSpan Period { get; }
    }

    /// <summary>
    /// A timer that provides periodic ticks for executing tasks on a daily basis.
    /// </summary>
    public sealed class DailyPeriodicTimer : IPeriodicTimer
    {
        private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(24));

        ///<inheritdoc/>
        public async ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken = default)
            => await _timer.WaitForNextTickAsync(cancellationToken);

        ///<inheritdoc/>
        public TimeSpan Period => _timer.Period;


        /// <summary>
        /// Releases the resources used by the timer.
        /// </summary>
        public void Dispose() => _timer.Dispose();
    }
}
