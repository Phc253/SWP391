namespace SWP391.Models.Follow
{
    public class ToggleFollowRequest
    {
        public long TargetId { get; set; }       // ID của Author, Topic, Journal...
        public string TargetType { get; set; } = null!;   // "Author", "Topic", "Journal"
    }
}
