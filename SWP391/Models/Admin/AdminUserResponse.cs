namespace SWP391.Models.Admin
{
    public class AdminUserResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public string ActorType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
