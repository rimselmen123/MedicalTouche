using System.ComponentModel.DataAnnotations;
using Hlouwa.Enums;

namespace Hlouwa.DTOs;

// =====================================================
// CREATE ORDER (Client)
// =====================================================
public class CreateOrderDto : IValidatableObject
{
    [Required]
    public PaymentMethod PaymentMethod { get; init; } = PaymentMethod.CashOnDelivery;

    // ✅ rename property to avoid enum/type name collision
    public PaymentProvider? Provider { get; init; }

    [MaxLength(500)]
    public string? CustomerNote { get; init; }

    [Required]
    public ShippingAddressDto ShippingAddress { get; init; } = null!;

    public DateTime? RequestedDeliveryDate { get; init; }

    [MaxLength(40)]
    public string? DeliveryTimeSlot { get; init; }

    [Required, MinLength(1)]
    public List<CreateOrderItemDto> Items { get; init; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PaymentMethod == PaymentMethod.Online &&
            (!Provider.HasValue || Provider.Value == PaymentProvider.None))
        {
            yield return new ValidationResult(
                "PaymentProvider requis pour paiement en ligne.",
                new[] { nameof(Provider) }
            );
        }
    }
}

public class ShippingAddressDto
{
    [Required, MaxLength(160)]
    public string FullName { get; init; } = null!;

    [Required, MaxLength(30)]
    public string Phone { get; init; } = null!;

    [Required, MaxLength(180)]
    public string Line1 { get; init; } = null!;

    [MaxLength(180)]
    public string? Line2 { get; init; }

    [Required, MaxLength(80)]
    public string City { get; init; } = null!;

    [MaxLength(20)]
    public string? PostalCode { get; init; }

    [MaxLength(80)]
    public string? Governorate { get; init; }
}

public class CreateOrderItemDto
{
    [Required]
    public int ArticleId { get; init; }

    public int? VariantId { get; init; }

    [Range(1, 999)]
    public int Quantity { get; init; } = 1;
}

// =====================================================
// CLIENT DTOs
// =====================================================
public record OrderSummaryDto(
    int Id,
    string OrderNumber,
    OrderStatus Status,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    decimal Total,
    string Currency,
    DateTime CreatedAt
);

public record OrderItemDto(
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

public record OrderShippingAddressDto(
    string? FullName,
    string? Phone,
    string? Line1,
    string? Line2,
    string? City,
    string? PostalCode,
    string? Governorate,
    string? CountryCode
);

public record OrderStatusHistoryDto(
    int Id,
    OrderStatus FromStatus,
    OrderStatus ToStatus,
    PaymentStatus? PaymentStatusSnapshot,
    string? Note,
    DateTime CreatedAt
);

public record OrderPaymentDto(
    int Id,
    PaymentProvider Provider,
    PaymentTxnStatus Status,
    decimal Amount,
    string Currency,
    DateTime? PaidAt,
    string? FailureReason,
    DateTime CreatedAt
);

public record OrderDetailsDto(
    int Id,
    string OrderNumber,
    OrderStatus Status,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    PaymentProvider Provider,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal DiscountTotal,
    decimal Total,
    string Currency,
    OrderShippingAddressDto ShippingAddress,
    string? CustomerNote,
    string? AdminNote,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<OrderItemDto> Items,
    List<OrderStatusHistoryDto> StatusHistory,
    List<OrderPaymentDto> Payments
);

// =====================================================
// ADMIN DTOs
// =====================================================
public class AdminUpdateOrderStatusDto
{
    [Required]
    public OrderStatus Status { get; init; }

    [MaxLength(600)]
    public string? Note { get; init; }

    [MaxLength(1000)]
    public string? AdminNote { get; init; }
}

public class AdminUpdateFeesDto
{
    [Range(0, 999999)]
    public decimal DeliveryFee { get; init; }

    [Range(0, 999999)]
    public decimal DiscountTotal { get; init; }
}

public class OrderAdminQuery
{
    public OrderStatus? Status { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }

    public string? Search { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public record OrderAdminListDto(
    int Id,
    string OrderNumber,
    OrderStatus Status,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    decimal Total,
    string Currency,
    DateTime CreatedAt,
    int ItemsCount,
    string? CustomerEmail,
    string? CustomerFullName,
    string? CustomerPhone
);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public class AdminUpdateNoteDto
{
    [MaxLength(1000)]
    public string? Note { get; init; }
}
