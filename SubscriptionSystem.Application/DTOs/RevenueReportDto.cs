// File: SubscriptionSystem.Application/DTOs/RevenueReportDto.cs
using System;
using System.Collections.Generic;

namespace SubscriptionSystem.Application.DTOs
{
    public class RevenueReportDto
    {
        public List<RevenueDataPointDto> Data { get; set; }
        public decimal TotalRevenue { get; set; }
        public string GroupBy { get; set; } // "day", "week", "month"
    }

    public class RevenueDataPointDto
    {
        public DateTime Date { get; set; }

        public decimal Credo { get; set; }
        public decimal AlatPay { get; set; }
        public decimal CoralPay { get; set; }
        public decimal Total { get; set; }
    }

    // Aliases for specific groupings
    public class DailyRevenueDto : RevenueDataPointDto { }
    public class WeeklyRevenueDto : RevenueDataPointDto { }
    public class MonthlyRevenueDto : RevenueDataPointDto { }
}