using System;

namespace Resiliency
{
    public class ExplicitTripCircuitBreakerStrategy
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
