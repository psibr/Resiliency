namespace Resiliency.BackoffStrategies
{
    /// <summary>
    /// An interface for a random number generator that loosely follows the methods provided by the System.Random class.
    /// </summary>
    public interface IRandomNumberGenerator
    {
        /// <summary>
        /// Returns a non-negative integer.
        /// </summary>
        int Next();

        /// <summary>
        /// Returns a non-negative integer between [0, maxValue)
        /// </summary>
        int Next(int maxValue);

        /// <summary>
        /// Returns a non-negative integer in [minValue, maxValue)
        /// </summary>
        int Next(int minValue, int maxValue);

        /// <summary>
        /// Returns a non-negative floating-point in [minValue, maxValue)
        /// </summary>
        double Next(double minValue, double maxValue);

        /// <summary>
        /// Fills a given array with random bytes.
        /// </summary>
        void NextBytes(byte[] buffer);

        /// <summary>
        /// Returns a floating-point between [0, 1)
        /// </summary>
        double NextDouble();
    }
}
