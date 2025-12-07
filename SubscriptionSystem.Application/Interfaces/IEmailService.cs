using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendAccountDeletionRequestNotificationAsync(string userEmail, string userId);
        Task SendAccountDeletedConfirmationAsync(string userEmail);
        Task SendFailureNotificationAsync(string to, string reason);
       
        Task SendTicketCreationNotificationAsync(string userEmail, int ticketId);
        Task SendTwoFactorSetupEmailAsync(string userEmail, string twoFactorSecret);
        Task SendActiveSubscriptionNotificationAsync(string email, DateTime expiryDate);
        //Task SendPurchaseConfirmationEmailAsync(string email, DateTime expiryDate);
        Task SendPurchaseConfirmationEmailAsync(string email, decimal amount, string currency, DateTime expiryDate);
        Task SendRenewalConfirmationEmailAsync(string email,  DateTime expiryDate);
        Task SendBulkEmailAsync(List<string> recipients, string subject, string body);
        Task SendBulkEmailWithAttachmentAsync(List<string> recipients, string subject, string body, byte[] attachmentData, string attachmentFileName, string attachmentContentType);
        Task SendActiveSubscriptionNotificationAsync(string email, DateTime expiryDate, string currency);
        Task SendAccountDeletionApprovalNotificationAsync(string userId);
        Task SendSubscriptionExpirationReminderAsync(string email, DateTime expiryDate);
    }
}

