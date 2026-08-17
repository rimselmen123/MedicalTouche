using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hlouwa.Models
{

    public class OrderItem : AuditableEntity
    {
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int ArticleId { get; set; }
        public Article Article { get; set; } = null!;

        public int? ArticleVariantId { get; set; }
        public ArticleVariant? ArticleVariant { get; set; }

        // Snapshot fields (important!)
        [Required, MaxLength(140)]
        public string Name { get; set; } = null!;

        [MaxLength(60)]
        public string? VariantName { get; set; } // "500g"

        [MaxLength(40)]
        public string? Sku { get; set; }

        [MaxLength(350)]
        public string? ImageUrl { get; set; }

        public decimal UnitPrice { get; set; }

        // In pâtisserie: quantity is usually unit count.
        public int Quantity { get; set; }

        [NotMapped]
        public decimal LineTotal => UnitPrice * Quantity;
    }

}
