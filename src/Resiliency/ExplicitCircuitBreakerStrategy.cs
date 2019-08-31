using System;

namespace Resiliency
{
    public class ExplicitTripCircuitBreakerStrategy
        : ICircuitBreakerStrategy
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
