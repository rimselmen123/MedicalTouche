using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hlouwa.Models
{
    public class StockItem : AuditableEntity
    {
        [Required]
        public int ArticleId { get; set; }
        public Article Article { get; set; } = null!;

        public int? ArticleVariantId { get; set; }
        public ArticleVariant? ArticleVariant { get; set; }

        // quantité physiquement dispo
        [Column(TypeName = "decimal(18,3)")]
        public decimal OnHand { get; set; }

        // quantité bloquée par réservation
        [Column(TypeName = "decimal(18,3)")]
        public decimal Reserved { get; set; }
    }
}
