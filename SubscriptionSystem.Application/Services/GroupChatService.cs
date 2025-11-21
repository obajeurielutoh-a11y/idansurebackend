using Microsoft.AspNetCore.Http;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Common;
using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Application.Services
{
    public class GroupChatService : IGroupChatService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly string _imageUploadPath = "wwwroot/uploads/images";
        private readonly string _voiceNoteUploadPath = "wwwroot/uploads/voicenotes";

        public GroupChatService(ICommentRepository commentRepository)
        {
            _commentRepository = commentRepository;
        }

        public async Task<Result<string>> PostMessageAsync(MessageDto message)
        {
            try
            {
                message.Id = GenerateCustomId();

                var newComment = new Comment
                {
                    Id = message.Id,
                    UserId = message.UserId,
                    Content = message.Content,
                    Timestamp = message.Timestamp,
                    ParentId = message.ParentId,
                    ImageUrl = message.ImageUrl,
                    VoiceNoteUrl = message.VoiceNoteUrl
                };

                var commentId = await _commentRepository.AddAsync(newComment);
                return Result<string>.Success(commentId);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"An error occurred while posting the message: {ex.Message}");
            }
        }

        public async Task<Result<PagedResult<MessageDto>>> GetMessagesAsync(int page, int pageSize)
        {
            try
            {
                var comments = await _commentRepository.GetPagedAsync(page, pageSize);
                var totalCount = await _commentRepository.GetTotalCountAsync();

                var messageDtos = comments.Select(c => new MessageDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    Content = c.Content,
                    Timestamp = c.Timestamp,
                    ParentId = c.ParentId,
                    ImageUrl = c.ImageUrl,
                    VoiceNoteUrl = c.VoiceNoteUrl
                }).ToList();

                var pagedResult = new PagedResult<MessageDto>
                {
                    Items = messageDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return Result<PagedResult<MessageDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                return Result<PagedResult<MessageDto>>.Failure($"An error occurred while retrieving messages: {ex.Message}");
            }
        }

        public async Task<Result<string>> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length < 20 * 1024) // Check if file is at least 20KB
            {
                return Result<string>.Failure("File is too small or not provided. Minimum size is 20KB.");
            }

            try
            {
                return await UploadFileAsync(file, _imageUploadPath, "image");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"An error occurred while uploading the image: {ex.Message}");
            }
        }

        public async Task<Result<string>> UploadVoiceNoteAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Result<string>.Failure("Voice note file is required.");
            }

            try
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(_voiceNoteUploadPath, fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Result<string>.Success($"/uploads/voicenotes/{fileName}");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"An error occurred while uploading the voice note: {ex.Message}");
            }
        }

        private async Task<Result<string>> UploadFileAsync(IFormFile file, string uploadPath, string fileType)
        {
            // Ensure the upload directory exists
            var fullUploadPath = Path.Combine(Directory.GetCurrentDirectory(), uploadPath);
            if (!Directory.Exists(fullUploadPath))
            {
                Directory.CreateDirectory(fullUploadPath);
            }

            // Generate a unique filename
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(fullUploadPath, fileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the relative path to the file
            return Result<string>.Success($"/uploads/{fileType}s/{fileName}");
        }

        public async Task<Result<bool>> DeleteMessageAsync(string messageId, string userId)
        {
            try
            {
                var message = await _commentRepository.GetByIdAsync(messageId);
                if (message == null)
                {
                    return Result<bool>.Failure("Message not found.");
                }

                if (message.UserId != userId)
                {
                    return Result<bool>.Failure("You don't have permission to delete this message.");
                }

                await _commentRepository.DeleteAsync(messageId);

                // Delete associated files if they exist
                DeleteFileIfExists(message.ImageUrl);
                DeleteFileIfExists(message.VoiceNoteUrl);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred while deleting the message: {ex.Message}");
            }
        }

        private void DeleteFileIfExists(string fileUrl)
        {
            if (!string.IsNullOrEmpty(fileUrl))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileUrl.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        private static readonly string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        private static readonly Random _random = new Random();

        private string GenerateCustomId()
        {
            var result = new char[5];
            for (int i = 0; i < 5; i++)
            {
                result[i] = Alphabet[_random.Next(Alphabet.Length)];
            }
            return $"idan-{new string(result)}".ToLower();
        }


    }
}

