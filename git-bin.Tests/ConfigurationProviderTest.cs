using System;
using GitBin;
using GitBin.Remotes;
using NUnit.Framework;
using Moq;

namespace git_bin.Tests
{
    [TestFixture]
    public class ConfigurationProviderTest
    {
        private Mock<IGitExecutor> _gitExecutor;

        [SetUp]
        public void SetUp()
        {
            _gitExecutor = new Mock<IGitExecutor>();

            _gitExecutor.Setup(x => x.GetString("config --get-regexp git-bin"))
                .Returns("git-bin.KeyOne ValueOne\ngit-bin.KeyTwo 2");

            _gitExecutor.Setup(x => x.GetString("rev-parse --git-dir")).Returns("a");
        }

        private string GetConfigArgumentString(string keyName)
        {
            return "config " + ConfigurationProvider.SectionName + '.' + keyName;
        }

        private string GetConfigArgumentStringForInt(string keyName)
        {
            return "config --int " + ConfigurationProvider.SectionName + '.' + keyName;
        }

        [Test]
        public void ChunkSize_ValueIsPositive_GetsSet()
        {
            _gitExecutor.Setup(x => x.GetLong(GetConfigArgumentStringForInt(ConfigurationProvider.ChunkSizeConfigName))).Returns(42);

            var target = new ConfigurationProvider(_gitExecutor.Object);

            Assert.AreEqual(42, target.ChunkSize);
        }

        [Test]
        public void ChunkSize_ValueIsNegative_Throws()
        {
            _gitExecutor.Setup(x => x.GetLong(GetConfigArgumentStringForInt(ConfigurationProvider.ChunkSizeConfigName))).Returns(-42);

            try
            {
                var target = new ConfigurationProvider(_gitExecutor.Object);
            }
            catch (ಠ_ಠ)
            {
                Assert.Pass();
            }

            Assert.Fail("Exception not thrown for negative chunk size");
        }

        [Test]
        public void ChunkSize_ValueIsEmpty_SetToDefault()
        {
            _gitExecutor.Setup(x => x.GetLong(GetConfigArgumentStringForInt(ConfigurationProvider.ChunkSizeConfigName))).Returns((int?)null);

            var target = new ConfigurationProvider(_gitExecutor.Object);

            Assert.AreEqual(ConfigurationProvider.DefaultChunkSize, target.ChunkSize);
        }

        [Test]
        public void MaximumCacheSize_ValueIsPositive_GetsSet()
        {
            _gitExecutor.Setup(x => x.GetLong(GetConfigArgumentStringForInt(ConfigurationProvider.MaximumCacheSizeConfigName))).Returns(42);

            var target = new ConfigurationProvider(_gitExecutor.Object);

            Assert.AreEqual(42, target.MaximumCacheSize);
        }

        [Test]
        public void MaximumCacheSize_ValueIsNegative_Throws()
        {
            _gitExecutor.Setup(x => x.GetLong(GetConfigArgumentStringForInt(ConfigurationProvider.MaximumCacheSizeConfigName))).Returns(-42);

            try
            {
                var target = new ConfigurationProvider(_gitExecutor.Object);
            }
            catch (ಠ_ಠ)
            {
                Assert.Pass();
            }

            Assert.Fail("Exception not thrown for negative cache size");
        }

        [Test]
        public void MaximumCacheSize_ValueIsEmpty_SetToDefault()
        {
            _gitExecutor.Setup(x => x.GetLong(GetConfigArgumentStringForInt(ConfigurationProvider.MaximumCacheSizeConfigName))).Returns((int?)null);

            var target = new ConfigurationProvider(_gitExecutor.Object);

            Assert.AreEqual(ConfigurationProvider.DefaultMaximumCacheSize, target.MaximumCacheSize);
        }

        [Test]
        public void CacheDirectory_HasValue_GetsSet()
        {
            string gitDir = "directory";
            string expectedDir = "directory\\git-bin";

            _gitExecutor.Setup(x => x.GetString("rev-parse --git-dir")).Returns(gitDir);

            var target = new ConfigurationProvider(_gitExecutor.Object);

            Assert.AreEqual(expectedDir, target.CacheDirectory);
        }

        [Test]
        public void CacheDirectory_NoValue_Throws()
        {
            _gitExecutor.Setup(x => x.GetString("rev-parse --git-dir")).Returns(string.Empty);

            try
            {
                var target = new ConfigurationProvider(_gitExecutor.Object);
            }
            catch (ಠ_ಠ)
            {
                Assert.Pass();
            }

            Assert.Fail("Exception not thrown for CacheDir");
        }
    }
}