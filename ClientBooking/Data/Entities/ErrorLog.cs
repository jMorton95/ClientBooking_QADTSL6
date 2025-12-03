using System.ComponentModel.DataAnnotations;

namespace ClientBooking.Data.Entities;

public class ErrorLog
{
    public int Id { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    public string LogLevel { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Exception { get; set; }

    [StringLength(255)]
    public string? Source { get; set; }
}
