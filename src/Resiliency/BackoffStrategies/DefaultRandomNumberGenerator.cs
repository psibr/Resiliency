
using System;

namespace Resiliency.BackoffStrategies
{
    public sealed class DefaultRandomNumberGenerator
        : Random
        , IRandomNumberGenerator
    {
        public double Next(double minValue, double maxValue) =>
            NextDouble() * (maxValue - minValue) + minValue;
    }
}
