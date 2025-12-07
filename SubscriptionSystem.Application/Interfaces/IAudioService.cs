using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IAudioService
    {
        // Synthesizes the given text to speech, uploads to S3, and returns a public or pre-signed URL.
        Task<string> SynthesizeToS3Async(string text, string? voiceName, string s3Prefix, CancellationToken ct = default);
    }
}
