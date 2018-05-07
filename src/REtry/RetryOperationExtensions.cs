namespace REtry
{
    public static class RetryOperationExtensions
    {
        public static bool Handled(this IRetryOperation retry) => true;

        public static bool Unhandled(this IRetryOperation retry) => false;
    }
}
