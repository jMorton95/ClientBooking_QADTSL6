using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClientBooking.Data.JoiningTables;

namespace ClientBooking.Data.Entities;

public class User : Entity
{
    [Required, StringLength(50)]
    public string FirstName { get; set; }
    
    [Required, StringLength(50)]
    public string LastName { get; set; }
    
    [Required, StringLength(255)]
    public string Email { get; set; }
    
    [Required]
    public bool IsActive { get; set; }
    
    [Required]
    public string HashedPassword { get; set; }
    
    public bool IsLockedOut { get; set; } = false;
    
    public DateTime? LockoutEnd { get; set; }

    public int AccessFailedCount { get; set; } = 0;
    
    [NotMapped]
    public string FullName =>  $"{FirstName} {LastName}";
    
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserBooking> UserBookings { get; set; } = new List<UserBooking>();
    public ICollection<UserUnavailability> UnavailabilityPeriods { get; set; } = new List<UserUnavailability>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}