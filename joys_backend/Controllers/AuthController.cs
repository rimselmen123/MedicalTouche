using Hlouwa.Models;
using Hlouwa.DTOs;
using Hlouwa.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Hlouwa.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userMgr;
    private readonly SignInManager<ApplicationUser> _signInMgr;
    private readonly RoleManager<IdentityRole> _roleMgr;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userMgr,
        SignInManager<ApplicationUser> signInMgr,
        RoleManager<IdentityRole> roleMgr,
        IConfiguration config,
        IWebHostEnvironment env,
        ILogger<AuthController> logger)
    {
        _userMgr = userMgr;
        _signInMgr = signInMgr;
        _roleMgr = roleMgr;
        _config = config;
        _env = env;
        _logger = logger;
    }

    private const string RoleClient = "Client";
    private const string RoleAdmin = "Admin";

    // -----------------------------
    // Response helpers (controller-only, coherent)
    // -----------------------------
    private ActionResult<T> ProblemT<T>(int status, string title, string detail, object? errors = null)
    {
        var pd = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = HttpContext.Request.Path
        };
        pd.Extensions["traceId"] = HttpContext.TraceIdentifier;
        if (errors != null) pd.Extensions["errors"] = errors;

        return StatusCode(status, pd);
    }

    private IActionResult ProblemX(int status, string title, string detail, object? errors = null)
    {
        var pd = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = HttpContext.Request.Path
        };
        pd.Extensions["traceId"] = HttpContext.TraceIdentifier;
        if (errors != null) pd.Extensions["errors"] = errors;

        return StatusCode(status, pd);
    }

    private ActionResult<T> ExceptionToProblemT<T>(string scope, Exception ex)
    {
        _logger.LogError(ex, "{Scope} failed.", scope);
        var detail = _env.IsDevelopment() ? ex.ToString() : "Une erreur interne est survenue.";
        return ProblemT<T>(500, $"Erreur serveur ({scope})", detail);
    }

    private IActionResult ExceptionToProblemX(string scope, Exception ex)
    {
        _logger.LogError(ex, "{Scope} failed.", scope);
        var detail = _env.IsDevelopment() ? ex.ToString() : "Une erreur interne est survenue.";
        return ProblemX(500, $"Erreur serveur ({scope})", detail);
    }

    // -----------------------------
    // DTO for /me (stable)
    // -----------------------------
    public record MeDto(string Id, string Email, string? FullName, string? Phone, IReadOnlyList<string> Roles);

    // -----------------------------
    // REGISTER
    // -----------------------------
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        try
        {
            if (dto == null)
                return ProblemT<AuthResponseDto>(400, "Requête invalide", "Payload invalide.");

            var email = (dto.Email ?? "").Trim().ToLowerInvariant();
            var fullName = dto.FullName?.Trim();
            var phone = dto.Phone?.Trim();

            if (string.IsNullOrWhiteSpace(email))
                return ProblemT<AuthResponseDto>(400, "Requête invalide", "Email requis.");

            if (string.IsNullOrWhiteSpace(dto.Password))
                return ProblemT<AuthResponseDto>(400, "Requête invalide", "Mot de passe requis.");

            var existing = await _userMgr.FindByEmailAsync(email);
            if (existing != null)
                return ProblemT<AuthResponseDto>(409, "Conflit", "Un compte existe déjà avec cet email.");

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                PhoneNumber = phone
            };

            var res = await _userMgr.CreateAsync(user, dto.Password);
            if (!res.Succeeded)
            {
                var errs = res.Errors.Select(e => e.Description).ToArray();
                return ProblemT<AuthResponseDto>(400, "Création impossible", "Validation échouée.", new { messages = errs });
            }

            await EnsureRoleExists(RoleClient);

            if (!await _userMgr.IsInRoleAsync(user, RoleClient))
            {
                var addRole = await _userMgr.AddToRoleAsync(user, RoleClient);
                if (!addRole.Succeeded)
                {
                    var errs = addRole.Errors.Select(e => e.Description).ToArray();
                    return ProblemT<AuthResponseDto>(400, "Rôle impossible", "Impossible d'ajouter le rôle client.", new { messages = errs });
                }
            }

            var token = await BuildJwt(user);

            // 201 Created -> points to /api/auth/me (route known)
            return CreatedAtAction(nameof(Me), null, token);
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<AuthResponseDto>("Register", ex);
        }
    }

    // -----------------------------
    // LOGIN
    // -----------------------------
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        try
        {
            if (dto == null)
                return ProblemT<AuthResponseDto>(400, "Requête invalide", "Payload invalide.");

            var email = (dto.Email ?? "").Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(dto.Password))
                return ProblemT<AuthResponseDto>(400, "Requête invalide", "Email et mot de passe requis.");

            var user = await _userMgr.FindByEmailAsync(email);
            if (user == null)
                return ProblemT<AuthResponseDto>(401, "Non autorisé", "Identifiants invalides.");

            var res = await _signInMgr.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

            if (res.IsLockedOut)
                return ProblemT<AuthResponseDto>(401, "Non autorisé", "Compte temporairement verrouillé. Réessayez plus tard.");

            if (!res.Succeeded)
                return ProblemT<AuthResponseDto>(401, "Non autorisé", "Identifiants invalides.");

            var token = await BuildJwt(user);
            return Ok(token);
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<AuthResponseDto>("Login", ex);
        }
    }

    // -----------------------------
    // ME (profile)
    // -----------------------------
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MeDto>> Me()
    {
        try
        {
            var userId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return ProblemT<MeDto>(401, "Non autorisé", "Token invalide.");

            var user = await _userMgr.FindByIdAsync(userId);
            if (user == null)
                return ProblemT<MeDto>(401, "Non autorisé", "Utilisateur introuvable.");

            var roles = await _userMgr.GetRolesAsync(user);

            var dto = new MeDto(
                user.Id,
                user.Email ?? "",
                user.FullName,
                user.PhoneNumber,
                roles.ToArray()
            );

            return Ok(dto);
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<MeDto>("Me", ex);
        }
    }

    // -----------------------------
    // BUILD JWT
    // -----------------------------
    private async Task<AuthResponseDto> BuildJwt(ApplicationUser user)
    {
        var jwt = _config.GetSection("Jwt");

        var key = jwt["Key"];
        var issuer = jwt["Issuer"];
        var audience = jwt["Audience"];

        if (string.IsNullOrWhiteSpace(key) ||
            string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience))
        {
            // Will be caught and returned as ProblemDetails 500 by callers
            throw new InvalidOperationException("Configuration Jwt invalide (Key/Issuer/Audience).");
        }

        var expiresMinutes = int.TryParse(jwt["ExpiresMinutes"], out var mins) ? mins : 120;

        var roles = (await _userMgr.GetRolesAsync(user)).ToArray();

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(expiresMinutes);

        var claims = new List<Claim>
        {
            // Standard claims
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, ToUnixTimeSeconds(now).ToString(), ClaimValueTypes.Integer64),

            // Custom claims used by frontend
            new("uid", user.Id),
            new("name", user.FullName ?? ""),
            new("phone", user.PhoneNumber ?? "")
        };

        // Roles
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: creds
        );

        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponseDto(tokenStr, expiresAt, user.Id, user.Email ?? "", user.FullName, roles);
    }

    private static long ToUnixTimeSeconds(DateTime dt)
        => new DateTimeOffset(dt).ToUnixTimeSeconds();

    private async Task EnsureRoleExists(string roleName)
    {
        if (!await _roleMgr.RoleExistsAsync(roleName))
        {
            var res = await _roleMgr.CreateAsync(new IdentityRole(roleName));
            if (!res.Succeeded)
            {
                var msg = string.Join(", ", res.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Impossible de créer le rôle {roleName}: {msg}");
            }
        }
    }
}
