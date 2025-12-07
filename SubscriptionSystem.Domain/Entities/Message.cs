using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubscriptionSystem.Domain.Entities
{
    public class Message
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
