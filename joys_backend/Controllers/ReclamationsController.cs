using Hlouwa.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Hlouwa.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hlouwa.Controllers;

[ApiController]
[Route("api/reclamations")]
public class ReclamationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ReclamationsController> _logger;

    public ReclamationsController(AppDbContext db, IWebHostEnvironment env, ILogger<ReclamationsController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    // -----------------------------
    // DTOs
    // -----------------------------
    public record ReclamationSummaryDto(
        int Id,
        string FullName,
        string Email,
        string? Phone,
        string? Subject,
        ReclamationStatus Status,
        int? OrderId,
        DateTime CreatedAt
    );

    public record ReclamationDetailsDto(
        int Id,
        string FullName,
        string Email,
        string? Phone,
        string? Subject,
        string Message,
        ReclamationStatus Status,
        int? OrderId,
        string? AdminNote,
        DateTime CreatedAt
    );

    public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

    public class CreateReclamationDto
    {
        [Required, MaxLength(160)]
        public string FullName { get; init; } = null!;

        [Required, MaxLength(160), EmailAddress]
        public string Email { get; init; } = null!;

        [MaxLength(30)]
        public string? Phone { get; init; }

        [MaxLength(160)]
        public string? Subject { get; init; }

        [Required, MinLength(5)]
        public string Message { get; init; } = null!;

        public int? OrderId { get; init; } // optional link to order
    }

    public class UpdateReclamationDto
    {
        [Required]
        public ReclamationStatus Status { get; init; }

        [MaxLength(600)]
        public string? AdminNote { get; init; }
    }

    public class ReclamationAdminQuery
    {
        public ReclamationStatus? Status { get; set; }
        public string? Search { get; set; } // name/email/subject
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // -----------------------------
    // Response helpers (coherent)
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

    private string? TryGetUserIdIfAuthenticated()
    {
        if (User?.Identity?.IsAuthenticated != true) return null;
        var id = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(id) ? null : id;
    }

    private string GetUserIdOrEmpty()
        => User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    // =====================================================
    // POST /api/reclamations
    // Public OR authenticated
    // =====================================================
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateReclamationDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = TryGetUserIdIfAuthenticated();

            var email = dto.Email.Trim().ToLowerInvariant();
            var fullName = dto.FullName.Trim();
            var phone = dto.Phone?.Trim();
            var subject = dto.Subject?.Trim();
            var message = dto.Message.Trim();

            // Anti-spam minimal: same email + same message within 2 minutes
            var twoMinAgo = DateTime.UtcNow.AddMinutes(-2);
            var exists = await _db.Reclamations.AsNoTracking().AnyAsync(r =>
                !r.IsDeleted &&
                r.Email.ToLower() == email &&
                r.Message == message &&
                r.CreatedAt >= twoMinAgo);

            if (exists)
                return ProblemT<object>(409, "Conflit", "Réclamation déjà envoyée récemment. Merci de patienter.");

            // If orderId provided, ensure it exists; if authenticated, ensure it belongs to user
            if (dto.OrderId.HasValue)
            {
                var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == dto.OrderId.Value);
                if (order == null) return ProblemT<object>(400, "Requête invalide", "OrderId invalide.");

                if (userId != null && order.UserId != userId)
                    return ProblemT<object>(403, "Accès refusé", "Cette commande ne vous appartient pas.");
            }

            var rec = new Reclamation
            {
                UserId = userId,
                OrderId = dto.OrderId,

                FullName = fullName,
                Email = email,
                Phone = phone,
                Subject = subject,
                Message = message,

                Status = ReclamationStatus.New
            };

            _db.Reclamations.Add(rec);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                rec.Id,
                status = rec.Status.ToString(),
                rec.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<object>("CreateReclamation", ex);
        }
    }

    // =====================================================
    // GET /api/reclamations/me
    // Client: list his reclamations
    // =====================================================
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<List<ReclamationSummaryDto>>> GetMine()
    {
        try
        {
            var userId = GetUserIdOrEmpty();
            if (string.IsNullOrWhiteSpace(userId))
                return ProblemT<List<ReclamationSummaryDto>>(401, "Non authentifié", "Utilisateur non authentifié.");

            var list = await _db.Reclamations
                .AsNoTracking()
                .Where(r => !r.IsDeleted && r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReclamationSummaryDto(
                    r.Id, r.FullName, r.Email, r.Phone, r.Subject, r.Status, r.OrderId, r.CreatedAt
                ))
                .ToListAsync();

            return Ok(list);
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<List<ReclamationSummaryDto>>("GetMyReclamations", ex);
        }
    }

    // =====================================================
    // GET /api/reclamations/me/{id}
    // Client: details
    // =====================================================
    [Authorize]
    [HttpGet("me/{id:int}")]
    public async Task<ActionResult<ReclamationDetailsDto>> GetMineById(int id)
    {
        try
        {
            var userId = GetUserIdOrEmpty();
            if (string.IsNullOrWhiteSpace(userId))
                return ProblemT<ReclamationDetailsDto>(401, "Non authentifié", "Utilisateur non authentifié.");

            var r = await _db.Reclamations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == id && x.UserId == userId);

            if (r == null)
                return ProblemT<ReclamationDetailsDto>(404, "Introuvable", "Réclamation introuvable.");

            return Ok(new ReclamationDetailsDto(
                r.Id, r.FullName, r.Email, r.Phone, r.Subject, r.Message, r.Status, r.OrderId, r.AdminNote, r.CreatedAt
            ));
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<ReclamationDetailsDto>("GetMyReclamationById", ex);
        }
    }

    // =====================================================
    // ADMIN: GET /api/reclamations
    // Pagination + filters
    // =====================================================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<ReclamationSummaryDto>>> GetAll([FromQuery] ReclamationAdminQuery q)
    {
        try
        {
            q.Page = Math.Max(1, q.Page);
            q.PageSize = Math.Clamp(q.PageSize, 1, 100);

            var query = _db.Reclamations
                .AsNoTracking()
                .Include(r => r.User)
                .Where(r => !r.IsDeleted);

            if (q.Status.HasValue)
                query = query.Where(r => r.Status == q.Status.Value);

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = q.Search.Trim();
                query = query.Where(r =>
                    r.FullName.Contains(s) ||
                    r.Email.Contains(s) ||
                    (r.Subject != null && r.Subject.Contains(s)));
            }

            if (q.From.HasValue)
            {
                // treat From as UTC if you store UTC in DB
                query = query.Where(r => r.CreatedAt >= q.From.Value);
            }

            if (q.To.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= q.To.Value);
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .Select(r => new ReclamationSummaryDto(
                    r.Id, r.FullName, r.Email, r.Phone, r.Subject, r.Status, r.OrderId, r.CreatedAt
                ))
                .ToListAsync();

            return Ok(new PagedResult<ReclamationSummaryDto>(items, q.Page, q.PageSize, total));
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<PagedResult<ReclamationSummaryDto>>("AdminListReclamations", ex);
        }
    }

    // =====================================================
    // ADMIN: GET /api/reclamations/{id}
    // Details (admin)
    // =====================================================
    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReclamationDetailsDto>> GetById(int id)
    {
        try
        {
            var r = await _db.Reclamations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == id);

            if (r == null)
                return ProblemT<ReclamationDetailsDto>(404, "Introuvable", "Réclamation introuvable.");

            return Ok(new ReclamationDetailsDto(
                r.Id, r.FullName, r.Email, r.Phone, r.Subject, r.Message, r.Status, r.OrderId, r.AdminNote, r.CreatedAt
            ));
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<ReclamationDetailsDto>("AdminGetReclamationById", ex);
        }
    }

    // =====================================================
    // ADMIN: PUT /api/reclamations/{id}
    // Update status + note
    // =====================================================
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReclamationDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var rec = await _db.Reclamations.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            if (rec == null) return ProblemX(404, "Introuvable", "Réclamation introuvable.");

            rec.Status = dto.Status;
            rec.AdminNote = dto.AdminNote?.Trim();

            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return ExceptionToProblemX("AdminUpdateReclamation", ex);
        }
    }

    // =====================================================
    // ADMIN: DELETE /api/reclamations/{id}  (soft delete)
    // =====================================================
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var rec = await _db.Reclamations.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            if (rec == null) return ProblemX(404, "Introuvable", "Réclamation introuvable.");

            rec.IsDeleted = true;
            rec.DeletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return ExceptionToProblemX("AdminDeleteReclamation", ex);
        }
    }
}
