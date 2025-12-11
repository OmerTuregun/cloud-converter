using Amazon;
using Amazon.S3;
using Amazon.SQS;
using CloudConverter.Api.Data;
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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin());
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
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();

