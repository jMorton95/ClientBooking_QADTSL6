using System.ComponentModel.DataAnnotations;

namespace ClientBooking.Data.Entities;

public class Client : Entity
{
    [Required, StringLength(255)]
    public string Name { get; set; }

    public string Description { get; set; }

    public TimeSpan ClientWorkingHoursStart { get; set; }
    
    public TimeSpan ClientWorkingHoursEnd { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}