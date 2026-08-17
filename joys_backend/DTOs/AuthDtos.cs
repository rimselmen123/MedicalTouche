namespace Hlouwa.DTOs
{

    public record RegisterDto(string Email, string Password, string? FullName, string? Phone);
    public record LoginDto(string Email, string Password);
    public record AuthResponseDto(string Token, DateTime ExpiresAt, string UserId, string Email, string? FullName, string[] Roles);
}
