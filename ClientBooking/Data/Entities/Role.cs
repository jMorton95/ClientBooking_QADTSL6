using System.ComponentModel.DataAnnotations;
using ClientBooking.Data.JoiningTables;
using ClientBooking.Shared.Enums;

namespace ClientBooking.Data.Entities;

public class Role
{
    [Required, Key]
    public int Id { get; set; }
    [Required]
    public RoleName Name { get; set; } =  RoleName.User;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}