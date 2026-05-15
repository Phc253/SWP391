namespace SWP391.Models.Account;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public int? RoleId { get; set; } // Role ???c ch?n t? combo box
}
