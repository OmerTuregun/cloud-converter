namespace CloudConverter.Api.DTOs;

public class UploadCompleteRequest
{
    public string FileName { get; set; } = string.Empty;
    public string S3Key { get; set; } = string.Empty;
}

