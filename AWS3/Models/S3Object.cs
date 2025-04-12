namespace AWS3.Models;
public class S3Object
{
    public String Name { get; set; }
    public MemoryStream InpuStream { get; set; }
    public String BucketName { get; set; }
}