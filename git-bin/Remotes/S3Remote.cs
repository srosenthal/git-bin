using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Security.Cryptography;
using System.Data;
using Amazon.Runtime;

namespace GitBin.Remotes
{
    public class S3Remote : IRemote
    {
        private const string S3KeyConfigName = "s3key";
        private const string S3SecretKeyConfigName = "s3secretKey";
        private const string S3BucketConfigName = "s3bucket";

        private const int RequestTimeoutInMinutes = 60;
        private const string InvalidAccessKeyErrorCode = "InvalidAccessKeyId";
        private const string InvalidSecurityErrorCode = "InvalidSecurity";

        private readonly string _bucketName;
        private readonly string _key;
        private readonly string _secretKey;
        private readonly AmazonS3Config _s3config;

        private IAmazonS3 _client;

        public S3Remote(IConfigurationProvider configurationProvider)
        {
            _bucketName = configurationProvider.GetString(S3BucketConfigName);
            _key = configurationProvider.GetString(S3KeyConfigName);
            _secretKey = configurationProvider.GetString(S3SecretKeyConfigName);

            AmazonS3Config s3config = new AmazonS3Config();
            s3config.UseHttp = !String.Equals(configurationProvider.Protocol, "HTTPS",
                StringComparison.OrdinalIgnoreCase);
            s3config.RegionEndpoint = RegionEndpoint.GetBySystemName(configurationProvider.S3SystemName);

            _s3config = s3config;
        }

        public GitBinFileInfo[] ListFiles()
        {
            var remoteFiles = new List<GitBinFileInfo>();
            var client = GetClient();

            var listRequest = new ListObjectsRequest();
            listRequest.BucketName = _bucketName;
            listRequest.MaxKeys = 250000; // Arbitrarily large to avoid making multiple round-trips.

            ListObjectsResponse listResponse;

            do
            {
                listResponse = client.ListObjects(listRequest);

                if (listResponse.S3Objects.Any())
                {
                    var keys = listResponse.S3Objects.Select(o => new GitBinFileInfo(o.Key, o.Size));

                    remoteFiles.AddRange(keys);

                    listRequest.Marker = listResponse.NextMarker;
                }
            }
            while (listResponse.IsTruncated);

            return remoteFiles.ToArray();
        }

        public void UploadFile(string sourceFilePath, string destinationFileName)
        {
            string tempDestinationFileName = "partial_" + new Random().Next(0, 10000000) + "." + destinationFileName;
            var client = GetClient();

            // Step 1 - Prepare to upload the chunk to a temp file name
            var putRequest = new PutObjectRequest();
            putRequest.BucketName = _bucketName;
            putRequest.FilePath = sourceFilePath;
            putRequest.Key = tempDestinationFileName;
            putRequest.Timeout = new TimeSpan(0, RequestTimeoutInMinutes, 0);
            putRequest.StreamTransferProgress += (s, args) => ReportProgress(args.PercentDone);

            try
            {
                // Step 2 - Upload the chunk to S3, get the MD5 hash as reported by S3, and compute the MD5 hash
                // locally.
                PutObjectResponse putResponse = client.PutObject(putRequest);
                string remotelyReportedMd5 = putResponse.ETag.Replace("\"", "");
                string locallyCalculatedMd5 = GetMd5Hash(sourceFilePath);

                // Step 3 - Compare the local and remote hashes. If they match, move the uploaded chunk to its final
                // location.
                try
                {
                    if (locallyCalculatedMd5.Equals(remotelyReportedMd5))
                    {
                        CopyObjectRequest copyRequest = new CopyObjectRequest();
                        copyRequest.SourceBucket = _bucketName;
                        copyRequest.SourceKey = tempDestinationFileName;
                        copyRequest.DestinationBucket = _bucketName;
                        copyRequest.DestinationKey = destinationFileName;

                        client.CopyObject(copyRequest);
                    }
                    else
                    {
                        throw new ಠ_ಠ("Chunk '" + destinationFileName + "' was corrupted in transit to S3.");
                    }
                }
                finally
                {
                    //Step 4 - Delete the temp file.
                    DeleteObjectRequest deleteRequest = new DeleteObjectRequest();
                    deleteRequest.BucketName = _bucketName;
                    deleteRequest.Key = tempDestinationFileName;

                    client.DeleteObject(deleteRequest);
                }
            }
            catch (AmazonS3Exception e)
            {
                throw new ಠ_ಠ(GetMessageFromException(e));
            }
        }

        public byte[] DownloadFile(string fileName)
        {
            var client = GetClient();

            var getRequest = new GetObjectRequest();
            getRequest.BucketName = _bucketName;
            getRequest.Key = fileName;

            try
            {
                using (var getResponse = client.GetObject(getRequest))
                {
                    getResponse.WriteObjectProgressEvent += (s, args) => ReportProgress(args.PercentDone);
                    var fileContent = new byte[getResponse.ContentLength];

                    var numberOfBytesRead = 0;
                    var totalBytesRead = 0;

                    do
                    {
                        numberOfBytesRead = getResponse.ResponseStream.Read(fileContent, totalBytesRead, fileContent.Length - totalBytesRead);

                        totalBytesRead += numberOfBytesRead;
                    } while (numberOfBytesRead > 0 && totalBytesRead < fileContent.Length);

                    return fileContent;
                }
            }
            catch (AmazonS3Exception e)
            {
                throw new ಠ_ಠ("Error downloading chunk from S3: " + GetMessageFromException(e));
            }
        }

        public event Action<int> ProgressChanged;

        private IAmazonS3 GetClient()
        {
            if (_client == null)
            {
                _client = AWSClientFactory.CreateAmazonS3Client( _key, _secretKey, _s3config);
            }

            return _client;
        }

        private void ReportProgress(int percentDone)
        {
            if (this.ProgressChanged != null)
            {
                this.ProgressChanged(percentDone);
            }
        }

        private string GetMessageFromException(AmazonS3Exception e)
        {
            if (!String.IsNullOrEmpty(e.ErrorCode) &&
                (e.ErrorCode == InvalidAccessKeyErrorCode ||
                 e.ErrorCode == InvalidSecurityErrorCode))
                return "S3 error: check your access key and secret access key";

            return String.Format("S3 error: code [{0}], message [{1}]", e.ErrorCode, e.Message);
        }

        protected string GetMd5Hash(string fileName)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }
    }
}