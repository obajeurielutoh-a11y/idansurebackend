using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SubscriptionSystem.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<PaymentRecord> AddPaymentRecordAsync(PaymentRecord paymentRecord)
        {
            _context.PaymentRecords.Add(paymentRecord);
            await _context.SaveChangesAsync();
            return paymentRecord;
        }

        public async Task<PaymentRecord> GetPaymentRecordByIdAsync(int id)
        {
            return await _context.PaymentRecords.FindAsync(id);
        }

        public async Task<IEnumerable<PaymentRecord>> GetPaymentRecordsByCustomerRefAsync(string customerRef)
        {
            return await _context.PaymentRecords
                .Where(p => p.CustomerRef == customerRef)
                .ToListAsync();
        }


        public async Task<PaymentRecord> UpdatePaymentRecordAsync(PaymentRecord paymentRecord)
        {
            _context.Entry(paymentRecord).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return paymentRecord;
        }

        public async Task<bool> DeletePaymentRecordAsync(int id)
        {
            var paymentRecord = await _context.PaymentRecords.FindAsync(id);
            if (paymentRecord == null)
                return false;

            _context.PaymentRecords.Remove(paymentRecord);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TransactionQueryRecord> GetTransactionQueryRecordByIdAsync(int id)
        {
            return await _context.TransactionQueryRecords.FindAsync(id);
        }

        public async Task<TransactionQueryRecord> GetTransactionQueryRecordByTraceIdAsync(string traceId)
        {
            return await _context.TransactionQueryRecords
                .FirstOrDefaultAsync(t => t.TraceId == traceId);
        }

        public async Task<IEnumerable<TransactionQueryRecord>> GetTransactionQueryRecordsByCustomerRefAsync(string customerRef)
        {
            return await _context.TransactionQueryRecords
                .Where(t => t.CustomerRef == customerRef)
                .ToListAsync();
        }

        public async Task<TransactionQueryRecord> AddTransactionQueryRecordAsync(TransactionQueryRecord transactionQueryRecord)
        {
            _context.TransactionQueryRecords.Add(transactionQueryRecord);
            await _context.SaveChangesAsync();
            return transactionQueryRecord;
        }

        public async Task<TransactionQueryRecord> UpdateTransactionQueryRecordAsync(TransactionQueryRecord transactionQueryRecord)
        {
            _context.Entry(transactionQueryRecord).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return transactionQueryRecord;
        }

        public async Task<bool> DeleteTransactionQueryRecordAsync(int id)
        {
            var transactionQueryRecord = await _context.TransactionQueryRecords.FindAsync(id);
            if (transactionQueryRecord == null)
                return false;

            _context.TransactionQueryRecords.Remove(transactionQueryRecord);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Payment> AddPaymentAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

       

        public async Task<bool> HasSuccessfulPaymentAsync(string email)
        {
            return await _context.Payments
                .AnyAsync(p => p.Email == email && p.Status == "Completed");
        }


        public async Task<Payment> UpdatePaymentAsync(Payment payment)
        {
            _context.Entry(payment).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<bool> DeletePaymentAsync(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return false;

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Payment> GetPaymentByIdAsync(int id)
        {
            return await _context.Payments.FindAsync(id);
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByUserIdAsync(string userId)
        {
            return await _context.Payments
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }
      

        public async Task<Payment> GetLatestPaymentAsync(string email)
        {
            return await _context.Payments
                .Where(p => p.Email == email)
                .OrderByDescending(p => p.PaymentDate)
                .FirstOrDefaultAsync();
        }



       
        public async Task<List<string>> GetUsersWithPaymentAmountAsync(decimal amount)
        {
            return await _context.Transactions
                .Where(p => p.Amount == amount)
                .Select(p => p.Email)
                .Distinct()
                .ToListAsync();
        }




    }
}

