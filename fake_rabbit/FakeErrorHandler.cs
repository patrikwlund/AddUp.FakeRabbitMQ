using System;
using System.Threading;
using log4net.Core;

namespace PPA.Logging.Amqp.Tests.Fakes
{
    public class FakeErrorHandler : IErrorHandler
    {
        private int _errorCount;


        public int ErrorCount
        {
            get { return _errorCount; }
        }

        public void Error(string message, Exception e, ErrorCode errorCode)
        {
            Interlocked.Increment(ref _errorCount);
        }

        public void Error(string message, Exception e)
        {
            Interlocked.Increment(ref _errorCount);
        }

        public void Error(string message)
        {
            Interlocked.Increment(ref _errorCount);
        }
    }
}