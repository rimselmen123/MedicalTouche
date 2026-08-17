using System.ComponentModel.DataAnnotations;
using Hlouwa.Enums;

namespace Hlouwa.DTOs;

public record CartItemDto(
    int Id,
    int ArticleId,
    int? VariantId,
    string Name,
    string? VariantName,
    string? Sku,
    string? ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal
);

public record CartDto(
    int Id,
    string Currency,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal DeliveryFee,
    decimal Total,
    List<CartItemDto> Items,
    DateTime UpdatedAt
);

public class AddCartItemDto
{
    [Required]
    public int ArticleId { get; init; }

    public int? VariantId { get; init; }

    [Range(1, 999)]
    public int Quantity { get; init; } = 1;
}

public class UpdateCartItemDto
{
    [Range(1, 999)]
    public int Quantity { get; init; } = 1;
}
