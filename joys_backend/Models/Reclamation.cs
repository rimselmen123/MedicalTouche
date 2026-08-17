using Hlouwa.Enums;
using System.ComponentModel.DataAnnotations;

namespace Hlouwa.Models
{

    public class Reclamation : AuditableEntity
    {
        // Optional: if connected
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        // Optional: link to an order
        public int? OrderId { get; set; }
        public Order? Order { get; set; }

        [Required, MaxLength(160)]
        public string FullName { get; set; } = null!;

        [Required, MaxLength(160)]
        public string Email { get; set; } = null!;

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(160)]
        public string? Subject { get; set; }

        [Required]
        public string Message { get; set; } = null!;

        public ReclamationStatus Status { get; set; } = ReclamationStatus.New;

        [MaxLength(600)]
        public string? AdminNote { get; set; }
    }
}
