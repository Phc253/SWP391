using System;

namespace SWP391.Models.Follow
{
    public class FollowItemResponse
    {
        public long FollowId { get; set; }
        public long TargetId { get; set; }
        public string TargetType { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        
        // Enrichment: Thông tin đi kèm
        public string? AuthorName { get; set; }
        public int? PaperCount { get; set; }
    }
}
