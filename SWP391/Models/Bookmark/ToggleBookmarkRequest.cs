namespace SWP391.Models.Bookmark
{
    public class ToggleBookmarkRequest
    {
        public long TargetId { get; set; }
        public string TargetType { get; set; } = null!; // Example: "Paper", "Keyword"
    }
}