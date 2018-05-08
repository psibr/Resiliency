using System;
using System.Threading;
using System.Threading.Tasks;

namespace REtry
{
    public class RetryOperation
    {
        public RetryOperation(RetryHandlerInfo handler, RetryTotalInfo total)
        {
            Handler = handler;
            Total = total;
        }

        public RetryHandlerInfo Handler { get; }

        public RetryTotalInfo Total { get; }

        public CancellationToken CancellationToken { get; }
    }
}
