using System.Threading.Tasks;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Common;
using Microsoft.AspNetCore.Http;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IGroupChatService
    {
        Task<Result<string>> PostMessageAsync(MessageDto message);
        Task<Result<PagedResult<MessageDto>>> GetMessagesAsync(int page, int pageSize);
        Task<Result<string>> UploadImageAsync(IFormFile file);
        Task<Result<string>> UploadVoiceNoteAsync(IFormFile file);
        Task<Result<bool>> DeleteMessageAsync(string messageId, string userId);
    }
}

