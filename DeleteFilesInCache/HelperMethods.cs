using System;
using System.Diagnostics;
using System.IO;

namespace DeleteFilesInCache
{
    internal static class HelperMethods
    {
        internal static void RunOutOfProcessExe(
            String executableName,
            String arguments)
        {
            // Create a new process to run:
            var processStartInfo = new ProcessStartInfo
            {
                WorkingDirectory = Directory.GetCurrentDirectory(),
                FileName = executableName,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var stdOut = String.Empty;
            var stdErr = String.Empty;
            var exitCode = -1;
            var processSuccessful = false;

            using (var process = Process.Start(processStartInfo))
            {
                var stdOutLocal = process.StandardOutput.ReadToEnd();
                var stdErrLocal = process.StandardError.ReadToEnd();
                var processCompleted = process.WaitForExit(1000 * 60 * 5); // Wait up to 5 minutes.
                if (!processCompleted)
                {
                    process.Kill();
                }

                // Grab some vars:
                var exitCodeLocal = process.ExitCode;
                var processSuccessfulLocal = exitCodeLocal == 0;

                // Set values:
                stdOut = stdOutLocal;
                stdErr = stdErrLocal;
                exitCode = exitCodeLocal;
                processSuccessful = processSuccessfulLocal;

                // Kill process just to make sure it is dead:
                try
                {
                    process.Kill();
                }
                catch
                {
                }

                process.Close();
                process.Dispose();
            }

            if (!processSuccessful)
            {
                throw new Exception();
            }
        }
    }
}
