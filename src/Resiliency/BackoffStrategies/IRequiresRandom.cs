using Resiliency.BackoffStrategies;

public interface IRequireRandom
{
    IRandomNumberGenerator RandomNumberGenerator { set; }
}
