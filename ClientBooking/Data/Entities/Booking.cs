using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClientBooking.Data.JoiningTables;

namespace ClientBooking.Data.Entities;

public class Booking : Entity
{
    [Required]
    public int ClientId { get; set; }

    [ForeignKey(nameof(ClientId))]
    public Client Client { get; set; }

    public string Notes { get; set; }

    [Required]
    public DateTime StartDateTime { get; set; }

    [Required]
    public DateTime EndDateTime { get; set; }

    [StringLength(50)]
    public string Status { get; set; }

    public ICollection<UserBooking> UserBookings { get; set; } = new List<UserBooking>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}