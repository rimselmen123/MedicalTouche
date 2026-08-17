using Hlouwa.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Hlouwa.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hlouwa.Controllers;

[ApiController]
[Route("api/addresses")]
[Authorize]
public class AddressesController : ControllerBase
{
    private readonly AppDbContext _db;
    public AddressesController(AppDbContext db) => _db = db;

    // -----------------------------
    // DTOs
    // -----------------------------
    public record AddressDto(
        int Id,
        string? Label,
        string FullName,
        string Phone,
        string Line1,
        string? Line2,
        string City,
        string? PostalCode,
        string? Governorate,
        string CountryCode,
        bool IsDefault,
        DateTime CreatedAt
    );

    public class UpsertAddressDto
    {
        [MaxLength(60)]
        public string? Label { get; init; }

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

        [MaxLength(2)]
        public string CountryCode { get; init; } = "TN";

        public bool IsDefault { get; init; }
    }

    // -----------------------------
    // Helpers (controller-only)
    // -----------------------------
    private string GetUserId()
        => User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    private static AddressDto ToDto(UserAddress a)
        => new(
            a.Id, a.Label, a.FullName, a.Phone, a.Line1, a.Line2,
            a.City, a.PostalCode, a.Governorate, a.CountryCode, a.IsDefault, a.CreatedAt
        );

    private static string NormalizeCountryCode(string? code)
        => string.IsNullOrWhiteSpace(code) ? "TN" : code.Trim().ToUpperInvariant();

    // ✅ For ActionResult<T> methods
    private ActionResult<T> ProblemT<T>(int status, string title, string detail)
    {
        var pd = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = HttpContext.Request.Path
        };
        pd.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return StatusCode(status, pd);
    }

    // ✅ For IActionResult methods
    private IActionResult ProblemX(int status, string title, string detail)
        => Problem(title: title, detail: detail, statusCode: status, instance: HttpContext.Request.Path);

    // -----------------------------
    // GET /api/addresses
    // -----------------------------
    [HttpGet]
    public async Task<ActionResult<List<AddressDto>>> GetAll()
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return ProblemT<List<AddressDto>>(401, "Non authentifié", "Token requis ou identifiant utilisateur introuvable.");

        var list = await _db.UserAddresses
            .AsNoTracking()
            .Where(a => a.UserId == userId && !a.IsDeleted)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AddressDto(
                a.Id, a.Label, a.FullName, a.Phone, a.Line1, a.Line2,
                a.City, a.PostalCode, a.Governorate, a.CountryCode, a.IsDefault, a.CreatedAt
            ))
            .ToListAsync();

        return Ok(list);
    }

    // -----------------------------
    // GET /api/addresses/{id}
    // -----------------------------
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AddressDto>> GetById(int id)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return ProblemT<AddressDto>(401, "Non authentifié", "Token requis ou identifiant utilisateur introuvable.");

        var a = await _db.UserAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted);

        if (a == null)
            return ProblemT<AddressDto>(404, "Introuvable", "Adresse introuvable.");

        return Ok(ToDto(a));
    }

    // -----------------------------
    // POST /api/addresses
    // -----------------------------
    [HttpPost]
    public async Task<ActionResult<AddressDto>> Create([FromBody] UpsertAddressDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return ProblemT<AddressDto>(401, "Non authentifié", "Token requis ou identifiant utilisateur introuvable.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var hasAny = await _db.UserAddresses.AnyAsync(a => a.UserId == userId && !a.IsDeleted);
        var makeDefault = dto.IsDefault || !hasAny;

        if (makeDefault)
        {
            var prev = await _db.UserAddresses
                .Where(a => a.UserId == userId && a.IsDefault && !a.IsDeleted)
                .ToListAsync();

            foreach (var p in prev) p.IsDefault = false;
        }

        var a = new UserAddress
        {
            UserId = userId,
            Label = dto.Label?.Trim(),
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone.Trim(),
            Line1 = dto.Line1.Trim(),
            Line2 = dto.Line2?.Trim(),
            City = dto.City.Trim(),
            PostalCode = dto.PostalCode?.Trim(),
            Governorate = dto.Governorate?.Trim(),
            CountryCode = NormalizeCountryCode(dto.CountryCode),
            IsDefault = makeDefault
        };

        _db.UserAddresses.Add(a);
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        return CreatedAtAction(nameof(GetById), new { id = a.Id }, ToDto(a));
    }

    // -----------------------------
    // PUT /api/addresses/{id}
    // -----------------------------
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertAddressDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return ProblemX(401, "Non authentifié", "Token requis ou identifiant utilisateur introuvable.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var a = await _db.UserAddresses
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted);

        if (a == null)
            return ProblemX(404, "Introuvable", "Adresse introuvable.");

        a.Label = dto.Label?.Trim();
        a.FullName = dto.FullName.Trim();
        a.Phone = dto.Phone.Trim();
        a.Line1 = dto.Line1.Trim();
        a.Line2 = dto.Line2?.Trim();
        a.City = dto.City.Trim();
        a.PostalCode = dto.PostalCode?.Trim();
        a.Governorate = dto.Governorate?.Trim();
        a.CountryCode = NormalizeCountryCode(dto.CountryCode);

        if (dto.IsDefault && !a.IsDefault)
        {
            var prev = await _db.UserAddresses
                .Where(x => x.UserId == userId && x.IsDefault && !x.IsDeleted)
                .ToListAsync();

            foreach (var p in prev) p.IsDefault = false;
            a.IsDefault = true;
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return NoContent();
    }

    // -----------------------------
    // PATCH /api/addresses/{id}/default
    // -----------------------------
    [HttpPatch("{id:int}/default")]
    public async Task<IActionResult> SetDefault(int id)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return ProblemX(401, "Non authentifié", "Token requis ou identifiant utilisateur introuvable.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var a = await _db.UserAddresses
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted);

        if (a == null)
            return ProblemX(404, "Introuvable", "Adresse introuvable.");

        var prev = await _db.UserAddresses
            .Where(x => x.UserId == userId && x.IsDefault && !x.IsDeleted)
            .ToListAsync();

        foreach (var p in prev) p.IsDefault = false;

        a.IsDefault = true;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return NoContent();
    }

    // -----------------------------
    // DELETE /api/addresses/{id}  (soft delete)
    // -----------------------------
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return ProblemX(401, "Non authentifié", "Token requis ou identifiant utilisateur introuvable.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var a = await _db.UserAddresses
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted);

        if (a == null)
            return ProblemX(404, "Introuvable", "Adresse introuvable.");

        var wasDefault = a.IsDefault;

        a.IsDeleted = true;
        a.DeletedAt = DateTime.UtcNow;
        a.IsDefault = false;

        await _db.SaveChangesAsync();

        if (wasDefault)
        {
            var next = await _db.UserAddresses
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (next != null)
            {
                next.IsDefault = true;
                await _db.SaveChangesAsync();
            }
        }

        await tx.CommitAsync();

        return NoContent();
    }
}
