using System;
using System.Collections.Generic;
using System.Linq;

namespace GitBin.Remotes
{
    public class RemoteProgressPrinter : IDisposable
    {
        private readonly int _chunkCount;
        private readonly int[] mostRecentChunkPercentages;
        private int summedChunkPercentage = 0;

        //private double lastPrintedPercentage = -1;
        private string lastPrintedStatusString = "";

        public RemoteProgressPrinter(int chunkCount)
        {
            _chunkCount = chunkCount;
            mostRecentChunkPercentages = Enumerable.Repeat(0, chunkCount).ToArray();
        }

        public void Dispose()
        {
            GitBinConsole.WriteNoPrefix(Environment.NewLine);
        }

        public void OnProgressChanged(int chunkNumber, int percentageComplete)
        {
            var chunkProgressDelta = percentageComplete - mostRecentChunkPercentages[chunkNumber];
            mostRecentChunkPercentages[chunkNumber] = percentageComplete;
            summedChunkPercentage += chunkProgressDelta;
            double totalPercentage = (double)summedChunkPercentage / _chunkCount;

            string percentageToPrint;
            lock (this)
            {
                percentageToPrint = String.Format("{0:N2}%", totalPercentage);

                GitBinConsole.WriteNoPrefix(new String('\b', lastPrintedStatusString.Length) + percentageToPrint);
                GitBinConsole.Flush();

                lastPrintedStatusString = percentageToPrint;
            }

        }
    }
}