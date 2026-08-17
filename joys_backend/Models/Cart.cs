using System.ComponentModel.DataAnnotations;

namespace Hlouwa.Models;

public class Cart : AuditableEntity
{
    [Required, MaxLength(450)]
    public string UserId { get; set; } = null!;
    public ApplicationUser? User { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "TND";

    // Totaux recalculés côté serveur
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
