using System.ComponentModel.DataAnnotations;

namespace Hlouwa.Models
{
    public abstract class AuditableEntity
    {
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Optimistic concurrency
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

}
