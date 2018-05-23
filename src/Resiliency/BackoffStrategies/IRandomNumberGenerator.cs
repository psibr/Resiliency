using System;
using System.Collections.Generic;
using System.Text;

namespace Resiliency.BackoffStrategies
{
    /// <summary>
    /// TODO: Improve documentation.
    /// </summary>
    public interface IRandomNumberGenerator
    {
        /// <summary>
        /// Non-negative integer.
        /// </summary>
        int Next();

        /// <summary>
        /// Non-negative integer between [0, maxValue)
        /// </summary>
        int Next(int maxValue);

        /// <summary>
        /// Non-negative integer in [minValue, maxValue)
        /// </summary>
        int Next(int minValue, int maxValue);

        /// <summary>
        /// Fills given array with random bytes.
        /// </summary>
        void NextBytes(byte[] buffer);

        /// <summary>
        /// returns floating-point between [0, 1)
        /// </summary>
        /// <returns></returns>
        double NextDouble();
    }
}
