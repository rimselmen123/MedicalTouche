using System.ComponentModel.DataAnnotations;

namespace Hlouwa.Models;

public class CartItem : AuditableEntity
{
    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public int? ArticleVariantId { get; set; }
    public ArticleVariant? ArticleVariant { get; set; }

    // Affichage + sécurité (snap “light”, mais prix recalculé)
    [Required, MaxLength(140)]
    public string Name { get; set; } = null!;

    [MaxLength(60)]
    public string? VariantName { get; set; }

    [MaxLength(40)]
    public string? Sku { get; set; }

    [MaxLength(350)]
    public string? ImageUrl { get; set; }

    public decimal UnitPrice { get; set; }

    [Range(1, 999)]
    public int Quantity { get; set; } = 1;
}
