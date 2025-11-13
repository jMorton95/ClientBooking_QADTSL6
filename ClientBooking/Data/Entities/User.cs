using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClientBooking.Data.Entities;

public class User : Entity
{
    [Required, StringLength(50)]
    public string FirstName { get; set; }
    
    [Required, StringLength(50)]
    public string LastName { get; set; }
    
    [Required, StringLength(255)]
    public string Email { get; set; }
    
    public bool IsActive { get; set; }
    
    [NotMapped]
    public string FullName =>  $"{FirstName} {LastName}";
}