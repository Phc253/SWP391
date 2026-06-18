namespace SWP391.Entities;

public partial class RevokedToken
{
    public long RevokedTokenId { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime RevokedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
