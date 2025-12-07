using SubscriptionSystem.Domain.Entities;
namespace SubscriptionSystem.Application.Interfaces
{
    public interface ICommentRepository
    {
        Task<Comment> GetByIdAsync(string id);
        Task<IEnumerable<Comment>> GetAllAsync();
        Task<IEnumerable<Comment>> GetPagedAsync(int page, int pageSize, string groupId = null);
        Task<int> GetTotalCountAsync(string groupId = null);
        Task<string> AddAsync(Comment comment);
        Task UpdateAsync(Comment comment);
        Task DeleteAsync(string id);
        
    }
}