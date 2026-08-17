using Hlouwa.Enums;
using System.ComponentModel.DataAnnotations;

namespace Hlouwa.Models
{
    // History for admin + troubleshooting
    public class OrderStatusHistory : AuditableEntity
    {
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public OrderStatus FromStatus { get; set; }
        public OrderStatus ToStatus { get; set; }

        public PaymentStatus? PaymentStatusSnapshot { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        // Admin / system actor (optional)
        [MaxLength(450)]
        public string? ChangedByUserId { get; set; }
    }

}
