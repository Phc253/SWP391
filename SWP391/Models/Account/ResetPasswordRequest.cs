namespace SWP391.Models.Account;

public class ResetPasswordRequest
{
    public string Email { get; set; } = null!;
    public string Pin { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
