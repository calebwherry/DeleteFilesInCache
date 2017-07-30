using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace DeleteFilesInCache
{
    internal sealed class FileCopier
    {
        private static readonly object Locker = new object();

        private static int NumTasksComplete;

        private int NumTasksCreated { get; set; }

        private string CacheDirectory { get; set; }

        private bool OutOfProcess { get; set; }

        private BlockingCollection<string> CopyQueue { get; set; }

        private BlockingCollection<string> DeleteQueue { get; set; }

        public FileCopier(
            int numTasksCreated,
            string cacheDirectory,
            bool outOfProcess,
            BlockingCollection<string> copyQueue,
            BlockingCollection<string> deleteQueue)
        {
            this.NumTasksCreated = numTasksCreated;
            this.CacheDirectory = cacheDirectory;
            this.OutOfProcess = outOfProcess;
            this.CopyQueue = copyQueue;
            this.DeleteQueue = deleteQueue;
        }

        public void Start()
        {
            // Grab next item from work queue:
            foreach (var fileToCopy in this.CopyQueue.GetConsumingEnumerable())
            {
                var cacheName = Path.Combine(this.CacheDirectory, Guid.NewGuid() + Path.GetExtension(fileToCopy));

                try
                {
                    if (this.OutOfProcess)
                    {
                        var executableName = "DeleteFilesInCache_OutOfProcessWorker.exe";
                        var arguments = "\"" + fileToCopy + "\" \"" + cacheName + "\"";
                        HelperMethods.RunOutOfProcessExe(executableName, arguments);
                    }
                    else
                    {
                        File.Copy(fileToCopy, cacheName);
                    }

                    this.DeleteQueue.Add(cacheName);
                }
                catch
                {
                    // Could not copy file.
                }
            }

            // This task is now complete so mark the delete queue closed for adding:
            // Note: Only close the queue after all current tasks are done.
            Interlocked.Increment(ref NumTasksComplete);
            lock (Locker)
            {
                if (NumTasksComplete == this.NumTasksCreated)
                {
                    this.DeleteQueue.CompleteAdding();
                }
            }
        }
    }
}
