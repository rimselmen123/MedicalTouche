using Hlouwa.Models;
using Hlouwa.Enums;
using System.ComponentModel.DataAnnotations;

public class Order : AuditableEntity
{
    [Required]
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    // Human-friendly unique number: HLW-2026-000123
    [Required, MaxLength(30)]
    public string OrderNumber { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    // If PaymentMethod == Online, choose provider
    public PaymentProvider PaymentProvider { get; set; } = PaymentProvider.None;

    // Totals
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "TND";

    // Delivery info
    public OrderAddressSnapshot ShippingAddress { get; set; } = new();

    public DateTime? RequestedDeliveryDate { get; set; }

    [MaxLength(40)]
    public string? DeliveryTimeSlot { get; set; } // "10:00-12:00"

    [MaxLength(500)]
    public string? CustomerNote { get; set; }

    [MaxLength(500)]
    public string? AdminNote { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<PaymentTransaction> Payments { get; set; } = new List<PaymentTransaction>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
}
