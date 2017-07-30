using System.IO;

namespace DeleteFilesInCache_OutOfProcessWorker
{
    class Program
    {
        static int Main(string[] args)
        {
            // This OutOfProcess worker has 2 modes:
            // 1 arg  => Treat arg as file and delete it.
            // 2 args => Treat args as files and copy first file to second file.
            var numArgs = args.Length;

            if (numArgs == 1)
            {
                try
                {
                    var fileToDelete = args[0];
                    File.Delete(fileToDelete);
                }
                catch
                {
                    return -1;
                } 
            }
            else if (numArgs == 2)
            {
                try
                {
                    var fileToCopy = args[0];
                    var fileToCopyTo = args[1];
                    File.Copy(fileToCopy, fileToCopyTo);
                }
                catch
                {
                    return -1;
                }
            }
            else
            {
                return -1;
            }

            return 0;
        }
    }
}
