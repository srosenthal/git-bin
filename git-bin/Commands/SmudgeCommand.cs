using System;
using System.Collections.Generic;
using System.IO;
using GitBin.Remotes;
using System.Threading;

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

            DownloadMissingFiles(document.ChunkHashes);

            OutputReassembledChunks(document.ChunkHashes);
        }

        private void DownloadMissingFiles(IEnumerable<string> chunkHashes)
        {
            var filesToDownload = _cacheManager.GetFilenamesNotInCache(chunkHashes);

            if (filesToDownload.Length == 0)
            {
                GitBinConsole.WriteNoPrefix(" All chunks already present in cache\n");
            }
            else
            {
                if (filesToDownload.Length == 1)
                {
                    GitBinConsole.WriteNoPrefix(" Downloading 1 chunk: ");
                }
                else
                {
                    GitBinConsole.WriteNoPrefix(" Downloading {0} chunks: ", filesToDownload.Length);
                }

                AsyncFileProcessor.ProcessFiles(filesToDownload, DownloadFile);
            }

            GitBinConsole.WriteLine();
        }

        private void DownloadFile(string[] filesToDownload, int indexToDownload)
        {
            var file = filesToDownload[indexToDownload];
            _remote.DownloadFile(_cacheManager.GetPathForFile(file), file);
        }

        private void OutputReassembledChunks(IEnumerable<string> chunkHashes)
        {
            var stdout = Console.OpenStandardOutput();

            foreach (var chunkHash in chunkHashes)
            {
                var chunkData = _cacheManager.ReadFileFromCache(chunkHash);
                stdout.Write(chunkData, 0, chunkData.Length);
            }

            stdout.Flush();
        }
    }
}