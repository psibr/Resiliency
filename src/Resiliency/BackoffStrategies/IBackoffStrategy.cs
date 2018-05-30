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
        TimeSpan InitialWaitTime { get; }

        /// <summary>
        /// Calculates the time to wait before the next retry attempt.
        /// </summary>
        /// <returns>The recommended time to wait before attempting to retry.</returns>
        TimeSpan Next();
    }
}