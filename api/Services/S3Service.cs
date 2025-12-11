using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace CloudConverter.Api.Services;

public class S3Service
{
    private readonly IAmazonS3 _s3;
    private readonly IConfiguration _configuration;

    public S3Service(IAmazonS3 s3, IConfiguration configuration)
    {
        _s3 = s3;
        _configuration = configuration;
    }

    public async Task<(string uploadUrl, string key)> GeneratePresignedUploadUrlAsync(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required", nameof(fileName));
        }

        var bucket = _configuration["AWS:S3:BucketName"] ?? _configuration["AWS__S3__BucketName"];
        if (string.IsNullOrWhiteSpace(bucket))
        {
            throw new InvalidOperationException("S3 bucket name is not configured.");
        }

        var objectKey = $"videos/{Guid.NewGuid():N}_{fileName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(15),
            ContentType = "application/octet-stream"
        };

        var url = _s3.GetPreSignedURL(request);
        return (url, objectKey);
    }
}

