using Hlouwa.Models;
using System.ComponentModel.DataAnnotations;
using Hlouwa.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hlouwa.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(AppDbContext db, IWebHostEnvironment env, ILogger<CategoriesController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    // -----------------------------
    // DTOs
    // -----------------------------
    public record CategoryDto(int Id, string Name, string Slug, string? ImageUrl, bool IsActive, int SortOrder);
    public record CategoryAdminDto(int Id, string Name, string Slug, string? ImageUrl, bool IsActive, int SortOrder, int ArticlesCount);

    public class CategoryUpsertDto
    {
        [Required, MaxLength(80)]
        public string Name { get; init; } = null!;

        [MaxLength(120)]
        public string? Slug { get; init; }

        public bool IsActive { get; init; } = true;
        public int SortOrder { get; init; } = 0;

        public IFormFile? Image { get; init; }
        public bool RemoveImage { get; init; } = false;
    }

    public class CategoryPatchDto
    {
        [MaxLength(80)]
        public string? Name { get; init; }

        [MaxLength(120)]
        public string? Slug { get; init; }

        public bool? IsActive { get; init; }
        public int? SortOrder { get; init; }
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

    // -----------------------------
    // PUBLIC: active categories
    // GET /api/categories
    // -----------------------------
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
    {
        try
        {
            var items = await _db.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.ImageUrl, c.IsActive, c.SortOrder))
                .ToListAsync();

            return Ok(items);
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<List<CategoryDto>>("GetAllCategories", ex);
        }
    }

    // GET /api/categories/by-slug/{slug}
    [HttpGet("by-slug/{slug}")]
    public async Task<ActionResult<CategoryDto>> GetBySlug(string slug)
    {
        try
        {
            slug = (slug ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(slug))
                return ProblemT<CategoryDto>(400, "Requête invalide", "Slug requis.");

            var c = await _db.Categories
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Slug == slug)
                .Select(x => new CategoryDto(x.Id, x.Name, x.Slug, x.ImageUrl, x.IsActive, x.SortOrder))
                .FirstOrDefaultAsync();

            return c == null
                ? ProblemT<CategoryDto>(404, "Introuvable", "Catégorie introuvable.")
                : Ok(c);
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<CategoryDto>("GetCategoryBySlug", ex);
        }
    }

    // -----------------------------
    // ADMIN: list all + articlesCount
    // GET /api/categories/admin
    // -----------------------------
    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public async Task<ActionResult<List<CategoryAdminDto>>> GetAllAdmin()
    {
        try
        {
            // Counts by category (only non-deleted articles)
            var counts = await _db.Articles
                .AsNoTracking()
                .Where(a => !a.IsDeleted && a.CategoryId != null)
                .GroupBy(a => a.CategoryId!.Value)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToListAsync();

            var dict = counts.ToDictionary(x => x.CategoryId, x => x.Count);

            var items = await _db.Categories
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(c => new CategoryAdminDto(
                    c.Id,
                    c.Name,
                    c.Slug,
                    c.ImageUrl,
                    c.IsActive,
                    c.SortOrder,
                    0 // filled after
                ))
                .ToListAsync();

            // merge counts (in-memory, safe)
            var result = items
                .Select(c => c with { ArticlesCount = dict.TryGetValue(c.Id, out var n) ? n : 0 })
                .ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<List<CategoryAdminDto>>("GetAllCategoriesAdmin", ex);
        }
    }

    // ADMIN get by id (clear route)
    // GET /api/categories/admin/{id}
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetByIdAdmin(int id)
    {
        try
        {
            var c = await _db.Categories
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Id == id)
                .Select(x => new CategoryDto(x.Id, x.Name, x.Slug, x.ImageUrl, x.IsActive, x.SortOrder))
                .FirstOrDefaultAsync();

            return c == null
                ? ProblemT<CategoryDto>(404, "Introuvable", "Catégorie introuvable.")
                : Ok(c);
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<CategoryDto>("GetCategoryByIdAdmin", ex);
        }
    }

    // -----------------------------
    // ADMIN: create
    // POST /api/categories
    // -----------------------------
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromForm] CategoryUpsertDto dto)
    {
        try
        {
            if (dto == null)
                return ProblemT<CategoryDto>(400, "Requête invalide", "Payload invalide.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var trimmedName = dto.Name.Trim();

            // Check unique name before hitting the DB unique index
            var nameExists = await _db.Categories.AnyAsync(c => !c.IsDeleted && c.Name == trimmedName);
            if (nameExists)
                return ProblemT<CategoryDto>(409, "Conflit", $"Une catégorie nommée '{trimmedName}' existe déjà.");

            var slug = await EnsureUniqueSlug(dto.Slug, trimmedName);

            string? imageUrl = null;
            if (dto.Image != null)
                imageUrl = await SaveCategoryImage(dto.Image);

            var cat = new Category
            {
                Name = trimmedName,
                Slug = slug,
                ImageUrl = imageUrl,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder
            };

            _db.Categories.Add(cat);
            await _db.SaveChangesAsync();

            var outDto = new CategoryDto(cat.Id, cat.Name, cat.Slug, cat.ImageUrl, cat.IsActive, cat.SortOrder);
            return CreatedAtAction(nameof(GetBySlug), new { slug = cat.Slug }, outDto);
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<CategoryDto>("CreateCategory", ex);
        }
    }

    // -----------------------------
    // ADMIN: update (PUT full)
    // PUT /api/categories/{id}
    // -----------------------------
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromForm] CategoryUpsertDto dto)
    {
        try
        {
            if (dto == null)
                return ProblemX(400, "Requête invalide", "Payload invalide.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var cat = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (cat == null)
                return ProblemX(404, "Introuvable", "Catégorie introuvable.");

            var trimmedName = dto.Name.Trim();

            var nameExists = await _db.Categories.AnyAsync(c => !c.IsDeleted && c.Name == trimmedName && c.Id != id);
            if (nameExists)
                return ProblemX(409, "Conflit", $"Une catégorie nommée '{trimmedName}' existe déjà.");

            var newSlug = await EnsureUniqueSlug(dto.Slug, trimmedName, cat.Id);

            cat.Name = trimmedName;
            cat.Slug = newSlug;
            cat.IsActive = dto.IsActive;
            cat.SortOrder = dto.SortOrder;

            if (dto.RemoveImage)
            {
                DeleteCategoryImageFile(cat.ImageUrl);
                cat.ImageUrl = null;
            }
            else if (dto.Image != null)
            {
                DeleteCategoryImageFile(cat.ImageUrl);
                cat.ImageUrl = await SaveCategoryImage(dto.Image);
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return ExceptionToProblemX("UpdateCategory", ex);
        }
    }

    // -----------------------------
    // ADMIN: patch (partial)
    // PATCH /api/categories/{id}
    // -----------------------------
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Patch(int id, [FromBody] CategoryPatchDto dto)
    {
        try
        {
            if (dto == null)
                return ProblemX(400, "Requête invalide", "Payload invalide.");

            var cat = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (cat == null)
                return ProblemX(404, "Introuvable", "Catégorie introuvable.");

            var nameChanged = false;

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                cat.Name = dto.Name.Trim();
                nameChanged = true;
            }

            if (dto.SortOrder.HasValue)
                cat.SortOrder = dto.SortOrder.Value;

            if (dto.IsActive.HasValue)
                cat.IsActive = dto.IsActive.Value;

            if (!string.IsNullOrWhiteSpace(dto.Slug))
            {
                cat.Slug = await EnsureUniqueSlug(dto.Slug, cat.Name, cat.Id);
            }
            else if (nameChanged)
            {
                // optional: keep slug in sync when name changes (comment/uncomment as you prefer)
                // cat.Slug = await EnsureUniqueSlug(null, cat.Name, cat.Id);
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return ExceptionToProblemX("PatchCategory", ex);
        }
    }

    // -----------------------------
    // ADMIN: reorder
    // PUT /api/categories/reorder
    // body: [3,1,2] => sortOrder 0..n
    // -----------------------------
    [Authorize(Roles = "Admin")]
    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder([FromBody] List<int> orderedIds)
    {
        try
        {
            if (orderedIds == null || orderedIds.Count == 0)
                return ProblemX(400, "Requête invalide", "Liste vide.");

            // unique check
            if (orderedIds.Distinct().Count() != orderedIds.Count)
                return ProblemX(400, "Requête invalide", "La liste contient des ids en double.");

            var cats = await _db.Categories
                .Where(c => !c.IsDeleted && orderedIds.Contains(c.Id))
                .ToListAsync();

            if (cats.Count != orderedIds.Count)
                return ProblemX(400, "Requête invalide", "Liste invalide: certains ids n'existent pas.");

            var map = cats.ToDictionary(c => c.Id);

            for (int i = 0; i < orderedIds.Count; i++)
                map[orderedIds[i]].SortOrder = i;

            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return ExceptionToProblemX("ReorderCategories", ex);
        }
    }

    // -----------------------------
    // ADMIN: delete (soft)
    // DELETE /api/categories/{id}
    // -----------------------------
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var cat = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (cat == null)
                return ProblemX(404, "Introuvable", "Catégorie introuvable.");

            var used = await _db.Articles.AnyAsync(a => !a.IsDeleted && a.CategoryId == id);
            if (used)
                return ProblemX(400, "Suppression impossible",
                    "Impossible de supprimer: catégorie utilisée par des articles. Désactive-la plutôt.");

            cat.IsDeleted = true;
            cat.DeletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return ExceptionToProblemX("DeleteCategory", ex);
        }
    }

    // -----------------------------
    // Image helpers
    // -----------------------------
    private static readonly HashSet<string> AllowedExts = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };

    private async Task<string> SaveCategoryImage(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExts.Contains(ext))
            throw new InvalidOperationException("Format d'image non supporté (jpg, png, webp uniquement).");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Image trop volumineuse (max 5 Mo).");

        var folder = Path.Combine(_env.WebRootPath, "uploads", "categories");
        Directory.CreateDirectory(folder);

        var fileName = $"cat_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(folder, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/categories/{fileName}";
    }

    private void DeleteCategoryImageFile(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return;
        var fullPath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(fullPath))
        {
            try { System.IO.File.Delete(fullPath); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete category image: {Path}", fullPath); }
        }
    }

    // -----------------------------
    // Helpers
    // -----------------------------
    private async Task<string> EnsureUniqueSlug(string? slug, string name, int? ignoreId = null)
    {
        var baseSlug = string.IsNullOrWhiteSpace(slug) ? Slugify(name) : Slugify(slug);
        var final = baseSlug;

        int i = 2;
        while (await _db.Categories.AnyAsync(c =>
                   !c.IsDeleted &&
                   c.Slug == final &&
                   (!ignoreId.HasValue || c.Id != ignoreId.Value)))
        {
            final = $"{baseSlug}-{i++}";
        }

        return final;
    }

    private static string Slugify(string text)
    {
        text = (text ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString("N");

        var chars = text
            .Replace("à", "a").Replace("â", "a").Replace("ä", "a")
            .Replace("ç", "c")
            .Replace("é", "e").Replace("è", "e").Replace("ê", "e").Replace("ë", "e")
            .Replace("î", "i").Replace("ï", "i")
            .Replace("ô", "o").Replace("ö", "o")
            .Replace("ù", "u").Replace("û", "u").Replace("ü", "u")
            .ToCharArray();

        var sb = new System.Text.StringBuilder(chars.Length);
        bool dash = false;

        foreach (var ch in chars)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                dash = false;
            }
            else
            {
                if (!dash)
                {
                    sb.Append('-');
                    dash = true;
                }
            }
        }

        var slug = sb.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N") : slug;
    }
}
