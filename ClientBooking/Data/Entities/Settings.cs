using System.ComponentModel.DataAnnotations;
using ClientBooking.Shared.Enums;

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
    public required RoleName DefaultUserRole { get; set; }

    [Required]
    public int Version { get; set; }
}