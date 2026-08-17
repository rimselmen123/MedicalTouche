using System.ComponentModel.DataAnnotations;

namespace Hlouwa.Models
{

    public class Category : AuditableEntity
    {
        [Required, MaxLength(80)]
        public string Name { get; set; } = null!;

        [Required, MaxLength(120)]
        public string Slug { get; set; } = null!; // unique (ex: "gateaux")

        [MaxLength(300)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        public ICollection<Article> Articles { get; set; } = new List<Article>();
    }
}
