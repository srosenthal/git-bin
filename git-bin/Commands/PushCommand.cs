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
            var filesInCache = _cacheManager.ListFiles();

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

                AsyncFileProcessor.ProcessFiles(filesToUpload,
                    (files, index) =>
                    {
                        var file = filesToUpload[index];
                        _remote.UploadFile(_cacheManager.GetPathForFile(file), file);
                    });
                
            }
            Console.WriteLine();
        }
    }
}