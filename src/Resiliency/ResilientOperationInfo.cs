namespace Resiliency
{
    public abstract class ResilientOperationInfo
    {
        public int AttemptsExhausted { get; internal set; }
    }

    public class ResilientOperationHandlerInfo
        : ResilientOperationInfo
    {
    }

    public class ResilientOperationTotalInfo
        : ResilientOperationInfo
    {
    }
}
