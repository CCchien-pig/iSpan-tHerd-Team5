using Amazon.S3;
using Amazon.S3.Model;
using Syncfusion.EJ2.FileManager.Base;
using System.IO;

namespace tHerdBackend.Infra.Providers
{
    public class S3FileProvider
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3FileProvider(string accessKey, string secretKey, string region, string bucketName)
        {
            _s3Client = new AmazonS3Client(accessKey, secretKey, Amazon.RegionEndpoint.GetBySystemName(region));
            _bucketName = bucketName;
        }

        public async Task<IEnumerable<FileManagerDirectoryContent>> GetFilesAsync(string prefix = "")
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = prefix
            };
            var response = await _s3Client.ListObjectsV2Async(request);

            return response.S3Objects.Select(o => new FileManagerDirectoryContent
            {
                Name = Path.GetFileName(o.Key),
                Type = o.Key.EndsWith("/") ? "Folder" : "File",
                Size = o.Size ?? 0, // ✅ null 則設為 0
                DateModified = o.LastModified ?? DateTime.UtcNow // ✅ null 則設為現在時間
            });
        }

        public async Task UploadAsync(string key, Stream fileStream, string contentType)
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = contentType
            };
            await _s3Client.PutObjectAsync(putRequest);
        }

        public async Task DeleteAsync(string key)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };
            await _s3Client.DeleteObjectAsync(deleteRequest);
        }
    }
}
