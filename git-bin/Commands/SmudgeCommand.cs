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

            try
            {
                DownloadMissingChunks(document.ChunkHashes);
                OutputReassembledChunks(document.ChunkHashes);
            }
            catch (Exception e)
            {
                GitBinConsole.WriteNewLine(e.Message);
                Console.Error.Write(e);
            }
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

                AsyncFileProcessor.ProcessFiles(chunksToDownload, DownloadChunk);
            }
        }

        private void DownloadChunk(string chunkHash, Action<int> progressListener)
        {
            var fullPath = _cacheManager.GetPathForChunk(chunkHash);
            var chunkData = _remote.DownloadFile(chunkHash, progressListener);
            var computedChunkHash = CacheManager.GetHashForChunk(chunkData, chunkData.Length);

            // A chunk's name is its hash. If a download's name and hash don't match then try and download it
            // again, because it failed the first time.
            if (chunkHash.Equals(computedChunkHash))
            {
                _cacheManager.WriteChunkToCache(chunkData, chunkData.Length);
            }
            else
            {
                GitBinConsole.WriteNewLine("Downloaded a corrupted chunk (" + chunkHash + "). Aborting.");
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