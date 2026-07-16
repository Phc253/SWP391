using System;
using System.Collections.Generic;

namespace SWP391.Models.Account
{
    public class UserProfileResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public string ActorType { get; set; } = string.Empty;
        public int? RemainingCredits { get; set; }
        public DateTime? LastCreditResetTime { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
