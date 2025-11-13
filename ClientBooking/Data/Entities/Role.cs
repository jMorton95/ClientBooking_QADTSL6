using System.ComponentModel.DataAnnotations;
using ClientBooking.Data.JoiningTables;

namespace ClientBooking.Data.Entities;

public class Role : Entity
{
    [Required, StringLength(100)]
    public string Name { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}