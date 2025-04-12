using AWS3.Models;

namespace AWS3.Services;

public interface IStorageService
{
    Task<S3ResponseDTO> UploadFileAsync(S3Object s3Obj, AWSCredentials awsCredentials);
    Task<(MemoryStream FileStream, string ContentType, string FileName)?> DownloadFileAsync(string fileName, string bucketName, AWSCredentials awsCredentials);
    Task<S3ResponseDTO> DeleteFileAsync(string fileName, string bucketName, AWSCredentials awsCredentials);
    Task<S3ResponseDTO> CopyFileAsync(string sourceFileName, string destinationFileName, string sourceBucket, string destinationBucket, AWSCredentials awsCredentials);



}