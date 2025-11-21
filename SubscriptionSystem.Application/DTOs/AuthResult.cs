namespace SubscriptionSystem.Application.DTOs
{
    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }  // Ensure this exists
        public string ErrorMessage { get; set; }  // Add this line if missing
    }

}
