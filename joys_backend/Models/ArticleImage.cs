using System.ComponentModel.DataAnnotations;

namespace Hlouwa.Models
{
    public class ArticleImage : AuditableEntity
    {
        public int ArticleId { get; set; }
        public Article Article { get; set; } = null!;

        [Required, MaxLength(350)]
        public string Url { get; set; } = null!; // /uploads/articles/xxx.jpg

        public bool IsCover { get; set; }
        public int SortOrder { get; set; } = 0;

        [MaxLength(140)]
        public string? Alt { get; set; }
    }
}
