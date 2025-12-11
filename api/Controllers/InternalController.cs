using CloudConverter.Api.Data;
using CloudConverter.Api.DTOs;
using CloudConverter.Api.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CloudConverter.Api.Controllers;

[ApiController]
[Route("api/internal")]
public class InternalController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<VideoHub> _hub;
    private readonly IConfiguration _configuration;

    public InternalController(ApplicationDbContext db, IHubContext<VideoHub> hub, IConfiguration configuration)
    {
        _db = db;
        _hub = hub;
        _configuration = configuration;
    }

    [HttpPost("progress")]
    public async Task<IActionResult> Progress([FromBody] ProgressUpdateRequest request, [FromHeader(Name = "X-Api-Key")] string? apiKey)
    {
        var expectedKey = _configuration["INTERNAL_API_KEY"] ?? _configuration["InternalApiKey"];
        if (string.IsNullOrWhiteSpace(expectedKey) || apiKey != expectedKey)
        {
            return Unauthorized();
        }

        var video = await _db.Videos.FirstOrDefaultAsync(v => v.Id == request.VideoId);
        if (video != null && !string.IsNullOrWhiteSpace(request.Status))
        {
            video.Status = request.Status;
            await _db.SaveChangesAsync();
        }

        await _hub.Clients.All.SendAsync("progress", new
        {
            videoId = request.VideoId,
            status = request.Status,
            percent = request.Percent,
            tags = video?.Tags,
            thumbnailUrl = video?.ThumbnailUrl
        });

        return Ok();
    }
}

