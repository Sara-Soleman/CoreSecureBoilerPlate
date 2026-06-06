using Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class RefreshToken : BaseEntity<Guid>
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string CreatedByIp { get; set; } = string.Empty;
        public DateTime? RevokedAtUtc { get; set; }
        public bool IsActive => !IsExpired && RevokedAtUtc == null;



        public string Country { get; set; } = "Unknown";
        public string City { get; set; } = "Unknown";
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }


       
        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
    }
}
