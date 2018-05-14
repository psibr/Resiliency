using System;

namespace Resiliency
{
    public class CircuitBreakerOptions
    {
        public TimeSpan DefaultCooldownPeriod { get; set; } = TimeSpan.FromSeconds(30);

        public CircuitState InitialState { get; set; } = CircuitState.Closed;

        public int HalfOpenSuccessCountBeforeClose { get; set; } = 1;
    }
}
