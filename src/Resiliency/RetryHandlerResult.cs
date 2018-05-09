namespace Resiliency
{
    public enum RetryHandlerResult
    {
        Unhandled = 0,
        Handled = 1,
        Cancelled = 2
    }
}