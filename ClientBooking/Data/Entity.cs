using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClientBooking.Data.Entities;

namespace ClientBooking.Data;


//Abstract base class for all entities to enforce generic auditable properties
public abstract class Entity
{
    [Required, Key]
    public int Id { get; set; }

    [Required]
    public int RowVersion { get; set; }
    
    [Required]
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    
    public int? SavedById { get; set; } 
    
    [ForeignKey(nameof(SavedById))]
    public User SavedBy { get; set; }
}