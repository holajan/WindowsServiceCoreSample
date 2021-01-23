using System;
using System.Threading;
using System.Collections.Concurrent;

namespace WindowsServiceCoreSample.Logging
{
    internal sealed class TraceSourceLoggerProcessor : IDisposable
    {
        #region member varible and default property initialization
        private readonly BlockingCollection<string> MessageQueue = new BlockingCollection<string>(1024);
        private readonly Thread OutputThread;
        #endregion

        #region constructors and destructors
        public TraceSourceLoggerProcessor()
        {
            this.OutputThread = new Thread(new ThreadStart(ProcessLogQueue))
            {
                IsBackground = true,
                Name = "TraceSource logger queue processing thread"
            };
            this.OutputThread.Start();
        }
        #endregion

        #region action methods
        public void EnqueueMessage(string message)
        {
            if (!this.MessageQueue.IsAddingCompleted)
            {
                try
                {
                    this.MessageQueue.Add(message);
                    return;
                }
                catch (InvalidOperationException)
                {
                    //Ignore exception
                }
            }

            try
            {
                WriteMessage(message);
            }
            catch
            {
                //Ignore exception
            }
        }

        public void Dispose()
        {
            this.MessageQueue.CompleteAdding();

            try
            {
                this.OutputThread.Join(1500);
            }
            catch (ThreadStateException)
            {
                //Ignore exception
            }
        }
        #endregion

        #region private member functions
        private void WriteMessage(string message)
        {
            System.Diagnostics.Trace.WriteLine(message);
        }

        private void ProcessLogQueue()
        {
            try
            {
                foreach (string item in this.MessageQueue.GetConsumingEnumerable())
                {
                    WriteMessage(item);
                }
            }
            catch
            {
                try
                {
                    this.MessageQueue.CompleteAdding();
                }
                catch
                {
                    //Ignore exception
                }
            }
        }
        #endregion
    }
}