using System;
using System.Collections.Generic;
using System.IO;
using GitBin.Remotes;
using System.Threading;

namespace GitBin
{
    public static class AsyncFileProcessor
    {
        public static void ProcessFiles(string[] filesToProcess, Action<string> fileProcessor)
        {
            object sync = new object();

            int totalFinished = 0;
            int nextIndexToProcess = 0;
            int totalProcessing = 0;
            int maxSimultaneousProcessingOperations = 10;
            Exception lastException = null;
            int nextToReport = 10;

            lock (sync)
            {
                while (nextIndexToProcess < filesToProcess.Length)
                {
                    string fileToProcess = filesToProcess[nextIndexToProcess];
                    totalProcessing++;

                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        Exception exception = null;
                        try
                        {
                            fileProcessor(fileToProcess);
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }

                        lock (sync)
                        {
                            totalProcessing--;
                            totalFinished++;

                            var percentCompleted = (int)(100 * totalFinished / (float)filesToProcess.Length);

                            if (percentCompleted >= nextToReport)
                            {
                                GitBinConsole.WriteNoPrefix(percentCompleted.ToString());
                                if (percentCompleted < 100)
                                {
                                    GitBinConsole.WriteNoPrefix(".");
                                }
                                nextToReport = percentCompleted + 10;
                            }

                            if (lastException == null)
                            {
                                lastException = exception;
                            }

                            Monitor.Pulse(sync);
                        }
                    });

                    while (lastException == null && totalProcessing >= maxSimultaneousProcessingOperations)
                    {
                        Monitor.Wait(sync);
                    }

                    if (lastException != null)
                    {
                        throw lastException;
                    }

                    nextIndexToProcess++;
                }

                while (lastException == null && totalFinished < filesToProcess.Length)
                {
                    Monitor.Wait(sync);
                }

                if (lastException != null)
                {
                    throw lastException;
                }
            }
        }
    }
}
