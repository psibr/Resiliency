namespace REtry
{
    public static class RetryOperationExtensions
    {
        public static bool Handled(this IRetryOperation retry)
        {
            if (retry.CancellationToken.IsCancellationRequested)
                return false;

            return true;
        }

        public static bool Unhandled(this IRetryOperation retry) => false;
    }
}
