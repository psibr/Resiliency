using System;

namespace Resiliency
{
    public interface ICircuitBreakerStratgey
    {
        bool ShouldTrip(Exception ex);

        void OnSuccess();
    }
}
