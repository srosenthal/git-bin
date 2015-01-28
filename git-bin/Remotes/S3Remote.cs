using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

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

        private AmazonS3 _client;

        public S3Remote(
            IConfigurationProvider configurationProvider)
        {
            _bucketName = configurationProvider.GetString(S3BucketConfigName);
            _key = configurationProvider.GetString(S3KeyConfigName);
            _secretKey = configurationProvider.GetString(S3SecretKeyConfigName);
        }

        public GitBinFileInfo[] ListFiles()
        {
            var remoteFiles = new List<GitBinFileInfo>();
            var client = GetClient();

            var listRequest = new ListObjectsRequest();
            listRequest.BucketName = _bucketName;

            ListObjectsResponse listResponse;

            do
            {
                listResponse = client.ListObjects(listRequest);

                if (listResponse.S3Objects.Any())
                {
                    var keys = listResponse.S3Objects.Select(o => new GitBinFileInfo(o.Key, o.Size));

                    remoteFiles.AddRange(keys);

                    listRequest.Marker = remoteFiles[remoteFiles.Count - 1].Name;
                }
            }
            while (listResponse.IsTruncated);

            return remoteFiles.ToArray();
        }

        public void UploadFile(string fullPath, string key)
        {
            var client = GetClient();

            var putRequest = new PutObjectRequest();
            putRequest.BucketName = _bucketName;
            putRequest.FilePath = fullPath;
            putRequest.Key = key;
            putRequest.Timeout = RequestTimeoutInMinutes * 60000;
            putRequest.PutObjectProgressEvent += (s, args) => ReportProgress(args);

            try
            {
                PutObjectResponse putResponse = client.PutObject(putRequest);
                putResponse.Dispose();
            }
            catch (AmazonS3Exception e)
            {
                throw new ಠ_ಠ(GetMessageFromException(e));
            }
        }

        public byte[] DownloadFile(string key)
        {
            var client = GetClient();

            var getRequest = new GetObjectRequest();
            getRequest.BucketName = _bucketName;
            getRequest.Key = key;
            getRequest.Timeout = RequestTimeoutInMinutes * 60000;

            try
            {
                using (var getResponse = client.GetObject(getRequest))
                {
                    getResponse.WriteObjectProgressEvent += (s, args) => ReportProgress(args);
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
                throw new ಠ_ಠ(GetMessageFromException(e));
            }
        }

        public event Action<int> ProgressChanged;

        private AmazonS3 GetClient()
        {
            if (_client == null)
            {
                _client = AWSClientFactory.CreateAmazonS3Client(
                    _key,
                    _secretKey);
            }

            return _client;
        }

        private void ReportProgress(TransferProgressArgs args)
        {
            if (this.ProgressChanged != null)
            {
                this.ProgressChanged(args.PercentDone);
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
    }
}