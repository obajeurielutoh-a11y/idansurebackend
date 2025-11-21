using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface ITranscriptionService
    {
        // Transcribe audio stream to text. fileName helps infer MIME/extension for APIs.
        Task<string> TranscribeAsync(Stream audioStream, string fileName, string? language = null, CancellationToken ct = default);
    }
}
