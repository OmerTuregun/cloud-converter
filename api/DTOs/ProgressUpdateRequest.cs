namespace CloudConverter.Api.DTOs;

public class ProgressUpdateRequest
{
    public int VideoId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? Percent { get; set; }
}

