namespace AWS3.Models;

public class FileDirectoryEntry
{
    public string Id { get; set; }
    public string FileName { get; set; }
    public string ObjectKey { get; set; }
    public string BucketName { get; set; }
    public string ContentType { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadDate { get; set; }
    public string UploadedBy { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}