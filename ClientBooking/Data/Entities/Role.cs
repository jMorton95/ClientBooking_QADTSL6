using System.ComponentModel.DataAnnotations;
using ClientBooking.Data.JoiningTables;

namespace ClientBooking.Data.Entities;

public enum RoleName
{
    User,
    Admin
}

public class Role : Entity
{
    [Required]
    public RoleName Name { get; set; } =  RoleName.User;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}