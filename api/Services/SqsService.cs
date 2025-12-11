using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;

namespace CloudConverter.Api.Services;

public class SqsService
{
    private readonly IAmazonSQS _sqs;
    private readonly IConfiguration _configuration;

    public SqsService(IAmazonSQS sqs, IConfiguration configuration)
    {
        _sqs = sqs;
        _configuration = configuration;
    }

    public async Task SendMessageAsync(string payload)
    {
        var queueUrl = _configuration["AWS:SQS:QueueUrl"] ?? _configuration["AWS__SQS__QueueUrl"];
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            throw new InvalidOperationException("SQS queue URL is not configured.");
        }

        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = payload
        };

        await _sqs.SendMessageAsync(request);
    }
}

