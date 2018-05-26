using System;

namespace Resiliency.BackoffStrategies
{
    /// <summary>
    /// Calculates the amount of time to wait in between retries.
    /// </summary>
    public interface IBackoffStrategy
    {
        /// <summary>
        /// The time to wait for the first retry and from which additional attempts will be calculated.
        /// </summary>
        TimeSpan InitialWaitTime{ get; }

        /// <summary>
        /// Calculates the time to wait before the next retry attempt.
        /// </summary>
        /// <param name="attemptNumber">is the count of the most recent attempt that was made. It should start at 1.</param>
        /// <returns>The recommended time to wait before attempting to retry.</returns>
        TimeSpan GetWaitTime(int attemptNumber);
    }
}