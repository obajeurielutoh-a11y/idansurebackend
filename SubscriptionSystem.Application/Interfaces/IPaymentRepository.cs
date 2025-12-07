using SubscriptionSystem.Domain.Entities;
namespace SubscriptionSystem.Application.Interfaces
{
    public interface IPaymentRepository
    {
        //Task AddPaymentAsync(Payment payment);
        Task<Payment> GetLatestPaymentAsync(string email);
        Task<bool> HasSuccessfulPaymentAsync(string email);
        Task<List<string>> GetUsersWithPaymentAmountAsync(decimal amount);

        Task<Payment> AddPaymentAsync(Payment payment);
      
       
        Task<Payment> UpdatePaymentAsync(Payment payment);
        Task<bool> DeletePaymentAsync(int id);
        Task<Payment> GetPaymentByIdAsync(int id);
        Task<IEnumerable<Payment>> GetPaymentsByUserIdAsync(string userId);
        Task<PaymentRecord> AddPaymentRecordAsync(PaymentRecord paymentRecord);
        Task<PaymentRecord> GetPaymentRecordByIdAsync(int id);
        Task<IEnumerable<PaymentRecord>> GetPaymentRecordsByCustomerRefAsync(string customerRef);
        Task<PaymentRecord> UpdatePaymentRecordAsync(PaymentRecord paymentRecord);
        Task<bool> DeletePaymentRecordAsync(int id);

        Task<TransactionQueryRecord> GetTransactionQueryRecordByIdAsync(int id);
        Task<TransactionQueryRecord> GetTransactionQueryRecordByTraceIdAsync(string traceId);
        Task<IEnumerable<TransactionQueryRecord>> GetTransactionQueryRecordsByCustomerRefAsync(string customerRef);
        Task<TransactionQueryRecord> AddTransactionQueryRecordAsync(TransactionQueryRecord transactionQueryRecord);
        Task<TransactionQueryRecord> UpdateTransactionQueryRecordAsync(TransactionQueryRecord transactionQueryRecord);
        Task<bool> DeleteTransactionQueryRecordAsync(int id);

    }
}

