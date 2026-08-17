using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hlouwa.Enums;

namespace Hlouwa.Models
{
    public class StockReservation : AuditableEntity
    {
        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public StockReservationStatus Status { get; set; } = StockReservationStatus.Active;

        public DateTime ExpiresAt { get; set; }

        [MaxLength(200)]
        public string? Reason { get; set; }

        public ICollection<StockReservationItem> Items { get; set; } = new List<StockReservationItem>();
    }

    public class StockReservationItem : AuditableEntity
    {
        public int StockReservationId { get; set; }
        public StockReservation StockReservation { get; set; } = null!;

        public int ArticleId { get; set; }
        public int? ArticleVariantId { get; set; }

        [MaxLength(140)]
        public string Name { get; set; } = null!;

        [MaxLength(60)]
        public string? VariantName { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }
    }
}
