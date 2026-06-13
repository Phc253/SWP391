namespace SWP391.Models.Notification
{
    public class NotificationResponse
    {
        public long NotificationId { get; set; }
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? RelatedId { get; set; }
        public string? RelatedType { get; set; }
    }
}
