using System;

namespace Resiliency
{
    public class CircuitBrokenException
        : Exception
    {
        const string DefaultMessage = "Too many operations have failed or an explicit throttle has occured, no calls are accepted at this time.";

        public CircuitBrokenException()
            : base(DefaultMessage)
        {
            MinimumRetryPeriod = TimeSpan.Zero;
        }

        public CircuitBrokenException(string message)
            : base(message)
        {
            MinimumRetryPeriod = TimeSpan.Zero;
        }

        public CircuitBrokenException(string message, Exception innerException)
            : base(message, innerException)
        {
            MinimumRetryPeriod = TimeSpan.Zero;
        }

        public CircuitBrokenException(Exception innerException)
            : base(DefaultMessage, innerException)
        {
            MinimumRetryPeriod = TimeSpan.Zero;
        }

        public CircuitBrokenException(TimeSpan minimumRetryPeriod)
            : base(DefaultMessage)
        {
            MinimumRetryPeriod = minimumRetryPeriod;
        }

        public CircuitBrokenException(string message, TimeSpan minimumRetryPeriod)
            : base(message)
        {
            MinimumRetryPeriod = minimumRetryPeriod;
        }

        public CircuitBrokenException(string message, Exception innerException, TimeSpan minimumRetryPeriod)
            : base(message, innerException)
        {
            MinimumRetryPeriod = minimumRetryPeriod;
        }

        public CircuitBrokenException(Exception innerException, TimeSpan minimumRetryPeriod)
            : base(DefaultMessage, innerException)
        {
            MinimumRetryPeriod = minimumRetryPeriod;
        }

        public TimeSpan MinimumRetryPeriod { get; }
    }
}
