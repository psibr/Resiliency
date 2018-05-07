using System;
using System.Threading;
using System.Threading.Tasks;

namespace REtry
{
    public interface IRetryOperation
    {
        RetryHandlerInfo Handler { get; }
        RetryTotalInfo Total { get; }

        Task Wait(int milliseconds, CancellationToken cancellationToken = default);
        Task Wait(TimeSpan period, CancellationToken cancellationToken = default);
    }
}