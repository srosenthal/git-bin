using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace GitBin
{
    public interface ICacheManager
    {
        byte[] ReadChunkFromCache(string chunkName);

        /// <summary>
        /// Write a chunk to the local cache for future (and current) use.
        /// </summary>
        /// <param name="contents">Contents of the chunk</param>
        /// <param name="contentLength">Length of the provided buffer to consider as part of the chunk</param>
        /// <returns>Name of the chunk that was cached</returns>
        string WriteChunkToCache(byte[] contents, int contentLength);
        GitBinFileInfo[] ListChunks();
        void ClearCache();
        string[] GetChunksNotInCache(IEnumerable<string> filenamesToCheck);
        string GetPathForChunk(string filename);
    }

    public class CacheManager : ICacheManager
    {
        private readonly DirectoryInfo _cacheDirectoryInfo;

        public CacheManager(IConfigurationProvider configurationProvider)
        {
            _cacheDirectoryInfo = Directory.CreateDirectory(configurationProvider.CacheDirectory);
        }

        public byte[] ReadChunkFromCache(string chunkName)
        {
            var path = GetPathForChunk(chunkName);

            if (!File.Exists(path))
                throw new ಠ_ಠ("Tried to read file from cache that does not exist. [" + path + ']');

            return File.ReadAllBytes(path);
        }

        public string WriteChunkToCache(byte[] contents, int contentLength)
        {
            string chunkName = GetHashForChunk(contents, contentLength);
            var path = GetPathForChunk(chunkName);
            
            if (File.Exists(path))
                return chunkName;

            var filestream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, contentLength, FileOptions.WriteThrough);
            filestream.Write(contents, 0, contentLength);
            filestream.Close();

            return chunkName;
        }

        public GitBinFileInfo[] ListChunks()
        {
            var allFiles = _cacheDirectoryInfo.GetFiles();
            var gitBinFileInfos = allFiles.Select(fi => new GitBinFileInfo(fi.Name, fi.Length));

            return gitBinFileInfos.ToArray();
        }

        public void ClearCache()
        {
            foreach (var file in ListChunks())
            {
                File.Delete(GetPathForChunk(file.Name));
            }
        }

        public string[] GetChunksNotInCache(IEnumerable<string> chuckNamesToCheck)
        {
            var filenamesInCache = ListChunks().Select(fi => fi.Name);
            var filenamesNotInCache = chuckNamesToCheck.Except(filenamesInCache);

            return filenamesNotInCache.ToArray();
        }

        public string GetPathForChunk(string chunkName)
        {
            return Path.Combine(_cacheDirectoryInfo.FullName, chunkName);
        }

        public static string GetHashForChunk(byte[] chunkBuffer, int chunkLength)
        {
            var hasher = new SHA256Managed();

            byte[] hashBytes = hasher.ComputeHash(chunkBuffer, 0, chunkLength);
            var hashString = BitConverter.ToString(hashBytes).Replace("-", String.Empty);

            return hashString;
        }
    }
}