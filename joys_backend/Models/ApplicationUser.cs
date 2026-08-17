using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Hlouwa.Models
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(160)]
        public string? FullName { get; set; }

        [MaxLength(30)]
        public string? DefaultPhone { get; set; }

        // Navigation
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Reclamation> Reclamations { get; set; } = new List<Reclamation>();
        public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    }
}

