using System;
using System.Collections.Generic;
using System.IO;
using GitBin.Remotes;
using System.Threading;

namespace GitBin
{
    public static class AsyncFileProcessor
    {
        public static void ProcessFiles(string[] filesToDownload, Action<string[], int> fileProcessor)
        {
            object sync = new object();

            int totalFinished = 0;

            int nextIndexToDownload = 0;

            int totalDownloading = 0;
            int maxSimultaneousDownloads = 10;
            Exception lastException = null;
            int nextToReport = 10;

            lock (sync)
            {
                while (nextIndexToDownload < filesToDownload.Length)
                {
                    int indexToDownload = nextIndexToDownload;
                    totalDownloading++;

                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        Exception exception = null;
                        try
                        {
                            fileProcessor(filesToDownload, indexToDownload);
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }

                        lock (sync)
                        {
                            totalDownloading--;
                            totalFinished++;

                            var percentCompleted = (int)(100 * totalFinished / (float)filesToDownload.Length);

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

                    while (lastException == null && totalDownloading >= maxSimultaneousDownloads)
                    {
                        Monitor.Wait(sync);
                    }

                    if (lastException != null)
                    {
                        throw lastException;
                    }

                    nextIndexToDownload++;
                }

                while (lastException == null && totalFinished < filesToDownload.Length)
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
