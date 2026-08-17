namespace Hlouwa.DTOs
{
    public class CreateReclamationDto
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Subject { get; set; }
        public string Message { get; set; } = null!;
    }
}
