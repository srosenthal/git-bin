using System;
using System.Collections.Generic;
using System.IO;
using GitBin.Remotes;
using System.Threading;
using System.Diagnostics;

namespace GitBin.Commands
{
    public class SmudgeCommand : ICommand
    {
        private readonly ICacheManager _cacheManager;
        private readonly IRemote _remote;

        public SmudgeCommand(
            ICacheManager cacheManager,
            IRemote remote,
            string[] args)
        {
            if (args.Length != 0)
                throw new ArgumentException();

            _cacheManager = cacheManager;
            _remote = remote;
        }

        public void Execute()
        {
            var stdin = Console.OpenStandardInput();
            var document = GitBinDocument.FromYaml(new StreamReader(stdin));

            GitBinConsole.Write("Smudging {0}:", document.Filename);

            DownloadMissingChunks(document.ChunkHashes);

            OutputReassembledChunks(document.ChunkHashes);
        }

        private void DownloadMissingChunks(IEnumerable<string> chunkHashes)
        {
            string[] chunksToDownload = _cacheManager.GetChunksNotInCache(chunkHashes);

            if (chunksToDownload.Length == 0)
            {
                GitBinConsole.WriteLineNoPrefix(" All chunks already present in cache");
            }
            else
            {
                if (chunksToDownload.Length == 1)
                {
                    GitBinConsole.WriteNoPrefix(" Downloading 1 chunk: ");
                }
                else
                {
                    GitBinConsole.WriteNoPrefix(" Downloading {0} chunks: ", chunksToDownload.Length);
                }

                try
                {
                    AsyncFileProcessor.ProcessFiles(chunksToDownload, DownloadChunk);
                    GitBinConsole.WriteLine();
                }
                catch (ಠ_ಠ e)
                {
                    GitBinConsole.WriteNewLine(e.Message);
                }
            }

        }

        private void DownloadChunk(string chunkHash)
        {
            const int MAX_DOWNLOAD_ATTEMPT_COUNT = 5;

            var fullPath = _cacheManager.GetPathForChunk(chunkHash);

            var attemptCount = 0;
            for (; attemptCount < MAX_DOWNLOAD_ATTEMPT_COUNT; attemptCount++)
            {
                var chunkData = _remote.DownloadFile(chunkHash);
                var computedChunkHash = CacheManager.GetHashForChunk(chunkData, chunkData.Length);

                // A chunk's name is its hash. If a download's name and hash don't match then try and download it
                // again, because it failed the first time.
                if (chunkHash.Equals(computedChunkHash))
                {
                    _cacheManager.WriteChunkToCache(chunkData, chunkData.Length);
                    break;
                }
                else
                {
                    GitBinConsole.WriteNewLine("Error downloading chunk '" + chunkHash + "'. Retrying...");
                }
            }

            if (attemptCount >= MAX_DOWNLOAD_ATTEMPT_COUNT)
            {
                throw new ಠ_ಠ("Exceeded retry attempts when downloading chunk: " + chunkHash);
            }
        }

        private void OutputReassembledChunks(IEnumerable<string> chunkHashes)
        {
            var stdout = Console.OpenStandardOutput();

            foreach (var chunkHash in chunkHashes)
            {
                var chunkData = _cacheManager.ReadChunkFromCache(chunkHash);
                stdout.Write(chunkData, 0, chunkData.Length);
            }

            stdout.Flush();
        }
    }
}