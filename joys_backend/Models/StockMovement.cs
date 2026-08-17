using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hlouwa.Enums;

namespace Hlouwa.Models
{
    public class StockMovement : AuditableEntity
    {
        public int ArticleId { get; set; }
        public int? ArticleVariantId { get; set; }

        public StockMovementType Type { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [MaxLength(30)]
        public string? RefType { get; set; } // "Order", "Admin", "Import"

        [MaxLength(60)]
        public string? RefId { get; set; }   // orderNumber / userId / etc.

        [MaxLength(300)]
        public string? Note { get; set; }

        [MaxLength(450)]
        public string? ActorUserId { get; set; }
    }
}
