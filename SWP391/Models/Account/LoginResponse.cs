namespace SWP391.Models.Account
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string ActorType { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}
