using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClientBooking.Data.JoiningTables;
using ClientBooking.Shared.Enums;

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

    [NotMapped]
    public TimeSpan Duration => EndDateTime - StartDateTime;

    public BookingStatus Status { get; set; } = BookingStatus.Scheduled;
    
    public bool IsRecurring { get; set; }
    
    public int NumberOfRecurrences { get; set; }

    public BookingRecurrencePattern RecurrencePattern { get; set; } = BookingRecurrencePattern.None;

    public ICollection<UserBooking> UserBookings { get; set; } = new List<UserBooking>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}