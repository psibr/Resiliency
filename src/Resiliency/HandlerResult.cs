namespace Resiliency
{
    public enum HandlerResult
    {
        Unhandled = 0,
        Retry = 1,
        Break = 2,
        Return = 3
    }
}