namespace AWS3.Models;

public class InterBucketCopyRequest
{
    public string SourceFileName { get; set; }
    public string SourceBucket { get; set; }
    public string DestinationBucket { get; set; }
}