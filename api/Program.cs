using Amazon;
using Amazon.S3;
using Amazon.SQS;
using CloudConverter.Api.Data;
using CloudConverter.Api.Hubs;
using CloudConverter.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var connectionString = builder.Configuration.GetConnectionString("Default")
                     ?? builder.Configuration["ConnectionStrings__Default"]
                     ?? "server=localhost;port=3306;database=cloudconverter;user=root;password=root";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

var clientOrigin = builder.Configuration["CLIENT_URL"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientCors", policy => policy
        .WithOrigins(clientOrigin)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var awsRegion = builder.Configuration["AWS:Region"] ?? builder.Configuration["AWS__Region"] ?? "us-east-1";
var awsServiceUrl = builder.Configuration["AWS:ServiceUrl"] ?? builder.Configuration["AWS__ServiceUrl"];

builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var config = new AmazonS3Config
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(awsRegion)
    };

    if (!string.IsNullOrWhiteSpace(awsServiceUrl))
    {
        config.ServiceURL = awsServiceUrl;
        config.ForcePathStyle = true;
    }

    return new AmazonS3Client(config);
});

builder.Services.AddSingleton<IAmazonSQS>(_ =>
{
    var config = new AmazonSQSConfig
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(awsRegion)
    };

    if (!string.IsNullOrWhiteSpace(awsServiceUrl))
    {
        config.ServiceURL = awsServiceUrl;
    }

    return new AmazonSQSClient(config);
});

builder.Services.AddScoped<S3Service>();
builder.Services.AddScoped<SqsService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    try
    {
        // Ensure Tags column exists (MySQL prior to 8.0.29 may not support IF NOT EXISTS)
        db.Database.ExecuteSqlRaw("ALTER TABLE Videos ADD COLUMN Tags longtext NULL;");
    }
    catch
    {
        // ignore if column already exists or table not created yet
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("ClientCors");
app.UseAuthorization();
app.MapControllers();
app.MapHub<VideoHub>("/hub/video");

app.Run();

