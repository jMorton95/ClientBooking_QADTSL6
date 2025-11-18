using System.ComponentModel.DataAnnotations;

namespace ClientBooking.Data.Entities;

public class Settings : Entity
{
    [Required]
    public TimeSpan DefaultWorkingHoursStart { get; set; }

    [Required]
    public TimeSpan DefaultWorkingHoursEnd { get; set; }
    
    [Required]
    public TimeSpan DefaultBreakTimeStart { get; set; }
    
    [Required]
    public TimeSpan DefaultBreakTimeEnd { get; set; }

    [Required]
    public int DefaultBookingDuration { get; set; }

    [Required, StringLength(100)]
    public required string DefaultUserRole { get; set; }

    [Required]
    public int Version { get; set; }
}