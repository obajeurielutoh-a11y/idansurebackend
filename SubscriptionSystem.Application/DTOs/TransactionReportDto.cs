// File: SubscriptionSystem.Application/DTOs/TransactionReportDto.cs
using System;
using System.Collections.Generic;

namespace SubscriptionSystem.Application.DTOs
{
    public class TransactionReportDto
    {
        public List<TransactionDto> Transactions { get; set; }
        public TransactionSummaryDto Summary { get; set; }
        public PaginationDto Pagination { get; set; }
    }

    public class TransactionDto
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string PaymentGateway { get; set; }
        public string ExternalTransactionId { get; set; }
        public decimal Amount { get; set; }
        public string PlanType { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class TransactionSummaryDto
    {
        public int TotalTransactions { get; set; }
        public decimal TotalAmount { get; set; }
        public List<GatewayStatDto> ByGateway { get; set; }
        public List<PlanTypeStatDto> ByPlanType { get; set; }
    }

    public class GatewayStatDto
    {
        public string Gateway { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class PlanTypeStatDto
    {
        public string PlanType { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class PaginationDto
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }
}