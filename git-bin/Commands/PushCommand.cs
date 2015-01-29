using System;
using System.IO;
using System.Linq;
using GitBin.Remotes;

namespace GitBin.Commands
{
    public class PushCommand : ICommand
    {
        private readonly ICacheManager _cacheManager;
        private readonly IRemote _remote;

        public PushCommand(
            ICacheManager cacheManager,
            IRemote remote,
            string[] args)
        {
            if (args.Length > 0)
                throw new ArgumentException();

            _cacheManager = cacheManager;
            _remote = remote;
        }

        public void Execute()
        {
            var filesInRemote = _remote.ListFiles();
            var filesInCache = _cacheManager.ListChunks();

            var filesToUpload = filesInCache.Except(filesInRemote).Select(x => x.Name).ToArray();

            if (filesToUpload.Length == 0)
            {
                GitBinConsole.Write("All chunks already present on remote");
            }
            else
            {
                if (filesToUpload.Length == 1)
                {
                    GitBinConsole.Write("Uploading 1 chunk: ");
                }
                else
                {
                    GitBinConsole.Write("Uploading {0} chunks: ", filesToUpload.Length);
                }

                try
                {
                    AsyncFileProcessor.ProcessFiles(filesToUpload, (chunkHash) =>
                    {
                        verifyChunkIntegrity(chunkHash);
                        _remote.UploadFile(_cacheManager.GetPathForChunk(chunkHash), chunkHash);
                    });
                }
                catch (InvalidDataException e)
                {
                    GitBinConsole.WriteNoPrefix("\n");
                    GitBinConsole.WriteLine(e.Message);
                }
                catch (ಠ_ಠ e)
                {
                    GitBinConsole.WriteNoPrefix("\n");
                    GitBinConsole.WriteLine("\nEncountered an error pushing to the remote chunk cache {0}", e.Message);
                }
            }

            Console.WriteLine();
        }

        private void verifyChunkIntegrity(string chunkHash)
        {
            var chunkPath = _cacheManager.GetPathForChunk(chunkHash);
            byte[] chunkData = File.ReadAllBytes(chunkPath);
            var calculatedChunkHash = CacheManager.GetHashForChunk(chunkData, chunkData.Length);

            if (!calculatedChunkHash.Equals(chunkHash))
            {
                throw new InvalidDataException("Chunk '" + chunkHash + "' is corrupted in the local cache, aborting. " +
                    "Please correct the issue before continue.");
            }
        }
    }
}