using System.Text.Json;
using CloudConverter.Api.Data;
using CloudConverter.Api.DTOs;
using CloudConverter.Api.Models;
using CloudConverter.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudConverter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly S3Service _s3Service;
    private readonly SqsService _sqsService;
    private readonly IConfiguration _configuration;

    public UploadController(ApplicationDbContext db, S3Service s3Service, SqsService sqsService, IConfiguration configuration)
    {
        _db = db;
        _s3Service = s3Service;
        _sqsService = sqsService;
        _configuration = configuration;
    }

    [HttpPost("init")]
    public async Task<ActionResult<UploadInitResponse>> InitUpload([FromBody] UploadInitRequest request)
    {
        try
        {
            var (uploadUrl, key) = await _s3Service.GeneratePresignedUploadUrlAsync(request.FileName);
            return Ok(new UploadInitResponse { UploadUrl = uploadUrl, S3Key = key });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("complete")]
    public async Task<ActionResult> CompleteUpload([FromBody] UploadCompleteRequest request)
    {
        try
        {
            var bucket = _configuration["AWS:S3:BucketName"] ?? _configuration["AWS__S3__BucketName"];
            var video = new Video
            {
                FileName = request.FileName,
                Status = "Processing",
                S3Url = $"s3://{bucket}/{request.S3Key}",
                CreatedAt = DateTime.UtcNow
            };

            _db.Videos.Add(video);
            await _db.SaveChangesAsync();

            var payload = JsonSerializer.Serialize(new
            {
                videoId = video.Id,
                s3Key = request.S3Key
            });

            await _sqsService.SendMessageAsync(payload);
            return Accepted(new { video.Id, video.Status });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("/api/videos")]
    public async Task<ActionResult<IEnumerable<Video>>> GetVideos()
    {
        var videos = await _db.Videos.AsNoTracking()
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();
        return Ok(videos);
    }
}

