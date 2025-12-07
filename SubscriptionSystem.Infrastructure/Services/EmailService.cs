using System;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;

namespace SubscriptionSystem.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
    private readonly string? _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _smtpUsername;
    private readonly string? _smtpPassword;
    private readonly string? _fromEmail;
    private readonly string? _fromName;
    private readonly string? _logoUrl;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _smtpHost = configuration["EmailSettings:SmtpHost"];
            if (!int.TryParse(configuration["EmailSettings:SmtpPort"], out _smtpPort))
            {
                _smtpPort = 587; // safe default
            }
            _smtpUsername = configuration["EmailSettings:SmtpUsername"];
            _smtpPassword = configuration["EmailSettings:SmtpPassword"];
            _fromEmail = configuration["EmailSettings:FromEmail"];
            _fromName = configuration["EmailSettings:FromName"];
            _logoUrl = configuration["EmailSettings:LogoUrl"];
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // If SMTP is not configured, log and no-op to avoid breaking the request pipeline
            if (string.IsNullOrWhiteSpace(_smtpHost) || string.IsNullOrWhiteSpace(_fromEmail))
            {
                _logger?.LogWarning("EmailService: SMTP not configured (host/from). Skipping email to {To} with subject '{Subject}'.", to, subject);
                return;
            }

            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName ?? string.Empty),
                    Subject = subject,
                    Body = CreateEmailBody(subject, body),
                    IsBodyHtml = true
                };
                message.To.Add(to);

                using (var client = new SmtpClient(_smtpHost, _smtpPort))
                {
                    client.UseDefaultCredentials = false;
                    if (!string.IsNullOrEmpty(_smtpUsername))
                    {
                        client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    }
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    await client.SendMailAsync(message);
                }
            }
            catch (SmtpException smtpEx)
            {
                // Graceful fallback: log and continue without throwing
                _logger?.LogError(smtpEx, "EmailService: SMTP send failed (host={Host}, port={Port}). Email to {To} subject '{Subject}'.", _smtpHost, _smtpPort, to, subject);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "EmailService: Unexpected error sending email to {To} subject '{Subject}'.", to, subject);
            }
        }

        public async Task SendActiveSubscriptionNotificationAsync(string email, DateTime expiryDate, string currency)
        {

            var subject = "Active Subscription Notification";
            var body = $"You already have an active subscription paid in {currency}. " +
                       $"Your current subscription will expire on {expiryDate:yyyy-MM-dd HH:mm:ss} UTC.";

            await SendEmailAsync(email, subject, body);


        }
        public async Task SendBulkEmailAsync(List<string> recipients, string subject, string body)
        {
            foreach (var recipient in recipients)
            {
                await SendEmailAsync(recipient, subject, body);
            }
        }
        public async Task SendBulkEmailWithAttachmentAsync(List<string> recipients, string subject, string body, byte[] attachmentData, string attachmentFileName, string attachmentContentType)
        {
            foreach (var recipient in recipients)
            {
                await SendEmailWithAttachmentAsync(recipient, subject, body, attachmentData, attachmentFileName, attachmentContentType);
            }
        }

        public async Task SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachmentData, string attachmentFileName, string attachmentContentType)
        {
            // If SMTP is not configured, log and no-op to avoid breaking the request pipeline
            if (string.IsNullOrWhiteSpace(_smtpHost) || string.IsNullOrWhiteSpace(_fromEmail))
            {
                _logger?.LogWarning("EmailService: SMTP not configured (host/from). Skipping email with attachment to {To} with subject '{Subject}'.", to, subject);
                return;
            }

            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName ?? string.Empty),
                    Subject = subject,
                    Body = CreateEmailBody(subject, body),
                    IsBodyHtml = true
                };
                message.To.Add(to);

                var attachment = new Attachment(new MemoryStream(attachmentData), attachmentFileName, attachmentContentType);
                message.Attachments.Add(attachment);

                using (var client = new SmtpClient(_smtpHost, _smtpPort))
                {
                    client.UseDefaultCredentials = false;
                    if (!string.IsNullOrEmpty(_smtpUsername))
                    {
                        client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    }
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    await client.SendMailAsync(message);
                }
            }
            catch (SmtpException smtpEx)
            {
                // Graceful fallback: log and continue without throwing
                _logger?.LogError(smtpEx, "EmailService: SMTP send with attachment failed (host={Host}, port={Port}). Email to {To} subject '{Subject}'.", _smtpHost, _smtpPort, to, subject);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "EmailService: Unexpected error sending email with attachment to {To} subject '{Subject}'.", to, subject);
            }
        }


        public async Task SendAccountDeletionApprovalNotificationAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email address cannot be null or empty.", nameof(email));
            }

            var subject = "Account Deletion Approved";
            var body = "Your account deletion request has been approved and processed. Your account has been permanently deleted from our system.";

            await SendEmailAsync(email, subject, body);
        }
        
        public async Task SendAccountDeletionRequestNotificationAsync(string userId, string recipientEmail)
        {
            if (!IsValidEmail(recipientEmail))
            {
                throw new ArgumentException("Invalid recipient email address", nameof(recipientEmail));
            }

            string subject = "Account Deletion Request";
            string body = $"A request has been made to delete the account associated with user ID: {userId}. Please review this request.";

            await SendEmailAsync(recipientEmail, subject, body);
        }
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public async Task SendPurchaseConfirmationEmailAsync(string email, decimal amount, string currency, DateTime expiryDate)
        {


            var subject = "Subscription Purchase Confirmation";
            var body = $"Thank you for your purchase! Your subscription for {amount} {currency} has been confirmed. " +
                       $"Your subscription will expire on {expiryDate:yyyy-MM-dd HH:mm:ss} UTC.";

            await SendEmailAsync(email, subject, body);

            
        }
        public async Task SendTwoFactorSetupEmailAsync(string userEmail, string twoFactorSecret)
        {
            var subject = "Two-Factor Authentication Setup";
            var body = $@"
                <h2>Two-Factor Authentication Setup</h2>fv
                <p>Your two-factor authentication has been enabled. Please use the following secret to set up your authenticator app:</p>
                <p><strong>{twoFactorSecret}</strong></p>
                <p>If you didn't request this change, please contact our support team immediately.</p>";

            await SendEmailAsync(userEmail, subject, body);
        }

        public async Task SendAccountDeletedConfirmationAsync(string userEmail)
        {
            var subject = "Account Deleted Confirmation";
            var body = @"
                <h2>Account Deleted Confirmation</h2>
                <p>Your account has been successfully deleted as per your request.</p>
                <p>If you believe this was done in error, please contact our support team.</p>";

            await SendEmailAsync(userEmail, subject, body);
        }
        public async Task SendTicketCreationNotificationAsync(string userEmail, int ticketId)
        {
            var subject = "Ticket Created";
            var body = $@"
                <h2>Ticket Created</h2>
                <p>Your ticket (ID: {ticketId}) has been successfully created.</p>
                <p>We will notify you via email once your ticket has been treated.</p>
                <p>Thank you for your patience.</p>";

            await SendEmailAsync(userEmail, subject, body);
        }
        private string CreateEmailBody(string subject, string content)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{subject}</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
    <table role='presentation' style='width: 100%; border-collapse: collapse;'>
        <tr>
            <td align='center' style='padding: 0;'>
                <table role='presentation' style='width: 602px; border-collapse: collapse; border: 1px solid #cccccc; background-color: #ffffff;'>
                    <tr>
                        <td align='center' style='padding: 40px 0 30px 0;'>
                            <img src='{_logoUrl}' alt='Company Logo' style='height: 60px; max-width: 300px;' />
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 36px 30px 42px 30px;'>
                            <table role='presentation' style='width: 100%; border-collapse: collapse;'>
                                <tr>
                                    <td style='padding: 0 0 36px 0; color: #153643;'>
                                        <h1 style='font-size: 24px; margin: 0 0 20px 0; font-family: Arial, sans-serif;'>{subject}</h1>
                                        <p style='margin: 0 0 12px 0; font-size: 16px; line-height: 24px;'>{content}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 30px; background-color: #153643;'>
                            <table role='presentation' style='width: 100%; border-collapse: collapse;'>
                                <tr>
                                    <td style='padding: 0; color: #ffffff;'>
                                        <p style='margin: 0; font-size: 14px; line-height: 20px;'>&reg; IdanSure Limited, 2025<br/>
                                        <a href='https://www.IdanSure.com' style='color: #ffffff; text-decoration: underline;'>https://www.IdanSure.com</a></p>
                                    </td>
                                    <td style='padding: 0; text-align: right;'>
                                        <table role='presentation' style='border-collapse: collapse;'>
                                            <tr>
                                                <td style='padding: 0 0 0 10px;'>
                                                    <a href='http://www.twitter.com/' style='color: #ffffff;'><img src='https://assets.codepen.io/210284/tw_1.png' alt='Twitter' width='38' style='height: auto; display: block; border: 0;' /></a>
                                                </td>
                                                <td style='padding: 0 0 0 10px;'>
                                                    <a href='http://www.facebook.com/idansure' style='color: #ffffff;'><img src='https://assets.codepen.io/210284/fb_1.png' alt='Facebook' width='38' style='height: auto; display: block; border: 0;' /></a>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        public async Task SendSubscriptionExpirationReminderAsync(string email, DateTime expiryDate)
        {
            var subject = "Subscription Expiration Reminder";
            var body = $@"
        <h2>Subscription Expiration Reminder</h2>
        <p>Dear valued customer,</p>
        <p>This is a friendly reminder that your subscription is set to expire soon.</p>
        <p>Your subscription will expire on: <strong>{expiryDate:MMMM dd, yyyy}</strong></p>
        <p>To avoid any interruption in service, please renew your subscription before the expiration date.</p>
        <p>If you have any questions or need assistance, please don't hesitate to contact our support team.</p>
        <p>Thank you for your continued support!</p>";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendFailureNotificationAsync(string to, string reason)
        {
            string subject = "Subscription Payment Failed";
            string body = $"Your subscription payment has failed. Reason: {reason}";
            await SendEmailAsync(to, subject, body);
        }

        public async Task SendRenewalConfirmationEmailAsync(string email, DateTime renewalDate)
        {
            string subject = "Subscription Renewal Confirmation";
            string body = $"Your subscription has been successfully renewed and will be active until {renewalDate:MMMM dd, yyyy}.";
            await SendEmailAsync(email, subject, body);
        }


        public async Task SendConfirmationEmailAsync(string email)
        {
            string subject = "Subscription Confirmation";
            string body = "Thank you for purchasing a subscription!";
            await SendEmailAsync(email, subject, body);
        }

        public async Task SendExpirationNotificationAsync(string email)
        {
            string subject = "Subscription Expiration Notice";
            string body = "Your subscription has expired. Please renew it.";
            await SendEmailAsync(email, subject, body);
        }
        public async Task SendActiveSubscriptionNotificationAsync(string email, DateTime expiryDate)
        {
            var subject = "Active Subscription Notification";
            var body = $@"
                <h2>Active Subscription Notification</h2>
                <p>Dear valued customer,</p>
                <p>This is a friendly reminder that you have an active subscription with us.</p>
                <p>Your subscription is set to expire on: <strong>{expiryDate:MMMM dd, yyyy}</strong></p>
                <p>If you wish to renew your subscription or have any questions, please don't hesitate to contact our support team.</p>
                <p>Thank you for your continued support!</p>";


            await SendEmailAsync(email, subject, body);
        }

        public async Task SendPurchaseConfirmationEmailAsync(string email, DateTime expiryDate)
        {
            var subject = "Subscription Purchase Confirmation";
            var body = $@"
                <h2>Subscription Purchase Confirmation</h2>
                <p>Dear valued customer,</p>
                <p>Thank you for purchasing a subscription with us!</p>
                <p>Your subscription is now active and will expire on: <strong>{expiryDate:MMMM dd, yyyy}</strong></p>
                <p>If you have any questions or need assistance, please don't hesitate to contact our support team.</p>
                <p>We hope you enjoy our services!</p>";

            await SendEmailAsync(email, subject, body);
        }
    }
}

