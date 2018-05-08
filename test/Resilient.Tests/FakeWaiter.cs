using System;

namespace REtry.Tests
{
    using System.Threading;
    using System.Threading.Tasks;

    public class FakeWaiter
        : IWaiter
    {
        private readonly CancellationToken cancellationToken;

        public FakeWaiter(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
        }

        public Task WaitAsync(int milliseconds) => 
            WaitAsync(TimeSpan.FromMilliseconds(milliseconds));

        public Task WaitAsync(TimeSpan period)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            return Task.CompletedTask;
        }
    }
}
