using System.ComponentModel.DataAnnotations;

namespace ClientBooking.Data.Entities;

public class Client : Entity
{
    [Required, StringLength(255)]
    public string Name { get; set; }

    public string Description { get; set; }
    
    [Required, StringLength(255)]
    public string Email { get; set; }
    
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}