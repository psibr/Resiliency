
using System;

namespace Resiliency.BackoffStrategies
{
    /// <summary>
    /// A default random number generator for the library that inherits from <see cref="System.Random"/>
    /// to provide a basic implementation.
    /// <para />
    /// Provides an aditional method to return a random floating-point number within a range, similar
    /// to the methods that return an integer.
    /// </summary>
    public sealed class DefaultRandomNumberGenerator
        : Random
        , IRandomNumberGenerator
    {
        public double Next(double minValue, double maxValue) =>
            NextDouble() * (maxValue - minValue) + minValue;
    }
}
