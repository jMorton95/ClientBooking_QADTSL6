using System.ComponentModel.DataAnnotations;

namespace ClientBooking.Data.Entities;

public enum AuditAction
{
    Create = 1,
    Update = 2,
}

public class AuditLog
{
    public int Id { get; set; }
    [StringLength(255)]
    public required string EntityName { get; set; } = string.Empty;
    [StringLength(255)]
    public required string EntityId { get; set; } = string.Empty;
    public int? UserId { get; set; }
    [StringLength(255)]
    public string? UserName { get; set; }
    public AuditAction Action { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public virtual User? User { get; set; }
}