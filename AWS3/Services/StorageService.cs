using Amazon.Runtime;
using AWS3.Models;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System.Net;
using AWSCredentials = AWS3.Models.AWSCredentials;
using S3Object = AWS3.Models.S3Object;

namespace AWS3.Services
{
    public class StorageService : IStorageService
    {
        private readonly ILogger<StorageService> _logger;
        private readonly AmazonS3Config _s3Config;

        public StorageService(ILogger<StorageService> logger)
        {
            _logger = logger;
            _s3Config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.EUNorth1
            };
        }

        private AmazonS3Client CreateClient(AWSCredentials credentials)
        {
            var awsCredentials = new BasicAWSCredentials(credentials.AwsKey, credentials.AwsSecretKey);
            return new AmazonS3Client(awsCredentials, _s3Config);
        }

        public async Task<S3ResponseDTO> UploadFileAsync(S3Object s3Obj, AWSCredentials awsCredentials)
        {
            var response = new S3ResponseDTO();

            try
            {
                using var client = CreateClient(awsCredentials);
                using var transferUtility = new TransferUtility(client);

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = s3Obj.InpuStream,
                    Key = s3Obj.Name,
                    BucketName = s3Obj.BucketName,
                    CannedACL = S3CannedACL.NoACL
                };

                await transferUtility.UploadAsync(uploadRequest);

                response.StatusCode = 200;
                response.Message = $"{s3Obj.Name} uploaded successfully.";
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "AmazonS3 error during upload");
                response.StatusCode = (int)ex.StatusCode;
                response.Message = $"S3 error: {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during file upload");
                response.StatusCode = 500;
                response.Message = $"Internal error: {ex.Message}";
            }

            return response;
        }

        public async Task<(MemoryStream FileStream, string ContentType, string FileName)?> DownloadFileAsync(string fileName, string bucketName, AWSCredentials awsCredentials)
        {
            try
            {
                using var client = CreateClient(awsCredentials);
                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileName
                };

                using var response = await client.GetObjectAsync(request);

                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    var memoryStream = new MemoryStream();
                    await response.ResponseStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    return (memoryStream, response.Headers.ContentType ?? "application/octet-stream", fileName);
                }

                return null;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File not found in S3: {FileName}", fileName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from S3");
                return null;
            }
        }

        public async Task<S3ResponseDTO> DeleteFileAsync(string fileName, string bucketName, AWSCredentials awsCredentials)
        {
            var response = new S3ResponseDTO();

            try
            {
                using var client = CreateClient(awsCredentials);

                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileName
                };

                var deleteResult = await client.DeleteObjectAsync(deleteRequest);

                response.StatusCode = (int)deleteResult.HttpStatusCode;
                response.Message = $"{fileName} deleted from S3.";
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "AmazonS3 error during delete");
                response.StatusCode = (int)ex.StatusCode;
                response.Message = $"S3 error: {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during file deletion");
                response.StatusCode = 500;
                response.Message = $"Internal error: {ex.Message}";
            }

            return response;
        }

        public async Task<S3ResponseDTO> CopyFileAsync(string sourceFileName, string destinationFileName, string sourceBucket, string destinationBucket, AWSCredentials awsCredentials)
        {
            var response = new S3ResponseDTO();

            try
            {
                using var client = CreateClient(awsCredentials);

                var copyRequest = new CopyObjectRequest
                {
                    SourceBucket = sourceBucket,
                    SourceKey = sourceFileName,
                    DestinationBucket = destinationBucket,
                    DestinationKey = destinationFileName
                };

                var copyResult = await client.CopyObjectAsync(copyRequest);

                response.StatusCode = (int)copyResult.HttpStatusCode;
                response.Message = $"{sourceFileName} copied to {destinationBucket}/{destinationFileName}.";
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "AmazonS3 error during copy");
                response.StatusCode = (int)ex.StatusCode;
                response.Message = $"S3 error: {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during file copy");
                response.StatusCode = 500;
                response.Message = $"Internal error: {ex.Message}";
            }

            return response;
        }
    }
}
