namespace SWP391.Models.Admin
{
    public class AdminUserResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
