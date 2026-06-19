namespace SWP391.Models.Admin
{
    public class AdminStatsResponse
    {
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public List<RoleCountItem> UsersByRole { get; set; } = new();
        public int SyncJobsLast30Days { get; set; }
        public int SyncJobsCompleted { get; set; }
        public int SyncJobsCompletedWithWarnings { get; set; }
        public int SyncJobsFailed { get; set; }
        public List<RecentSyncJobItem> RecentSyncJobs { get; set; } = new();
        public int TotalNotifications { get; set; }
        public int ActivityLogsLast7Days { get; set; }
        public DateTime? LastSyncTime { get; set; }
    }

    public class RoleCountItem
    {
        public string RoleName { get; set; } = null!;
        public int Count { get; set; }
    }

    public class RecentSyncJobItem
    {
        public long SyncJobId { get; set; }
        public string SourceName { get; set; } = null!;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Status { get; set; }
        public int? RecordsFetched { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
