using System;
using System.Threading;
using System.Threading.Tasks;

namespace REtry
{
    public interface IRetryOperation
    {
        RetryHandlerInfo Handler { get; }
        RetryTotalInfo Total { get; }

        CancellationToken CancellationToken { get; }

        Task Wait(int millisecond);
        Task Wait(TimeSpan period);
    }
}