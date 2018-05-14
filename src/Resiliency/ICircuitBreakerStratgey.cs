using System;
using System.Threading;

namespace Resiliency
{
    public interface ICircuitBreakerStratgey
    {
        bool ShouldTrip(Exception ex);

        void OnSuccess();
    }

    public class ConsecutiveFailureCircuitBreakerStrategy
        : ICircuitBreakerStratgey
    {
        private readonly int MaxConsecutiveFailureCount;

        private int _consecutiveFailureCount;

        public ConsecutiveFailureCircuitBreakerStrategy(int maxConsecutiveFailureCount)
        {
            MaxConsecutiveFailureCount = maxConsecutiveFailureCount;
        }

        public bool ShouldTrip(Exception ex)
        {
            var incrementedValue = Interlocked.Increment(ref _consecutiveFailureCount);

            return incrementedValue == MaxConsecutiveFailureCount;
        }

        public void OnSuccess()
        {
            Interlocked.Exchange(ref _consecutiveFailureCount, 0);
        }
    }

    public class ExplicitCircuitBreakerStrategy
        : ICircuitBreakerStratgey
    {
        public bool ShouldTrip(Exception ex)
        {
            return false;
        }

        public void OnSuccess()
        {
        }
    }
}
