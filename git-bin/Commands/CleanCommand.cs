using System;
using System.Security.Cryptography;
using System.Text;

namespace GitBin.Commands
{
    public class CleanCommand : ICommand
    {
        private readonly IConfigurationProvider _configurationProvider;
        private readonly ICacheManager _cacheManager;
        private readonly string _filename;

        public CleanCommand(
            IConfigurationProvider configurationProvider,
            ICacheManager cacheManager,
            string[] args)
        {
            if (args.Length != 1)
                throw new ArgumentException();

            _configurationProvider = configurationProvider;
            _cacheManager = cacheManager;

            _filename = args[0];
        }

        public void Execute()
        {
            GitBinConsole.WriteLine("Cleaning {0}", _filename);

            var document = new GitBinDocument(_filename);

            var chunkBuffer = new byte[_configurationProvider.ChunkSize];
            var numberOfBytesRead = 0;
            var totalBytesInChunk = 0;

            var stdin = Console.OpenStandardInput();

            do
            {
                numberOfBytesRead = stdin.Read(chunkBuffer, totalBytesInChunk, chunkBuffer.Length - totalBytesInChunk);
                
                totalBytesInChunk += numberOfBytesRead;

                if ((totalBytesInChunk == chunkBuffer.Length || numberOfBytesRead == 0) && totalBytesInChunk > 0)
                {
                    var chunkHash = _cacheManager.WriteChunkToCache(chunkBuffer, totalBytesInChunk);

                    document.RecordChunk(chunkHash);
                    totalBytesInChunk = 0;
                }
            } while (numberOfBytesRead > 0);

            var yamlString = GitBinDocument.ToYaml(document);

            Console.Write(yamlString);
            Console.Out.Flush();
        }
    }
}