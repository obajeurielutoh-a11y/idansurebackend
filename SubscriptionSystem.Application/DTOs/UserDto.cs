using System;
using System.Collections.Generic;

namespace SubscriptionSystem.Application.DTOs
{
    public class UserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SubscriptionDto> Subscriptions { get; set; }
    }
}

