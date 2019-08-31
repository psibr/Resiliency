using System;

namespace Resiliency
{
    public interface ICircuitBreakerStrategy
    {
        bool ShouldTrip(Exception ex);

        void OnSuccess();
    }
}
