namespace SWP391.Models.Account;

public class RegisterResponse
{
    public int UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool IsActive { get; set; }
}
