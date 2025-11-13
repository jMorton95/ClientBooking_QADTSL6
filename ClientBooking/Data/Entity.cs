using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClientBooking.Data.Entities;

namespace ClientBooking.Data;

public abstract class Entity
{
    [Required, Key]
    public int Id { get; set; }
    
    [Required, Timestamp]
    public int RowVersion { get; set; }
    
    [Required]
    public DateTime SavedAt { get; set; }
    
    [Required]
    public int SavedById { get; set; } 
    
    [ForeignKey(nameof(SavedById))]
    public User SavedBy { get; set; }
}