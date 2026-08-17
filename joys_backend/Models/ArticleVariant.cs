using System.ComponentModel.DataAnnotations;

namespace Hlouwa.Models
{
    public class ArticleVariant : AuditableEntity
    {
        public int ArticleId { get; set; }
        public Article Article { get; set; } = null!;

        // Example: "250g", "500g", "6 pièces", etc.
        [Required, MaxLength(60)]
        public string Name { get; set; } = null!;

        public decimal Price { get; set; }

        public bool IsDefault { get; set; }

        // Optional stock per variant
        public int? StockQuantity { get; set; } // null = not tracked

        [MaxLength(40)]
        public string? Sku { get; set; }
    }
}
