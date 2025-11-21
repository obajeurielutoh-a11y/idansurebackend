using System.Collections.Generic;

namespace SubscriptionSystem.Application.DTOs
{
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int Page { get; set; }

        public PaginatedResult()
        {
            Items = new List<T>();
        }
    }
}

