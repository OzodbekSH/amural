using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gym
{
    public class UserAccount
    {
        [Key]
        public int    UserId      { get; set; }
        public string Login       { get; set; }

        [Column("UserPassword")]
        public string Password    { get; set; }

        // EF-mapped string column; Role is the computed enum property for app code
        [Column("UserRole")]
        public string RoleString  { get; set; }

        [NotMapped]
        public UserRole Role
        {
            get => Enum.TryParse(RoleString, out UserRole r) ? r : UserRole.Client;
            set => RoleString = value.ToString();
        }

        public string DisplayName { get; set; }
        public int?   MemberId    { get; set; }
    }
}
