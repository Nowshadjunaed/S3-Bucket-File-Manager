using Amazon.Util.Internal;
using AWS3.Models;
using AWS3.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace S3FileManager.Controllers
{
   [ApiController]
[Route("api/[controller]")]
public class S3FileManagerController : ControllerBase
{
    private readonly ILogger<S3FileManagerController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IStorageService _storageService;
    private readonly IFileDirectoryService _fileDirectoryService;
    private readonly string _defaultBucketName;

    public S3FileManagerController(
        ILogger<S3FileManagerController> logger,
        IConfiguration configuration,
        IStorageService storageService,
        IFileDirectoryService fileDirectoryService)
    {
        _logger = logger;
        _configuration = configuration;
        _storageService = storageService;
        _fileDirectoryService = fileDirectoryService;
        _defaultBucketName = _configuration["AWSConfiguration:DefaultBucketName"];
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        try
        {
            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var directoryEntryId = Guid.NewGuid().ToString();
            var fileId = Guid.NewGuid().ToString();
            var objectKey = $"{directoryEntryId}/{fileId}{Path.GetExtension(file.FileName)}";

            var s3Obj = new S3Object
            {
                BucketName = _defaultBucketName,
                InpuStream = memoryStream,
                Name = objectKey
            };

            var credentials = GetAwsCredentials();

            var result = await _storageService.UploadFileAsync(s3Obj, credentials);

            if (result.StatusCode == 200)
            {
                var fileEntry = new FileDirectoryEntry
                {
                    Id = directoryEntryId,
                    FileName = file.FileName,
                    ObjectKey = objectKey,
                    BucketName = _defaultBucketName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    UploadDate = DateTime.UtcNow,
                    UploadedBy = "Junaed",
                    Metadata = new Dictionary<string, string>
                    {
                        { "OriginalFileName", file.FileName },
                        { "Extension", Path.GetExtension(file.FileName) }
                    }
                };

                await _fileDirectoryService.CreateFileEntryAsync(fileEntry);

                return Ok(new
                {
                    result.StatusCode,
                    result.Message,
                    FileEntryId = fileEntry.Id,
                    FileEntry = fileEntry
                });
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload failed.");
            return StatusCode(500, new { Message = "Internal server error." });
        }
    }

    [HttpGet("download/{directoryEntryId}")]
    public async Task<IActionResult> DownloadFile(string directoryEntryId)
    {
        if (string.IsNullOrWhiteSpace(directoryEntryId))
            return BadRequest(new { Message = "File ID is required." });

        var fileEntry = await _fileDirectoryService.GetFileEntryByIdAsync(directoryEntryId);
        if (fileEntry == null)
            return NotFound(new { Message = "File not found." });

        var credentials = GetAwsCredentials();
        var downloadResult = await _storageService.DownloadFileAsync(fileEntry.ObjectKey, fileEntry.BucketName, credentials);

        if (downloadResult == null)
            return NotFound(new { Message = "File not found in S3." });

        var (fileStream, contentType, _) = downloadResult.Value;
        return File(fileStream, fileEntry.ContentType ?? contentType ?? "application/octet-stream", fileEntry.FileName);
    }

    [HttpDelete("delete/{directoryEntryId}")]
    public async Task<IActionResult> DeleteFile(string directoryEntryId)
    {
        if (string.IsNullOrWhiteSpace(directoryEntryId))
            return BadRequest(new { Message = "File ID is required." });

        var fileEntry = await _fileDirectoryService.GetFileEntryByIdAsync(directoryEntryId);
        if (fileEntry == null)
            return NotFound(new { Message = "File not found." });

        var credentials = GetAwsCredentials();
        var deleteResult = await _storageService.DeleteFileAsync(fileEntry.ObjectKey, fileEntry.BucketName, credentials);

        if (deleteResult.StatusCode == 200 || deleteResult.StatusCode == 204)
        {
            var dbDeleted = await _fileDirectoryService.DeleteFileEntryAsync(directoryEntryId);
            if (dbDeleted)
                return Ok(new { Message = "File deleted successfully." });

            return StatusCode(500, new { Message = "File deleted from S3, but database cleanup failed." });
        }

        return StatusCode(deleteResult.StatusCode, deleteResult);
    }

    [HttpPost("copy")]
    public async Task<IActionResult> CopyBetweenBuckets([FromBody] InterBucketCopyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourceFileName) ||
            string.IsNullOrWhiteSpace(request.SourceBucket) ||
            string.IsNullOrWhiteSpace(request.DestinationBucket))
        {
            return BadRequest(new { Message = "All fields are required." });
        }

        var sourceEntry = await _fileDirectoryService.GetFileEntryByObjectKeyAsync(request.SourceFileName);
        if (sourceEntry == null)
            return NotFound(new { Message = "Source file not found in metadata." });

        var credentials = GetAwsCredentials();
        var copyResult = await _storageService.CopyFileAsync(
            request.SourceFileName,
            request.SourceFileName,
            request.SourceBucket,
            request.DestinationBucket,
            credentials);

        if (copyResult.StatusCode is 200 or 201)
        {
            var newEntry = new FileDirectoryEntry
            {
                Id = Guid.NewGuid().ToString(),
                FileName = sourceEntry.FileName,
                ObjectKey = request.SourceFileName,
                BucketName = request.DestinationBucket,
                ContentType = sourceEntry.ContentType,
                FileSize = sourceEntry.FileSize,
                UploadDate = DateTime.UtcNow,
                UploadedBy = sourceEntry.UploadedBy,
                Metadata = new Dictionary<string, string>
                {
                    { "OriginalFileName", sourceEntry.FileName },
                    { "CopiedFrom", $"{request.SourceBucket}/{request.SourceFileName}" },
                    { "Extension", Path.GetExtension(sourceEntry.FileName) }
                }
            };

            await _fileDirectoryService.CreateFileEntryAsync(newEntry);

            return Ok(new
            {
                copyResult.StatusCode,
                copyResult.Message,
                CopiedFileEntryId = newEntry.Id,
                FileEntry = newEntry
            });
        }

        return StatusCode(copyResult.StatusCode, copyResult);
    }

    private AWSCredentials GetAwsCredentials() => new()
    {
        AwsKey = _configuration["AWSConfiguration:AWSAccessKey"],
        AwsSecretKey = _configuration["AWSConfiguration:AWSSecretKey"]
    };
}

}
