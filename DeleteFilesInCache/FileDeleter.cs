using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace DeleteFilesInCache
{
    internal sealed class FileDeleter
    {
        private static readonly object Locker = new object();

        private static int NumTasksComplete;

        private int NumTasksCreated { get; set; }

        private bool OutOfProcess { get; set; }

        private BlockingCollection<string> DeleteQueue { get; set; }

        public FileDeleter(
            int numTasksCreated,
            bool outOfProcess,
            BlockingCollection<string> deleteQueue)
        {
            this.NumTasksCreated = numTasksCreated;
            this.OutOfProcess = outOfProcess;
            this.DeleteQueue = deleteQueue;
        }

        public void Start()
        {
            // Grab next item from work queue:
            foreach (var fileToDelete in this.DeleteQueue.GetConsumingEnumerable())
            {
                try
                {
                    if (this.OutOfProcess)
                    {
                        var executableName = "DeleteFilesInCache_OutOfProcessWorker.exe";
                        var arguments = "\"" + fileToDelete + "\"";
                        HelperMethods.RunOutOfProcessExe(executableName, arguments);
                    }
                    else
                    {
                        File.Delete(fileToDelete);
                    }
                }
                catch(Exception ex)
                {
                    // Could not delete file so we throw it back on the queue after giving it a bit of time:
                    Console.WriteLine("Can't delete file, putting back on queue: errormsg='" + ex + "'");
                    Thread.Sleep(2000);
                    this.DeleteQueue.Add(fileToDelete);
                }
            }

            // This task is now complete:
            Interlocked.Increment(ref NumTasksComplete);
        }
    }
}
