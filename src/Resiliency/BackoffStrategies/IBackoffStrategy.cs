using System;

namespace Resiliency.BackoffStrategies
{
    public interface IBackoffStrategy
    {
        TimeSpan InitialWaitTime{ get; }
        TimeSpan GetWaitTime(int attemptNumber);
    }
}