using System.ComponentModel.DataAnnotations;

namespace Hlouwa.Models
{
    public class Article : AuditableEntity
    {
        [Required, MaxLength(140)]
        public string Title { get; set; } = null!;

        [Required, MaxLength(180)]
        public string Slug { get; set; } = null!; // unique

        [MaxLength(300)]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        // Base price (can be overridden by variants)
        public decimal Price { get; set; }

        public decimal? OldPrice { get; set; }

        [MaxLength(40)]
        public string? Sku { get; set; }

        public bool IsActive { get; set; } = true;
            public bool IsFeatured { get; set; }

        // Optional availability logic (pâtisserie souvent "sur commande")
        public bool IsMadeToOrder { get; set; } = true;

        // If you manage stock:
        public int? StockQuantity { get; set; } // null = not tracked

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public ICollection<ArticleImage> Images { get; set; } = new List<ArticleImage>();
        public ICollection<ArticleVariant> Variants { get; set; } = new List<ArticleVariant>();
    }
}
