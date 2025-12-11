namespace CloudConverter.Api.Models;

public class Video
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = "Processing";
    public string S3Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? Tags { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

