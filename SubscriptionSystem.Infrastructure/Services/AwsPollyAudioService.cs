using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Polly;
using Amazon.Polly.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SubscriptionSystem.Infrastructure.Services
{
    public class AwsPollyAudioService : IAudioService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AwsPollyAudioService> _logger;

        public AwsPollyAudioService(IConfiguration config, ILogger<AwsPollyAudioService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<string> SynthesizeToS3Async(string text, string? voiceName, string s3Prefix, CancellationToken ct = default)
        {
            var accessKey = _config["AWS:AccessKeyId"];
            var secretKey = _config["AWS:SecretAccessKey"];
            // Support human-friendly config values like "Europe (Frankfurt) eu-central-1" and fall back to env var/region default
            var regionStr = _config["AWS:Region"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-central-1";
            // Extract the actual region token if the string contains spaces
            if (regionStr.Contains(' '))
            {
                var parts = regionStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                regionStr = parts[^1];
            }
            var bucket = _config["AWS:S3:Bucket"];

            if (string.IsNullOrWhiteSpace(bucket))
                throw new InvalidOperationException("AWS configuration is missing. Please set AWS:S3:Bucket.");

            var region = RegionEndpoint.GetBySystemName(regionStr);

            // Prefer explicit keys if provided; otherwise use the default credentials chain (e.g., App Runner IAM role)
            AmazonPollyClient polly;
            AmazonS3Client s3;
            if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
            {
                var creds = new BasicAWSCredentials(accessKey, secretKey);
                polly = new AmazonPollyClient(creds, region);
                s3 = new AmazonS3Client(creds, region);
            }
            else
            {
                polly = new AmazonPollyClient(region);
                s3 = new AmazonS3Client(region);
            }

            var voice = string.IsNullOrWhiteSpace(voiceName) ? "Matthew" : voiceName; // default energetic male
            var synthReq = new SynthesizeSpeechRequest
            {
                Engine = Engine.Neural,
                OutputFormat = OutputFormat.Mp3,
                Text = text,
                VoiceId = voice
            };

            using var synthResp = await polly.SynthesizeSpeechAsync(synthReq, ct);
            using var audioStream = synthResp.AudioStream;

            var key = $"{s3Prefix.Trim('/')}/{Guid.NewGuid():N}.mp3";
            var putReq = new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = audioStream,
                ContentType = "audio/mpeg"
            };

            // Friendly caching for static audio while remaining short-lived via presigned URL
            putReq.Headers.CacheControl = "public, max-age=86400"; // 24h
            putReq.Metadata["x-amz-meta-source"] = "chat-ai";

            await s3.PutObjectAsync(putReq, ct);

            // Generate a presigned URL with configurable expiry (default 24h)
            var expiryHours = Math.Max(1, _config.GetValue<int?>("Voice:PresignedUrlHours") ?? 24);
            var urlReq = new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = key,
                Expires = DateTime.UtcNow.AddHours(expiryHours)
            };
            var url = s3.GetPreSignedURL(urlReq);
            return url;
        }
    }
}
