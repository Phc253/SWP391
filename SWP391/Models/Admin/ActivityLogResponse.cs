namespace SWP391.Models.Admin
{
    public class PagedActivityLogResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<ActivityLogItem> Items { get; set; } = new();
    }

    public class ActivityLogItem
    {
        public long ActivityLogId { get; set; }
        public int? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string Action { get; set; } = null!;
        public string? TargetType { get; set; }
        public long? TargetId { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
