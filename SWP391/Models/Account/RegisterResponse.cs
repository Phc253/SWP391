namespace SWP391.Models.Account;

public class RegisterResponse
{
    public int UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? PhoneNumber { get; set; }

    public string ActorType { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public string Message { get; set; } = string.Empty;
}
