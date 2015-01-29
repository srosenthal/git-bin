using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace GitBin
{
    public interface ICacheManager
    {
        byte[] ReadChunkFromCache(string chunkHash);

        /// <summary>
        /// Write a chunk to the local cache for future (and current) use.
        /// </summary>
        /// <param name="contents">Contents of the chunk</param>
        /// <param name="contentLength">Length of the provided buffer to consider as part of the chunk</param>
        /// <returns>Name of the chunk that was cached</returns>
        string WriteChunkToCache(byte[] contents, int contentLength);
        GitBinFileInfo[] ListChunks();
        void ClearCache();
        string[] GetChunksNotInCache(IEnumerable<string> chunkHashes);
        string GetPathForChunk(string chunkHash);
    }

    public class CacheManager : ICacheManager
    {
        private readonly DirectoryInfo _cacheDirectoryInfo;

        public CacheManager(IConfigurationProvider configurationProvider)
        {
            _cacheDirectoryInfo = Directory.CreateDirectory(configurationProvider.CacheDirectory);
        }

        public byte[] ReadChunkFromCache(string chunkHash)
        {
            var path = GetPathForChunk(chunkHash);

            if (!File.Exists(path))
                throw new ಠ_ಠ("Tried to read chunk from cache that does not exist. [" + chunkHash + ']');

            return File.ReadAllBytes(path);
        }

        public string WriteChunkToCache(byte[] contents, int contentLength)
        {
            var chunkHash = GetHashForChunk(contents, contentLength);
            var path = GetPathForChunk(chunkHash);
            
            if (File.Exists(path))
                return chunkHash;

            var filestream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, contentLength, FileOptions.WriteThrough);
            filestream.Write(contents, 0, contentLength);
            filestream.Close();

            return chunkHash;
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

        public string[] GetChunksNotInCache(IEnumerable<string> chunkHashes)
        {
            var chunksInCache = ListChunks().Select(fi => fi.Name);
            var chunksNotInCache = chunkHashes.Except(chunksInCache);

            // Try to remove any items from the list that clearly are not chunks, such as those files that don't have exactly 64 characters in their name.
            chunksNotInCache = chunksNotInCache.Where(hash => hash.Length == 32 * 2);

            return chunksNotInCache.ToArray();
        }

        public string GetPathForChunk(string chunkHash)
        {
            return Path.Combine(_cacheDirectoryInfo.FullName, chunkHash);
        }

        public static string GetHashForChunk(byte[] chunkBuffer, int chunkLength)
        {
            var hasher = new SHA256Managed();

            var hashBytes = hasher.ComputeHash(chunkBuffer, 0, chunkLength);
            var hashString = BitConverter.ToString(hashBytes).Replace("-", String.Empty);

            return hashString;
        }
    }
}