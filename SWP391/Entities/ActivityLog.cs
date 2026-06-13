using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWP391.Entities;

public partial class ActivityLog
{
    [Key]
    public long ActivityLogId { get; set; }

    public int? UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = null!;

    [MaxLength(50)]
    public string? TargetType { get; set; }

    public long? TargetId { get; set; }

    public string? Details { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}
