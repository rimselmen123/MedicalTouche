using System.ComponentModel.DataAnnotations;

namespace Hlouwa.Models
{

    // ------------------------------
    // Addresses (User-managed) + Snapshot (Order-owned)
    // ------------------------------
    public class UserAddress : AuditableEntity
    {
        [Required]
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        [MaxLength(60)]
        public string? Label { get; set; } // "Maison", "Travail"

        [Required, MaxLength(160)]
        public string FullName { get; set; } = null!;

        [Required, MaxLength(30)]
        public string Phone { get; set; } = null!;

        [Required, MaxLength(180)]
        public string Line1 { get; set; } = null!;

        [MaxLength(180)]
        public string? Line2 { get; set; }

        [Required, MaxLength(80)]
        public string City { get; set; } = null!;

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(80)]
        public string? Governorate { get; set; } // Tunis, Ariana, Sousse...

        [MaxLength(2)]
        public string CountryCode { get; set; } = "TN";

        public bool IsDefault { get; set; }
    }

}
