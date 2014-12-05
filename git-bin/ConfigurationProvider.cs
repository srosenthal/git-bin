using System;
using System.Collections.Generic;
using System.IO;
using GitBin.Remotes;

namespace GitBin
{
    public interface IConfigurationProvider
    {
        long ChunkSize { get; }
        long MaximumCacheSize { get; }
        string CacheDirectory { get; }

        long GetLong(string name, long defaultValue);
        string GetString(string name);
    }

    public class ConfigurationProvider : IConfigurationProvider
    {
        public const long DefaultChunkSize = 1024 * 1024;
        public const long DefaultMaximumCacheSize = long.MaxValue;

        public const string DefaultCacheDirectory = "git-bin";
        public const string SectionName = "git-bin";
        public const string ChunkSizeConfigName = "chunkSize";
        public const string MaximumCacheSizeConfigName = "maxCacheSize";
        public const string CacheDirectoryConfigName = "cacheDirectory";

        private readonly IGitExecutor _gitExecutor;
        private readonly Dictionary<string, string> _configurationOptions;

        public long ChunkSize { get; private set; }
        public long MaximumCacheSize { get; private set; }
        public string CacheDirectory { get; private set; }

        public ConfigurationProvider(IGitExecutor gitExecutor)
        {
            _gitExecutor = gitExecutor;

            _configurationOptions = GetConfigurationOptions();

            this.CacheDirectory = GetCacheDirectory(GetString(CacheDirectoryConfigName, DefaultCacheDirectory));
            this.ChunkSize = GetLong(ChunkSizeConfigName, DefaultChunkSize);
            this.MaximumCacheSize = GetLong(MaximumCacheSizeConfigName, DefaultMaximumCacheSize);
        }

        private Dictionary<string, string> GetConfigurationOptions()
        {
            var rawAllOptions = _gitExecutor
                .GetString("config --get-regexp " + SectionName)
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var options = new Dictionary<string, string>();

            foreach (var option in rawAllOptions)
            {
                var optionKeyValue = option.Split(' ');

                if (optionKeyValue.Length != 2)
                    throw new ಠ_ಠ("Invalid config option: " + option);

                var key = optionKeyValue[0]
                    .Substring(optionKeyValue[0].IndexOf('.') + 1)
                    .ToLowerInvariant();

                options[key] = optionKeyValue[1];
            }

            return options;
        }

        public long GetLong(string name, long defaultValue)
        {
            string rawValue;

            if (!_configurationOptions.TryGetValue(name.ToLowerInvariant(), out rawValue))
                return defaultValue;

            var convertedValue = Convert.ToInt64(rawValue);

            if (convertedValue < 0)
                throw new ಠ_ಠ(name + " cannot be negative");

            return convertedValue;
        }

        public string GetString(string name)
        {
            return GetString(name, null);
        }

        public string GetString(string name, string defaultValue)
        {
            string rawValue;

            if (!_configurationOptions.TryGetValue(name.ToLowerInvariant(), out rawValue))
            {
                if (defaultValue != null)
                    return defaultValue;
                else
                    throw new ಠ_ಠ('[' + name + "] must be set");
            }

            return rawValue;
        }

        private string GetCacheDirectory(string path)
        {
            if (Path.IsPathRooted(path) == true)
                return path;

            var rawValue = _gitExecutor.GetString("rev-parse --git-dir");

            if (string.IsNullOrEmpty(rawValue))
                throw new ಠ_ಠ("Error determining .git directory");

            return Path.Combine(rawValue, path);
        }
    }
}