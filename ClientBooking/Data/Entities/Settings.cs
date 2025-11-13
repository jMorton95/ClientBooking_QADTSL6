using System.ComponentModel.DataAnnotations;

namespace ClientBooking.Data.Entities;

public class Settings : Entity
{
    [Required]
    public TimeSpan CompanyWorkingHoursStart { get; set; }

    [Required]
    public TimeSpan CompanyWorkingHoursEnd { get; set; }

    [Required]
    public int DefaultBookingDuration { get; set; }

    [Required, StringLength(100)]
    public string DefaultUserRole { get; set; }

    [Required]
    public int MaxDailyUserBookings { get; set; }

    [Required]
    public bool AllowWeekendBookings { get; set; }

    [Required]
    public int Version { get; set; }
}