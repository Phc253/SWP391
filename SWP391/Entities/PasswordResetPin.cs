using System;

namespace SWP391.Entities;

public partial class PasswordResetPin
{
    public long PasswordResetPinId { get; set; }
    public int UserId { get; set; }
    public string PinHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public virtual User User { get; set; } = null!;
}
