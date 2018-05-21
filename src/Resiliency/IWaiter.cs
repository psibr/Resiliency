using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
{
    public interface IWaiter
    {
        Task WaitAsync(TimeSpan period);
    }

    public class TaskDelayWaiter
        : IWaiter
    {
        private readonly CancellationToken _cancellationToken;

        public TaskDelayWaiter(CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
        }

        public Task WaitAsync(TimeSpan period)
        {
            return Task.Delay(period, _cancellationToken);
        }
    }
}