using System;
using System.Collections.Generic;
using System.IO;
using GitBin.Remotes;
using System.Threading;

namespace GitBin
{
    public static class AsyncFileProcessor
    {
        public static void ProcessFiles(string[] filesToProcess, Action<string, Action<int>> fileProcessor)
        {
            object sync = new object();
            RemoteProgressPrinter progressPrinter = new RemoteProgressPrinter(filesToProcess.Length);

            int indexToProcess = 0;
            int totalProcessing = 0;
            int processedFiles = 0;
            int maxSimultaneousProcessingOperations = 10;
            Exception lastException = null;

            lock (sync)
            {
                while (indexToProcess < filesToProcess.Length)
                {
                    int scopedIndexToProcess = indexToProcess;
                    totalProcessing++;

                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        Exception exception = null;

                        try
                        {
                            fileProcessor(filesToProcess[scopedIndexToProcess], (percentComplete) =>
                                progressPrinter.OnProgressChanged(scopedIndexToProcess, percentComplete));
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }

                        lock (sync)
                        {
                            totalProcessing--;
                            processedFiles++;

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

                    indexToProcess++;
                }

                while (lastException == null && processedFiles < filesToProcess.Length)
                {
                    Monitor.Wait(sync);
                }

                progressPrinter.Dispose();

                if (lastException != null)
                {
                    throw lastException;
                }
            }
        }
    }
}
