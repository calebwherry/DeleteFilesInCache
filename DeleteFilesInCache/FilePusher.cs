using System.Collections.Concurrent;
using System.Threading;

namespace DeleteFilesInCache
{
    internal sealed class FilePusher
    {
        private static readonly object Locker = new object();

        private static int NumTasksComplete;

        private int NumTasksCreated { get; set; }

        private int MaxItemsToProcess { get; set; }

        private string TestFileName { get; set; }

        private BlockingCollection<string> CopyQueue { get; set; }

        public FilePusher(
            int numTasksCreated,
            int maxItemsToProcess,
            string testFileName,
            BlockingCollection<string> copyQueue)
        {
            this.NumTasksCreated = numTasksCreated;
            this.MaxItemsToProcess = maxItemsToProcess;
            this.TestFileName = testFileName;
            this.CopyQueue = copyQueue;
        }

        public void Start()
        {
            // Keep adding files to copy queue until we've reached the max amount:
            var numItemsAdded = 0;
            while (numItemsAdded < this.MaxItemsToProcess)
            {
                this.CopyQueue.Add(this.TestFileName);
                numItemsAdded++;
            }

            // This task is now complete so mark the delete queue closed for adding:
            // Note: Only close the queue after all current tasks are done.
            Interlocked.Increment(ref NumTasksComplete);
            lock (Locker)
            {
                if (NumTasksComplete == this.NumTasksCreated)
                {
                    this.CopyQueue.CompleteAdding();
                }
            }
        }
    }
}
