using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClientBooking.Data.Entities;

public enum NotificationType
{
    Booking,
    Reminder,
    Alert,
    Info
}

public class Notification : Entity
{
    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; }

    public int? BookingId { get; set; }

    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; }

    [Required]
    public string Message { get; set; }
    
    public NotificationType NotificationType { get; set; } =  NotificationType.Info;

    public DateTime? SentDate { get; set; }

    [Required]
    public bool IsRead { get; set; } = false;
}