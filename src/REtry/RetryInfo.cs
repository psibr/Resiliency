namespace REtry
{
    public abstract class RetryInfo
    {
        public int AttemptsExhausted { get; internal set; }
    }

    public class RetryHandlerInfo
        : RetryInfo
    {
    }

    public class RetryTotalInfo
        : RetryInfo
    {
    }
}
