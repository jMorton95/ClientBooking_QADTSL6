using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClientBooking.Data.Entities;

namespace ClientBooking.Data.JoiningTables;

[Table("UserBookings")]
public class UserBooking
{
    [Key, Column(Order = 0)]
    public int BookingId { get; set; }

    [Key, Column(Order = 1)]
    public int UserId { get; set; }

    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
}