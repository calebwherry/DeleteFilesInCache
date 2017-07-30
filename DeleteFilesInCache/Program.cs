using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DeleteFilesInCache
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting.");

            // Configurable items:
            var threadCount = Environment.ProcessorCount;
            var cacheDirectory = "TEMP_CACHE";
            var testFileName = "sample.pdf"; // http://download.support.xerox.com/pub/docs/FlowPort2/userdocs/any-os/en/fp_dc_setup_guide.pdf
            var maxQueueSize = 10000;
            var maxItemsToProcess = 1000;
            var outOfProcessCopy = true;
            var outOfProcessDelete = false;

            // Queues for workflow:
            var copyQueue = new BlockingCollection<string>(maxQueueSize);
            var deleteQueue = new BlockingCollection<string>(maxQueueSize);

            // Create cache directory if it doesn't exist, otherwise clean it:
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }
            else
            {
                foreach (FileInfo file in new DirectoryInfo(cacheDirectory).GetFiles()) file.Delete();
            }

            // All tasks will 
            var allTasks = new List<Task>();

            // Add single-producer task:
            allTasks.Add(
                Task.Factory.StartNew(
                    () =>
                    {
                        (new FilePusher(
                            1,
                            maxItemsToProcess,
                            testFileName,
                            copyQueue))
                        .Start();
                    },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default));

            // Add multi-consumer tasks:
            for (var i =0; i < threadCount; i++)
            {
                // Add copier task:
                allTasks.Add(
                    Task.Factory.StartNew(
                        () =>
                        {
                            (new FileCopier(
                                threadCount,
                                cacheDirectory,
                                outOfProcessCopy,
                                copyQueue,
                                deleteQueue))
                            .Start();
                        },
                        CancellationToken.None,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default));

                // Add deleter task:
                allTasks.Add(
                    Task.Factory.StartNew(
                        () =>
                        {
                            (new FileDeleter(
                                threadCount,
                                outOfProcessDelete,
                                deleteQueue))
                            .Start();
                        },
                        CancellationToken.None,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default));
            }

            // Wait for all tasks to end:
            Console.WriteLine("Waiting on all tasks to complete...");
            Task.WaitAll(allTasks.ToArray());
            Console.WriteLine("All tasks complete.");
        }
    }
}
