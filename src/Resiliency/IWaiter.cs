using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
{
    /// <summary>
    /// Allows wait operations.
    /// </summary>
    /// <remarks>Useful for testing and mocking.</remarks>
    public interface IWaiter
    {
        /// <summary>
        /// Waits for a period of time.
        /// </summary>
        /// <param name="period">The period of time to wait.</param>
        /// <returns>Empty task</returns>
        Task WaitAsync(TimeSpan period);
    }

    public class TaskDelayWaiter
        : IWaiter
    {
        private readonly CancellationToken _cancellationToken;

        public TaskDelayWaiter(CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
        }

        public Task WaitAsync(TimeSpan period)
        {
            if (period <= TimeSpan.Zero)
                return Task.CompletedTask;

            return Task.Delay(period, _cancellationToken);
        }
    }
}