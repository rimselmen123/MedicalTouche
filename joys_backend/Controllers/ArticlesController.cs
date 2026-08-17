using Hlouwa.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hlouwa.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ArticlesController> _logger;

    private static readonly HashSet<string> AllowedImageExt = new(StringComparer.OrdinalIgnoreCase)
    { ".jpg", ".jpeg", ".png", ".webp" };

    private static readonly HashSet<string> AllowedImageMime = new(StringComparer.OrdinalIgnoreCase)
    { "image/jpeg", "image/png", "image/webp" };

    public ArticlesController(
        AppDbContext db,
        IConfiguration config,
        IWebHostEnvironment env,
        ILogger<ArticlesController> logger)
    {
        _db = db;
        _config = config;
        _env = env;
        _logger = logger;
    }

    // =====================================================
    // Response Helpers
    // =====================================================
    private ActionResult<T> ProblemT<T>(int status, string title, string detail, object? errors = null)
    {
        var pd = new ProblemDetails { Status = status, Title = title, Detail = detail, Instance = HttpContext.Request.Path };
        pd.Extensions["traceId"] = HttpContext.TraceIdentifier;
        if (errors != null) pd.Extensions["errors"] = errors;
        return StatusCode(status, pd);
    }

    private IActionResult ProblemX(int status, string title, string detail, object? errors = null)
    {
        var pd = new ProblemDetails { Status = status, Title = title, Detail = detail, Instance = HttpContext.Request.Path };
        pd.Extensions["traceId"] = HttpContext.TraceIdentifier;
        if (errors != null) pd.Extensions["errors"] = errors;
        return StatusCode(status, pd);
    }

    private IActionResult ExceptionToProblem(string scope, Exception ex)
    {
        _logger.LogError(ex, "{Scope} failed.", scope);
        var detail = _env.IsDevelopment() ? ex.ToString() : "Une erreur interne est survenue.";
        return ProblemX(500, $"Erreur serveur ({scope})", detail);
    }

    private ActionResult<T> ExceptionToProblemT<T>(string scope, Exception ex)
    {
        _logger.LogError(ex, "{Scope} failed.", scope);
        var detail = _env.IsDevelopment() ? ex.ToString() : "Une erreur interne est survenue.";
        return ProblemT<T>(500, $"Erreur serveur ({scope})", detail);
    }

    // =====================================================
    // DTOs
    // =====================================================
    public record ArticleListDto(int Id, string Title, string Slug, decimal Price, decimal? OldPrice, bool IsActive, bool IsFeatured, string? CoverUrl, string? CategoryName, int? CategoryId, DateTime CreatedAt);
    public record ArticleDetailsDto(int Id, string Title, string Slug, string? ShortDescription, string? Description, decimal Price, decimal? OldPrice, string? Sku, bool IsActive, bool IsFeatured, bool IsMadeToOrder, int? StockQuantity, int? CategoryId, string? CategoryName, DateTime CreatedAt, List<ArticleImageDto> Images, List<ArticleVariantDto> Variants);
    public record ArticleImageDto(int Id, string Url, bool IsCover, int SortOrder, string? Alt);
    public record ArticleVariantDto(int Id, string Name, decimal Price, bool IsDefault, int? StockQuantity, string? Sku);
    public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

    public class ArticleQuery
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public bool? Active { get; set; }
        public bool? Featured { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string Sort { get; set; } = "newest";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ArticleUpsertForm
    {
        [Required, MaxLength(140)] public string Title { get; set; } = null!;
        [MaxLength(180)] public string? Slug { get; set; }
        [MaxLength(300)] public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        [Range(0, 999999)] public decimal Price { get; set; }
        [Range(0, 999999)] public decimal? OldPrice { get; set; }
        [MaxLength(40)] public string? Sku { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; }
        public bool IsMadeToOrder { get; set; } = true;
        public int? StockQuantity { get; set; }
        public int? CategoryId { get; set; }
        public List<IFormFile>? Images { get; set; }
        public int? CoverIndex { get; set; }
        public string? VariantsJson { get; set; }
    }

    public class VariantInput
    {
        [Required, MaxLength(60)] public string Name { get; set; } = null!;
        [Range(0, 999999)] public decimal Price { get; set; }
        public bool IsDefault { get; set; }
        public int? StockQuantity { get; set; }
        [MaxLength(40)] public string? Sku { get; set; }
    }

    public class UpdateArticleDto
    {
        [MaxLength(140)] public string? Title { get; set; }
        [MaxLength(180)] public string? Slug { get; set; }
        [MaxLength(300)] public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public decimal? OldPrice { get; set; }
        [MaxLength(40)] public string? Sku { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsFeatured { get; set; }
        public bool? IsMadeToOrder { get; set; }
        public int? StockQuantity { get; set; }
        public int? CategoryId { get; set; }
    }

    // =====================================================
    // GET Actions
    // =====================================================
    [HttpGet]
    public async Task<ActionResult<PagedResult<ArticleListDto>>> GetAll([FromQuery] ArticleQuery q)
    {
        try
        {
            q.Page = Math.Max(1, q.Page);
            q.PageSize = Math.Clamp(q.PageSize, 1, 100);
            IQueryable<Article> query = _db.Articles.AsNoTracking().Where(a => !a.IsDeleted);

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = q.Search.Trim();
                query = query.Where(a => a.Title.Contains(s) || (a.Slug != null && a.Slug.Contains(s)) || (a.ShortDescription != null && a.ShortDescription.Contains(s)));
            }
            if (q.CategoryId.HasValue) query = query.Where(a => a.CategoryId == q.CategoryId.Value);
            if (q.Active.HasValue) query = query.Where(a => a.IsActive == q.Active.Value);

            query = q.Sort switch
            {
                "priceAsc" => query.OrderBy(a => a.Price).ThenByDescending(a => a.CreatedAt),
                "priceDesc" => query.OrderByDescending(a => a.Price).ThenByDescending(a => a.CreatedAt),
                _ => query.OrderByDescending(a => a.CreatedAt)
            };

            var total = await query.CountAsync();
            var items = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
                .Select(a => new ArticleListDto(
                    a.Id, a.Title, a.Slug, a.Price, a.OldPrice, a.IsActive, a.IsFeatured,
                    _db.ArticleImages.Where(i => i.ArticleId == a.Id).OrderByDescending(i => i.IsCover).ThenBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                    _db.Categories.Where(c => c.Id == a.CategoryId && !c.IsDeleted).Select(c => c.Name).FirstOrDefault(),
                    a.CategoryId, a.CreatedAt
                )).ToListAsync();

            return Ok(new PagedResult<ArticleListDto>(items, q.Page, q.PageSize, total));
        }
        catch (Exception ex) { return ProblemT<PagedResult<ArticleListDto>>(500, "Erreur serveur", ex.Message); }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ArticleDetailsDto>> GetById(int id)
    {
        var a = await _db.Articles.AsNoTracking().Include(x => x.Category).Include(x => x.Images).Include(x => x.Variants).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (a == null) return ProblemT<ArticleDetailsDto>(404, "Introuvable", "Article introuvable.");
        return Ok(ToDetailsDto(a));
    }

    [HttpGet("by-slug/{slug}")]
    public async Task<ActionResult<ArticleDetailsDto>> GetBySlug(string slug)
    {
        var a = await _db.Articles.AsNoTracking().Include(x => x.Category).Include(x => x.Images).Include(x => x.Variants).FirstOrDefaultAsync(x => !x.IsDeleted && x.Slug == slug);
        if (a == null) return ProblemT<ArticleDetailsDto>(404, "Introuvable", "Article introuvable.");
        return Ok(ToDetailsDto(a));
    }

    [HttpGet("{id:int}/images")]
    public async Task<ActionResult<List<ArticleImageDto>>> GetImages(int id)
    {
        var imgs = await _db.ArticleImages.AsNoTracking().Where(i => i.ArticleId == id).OrderBy(i => i.SortOrder).Select(i => new ArticleImageDto(i.Id, i.Url, i.IsCover, i.SortOrder, i.Alt)).ToListAsync();
        return Ok(imgs);
    }

    // =====================================================
    // POST / PUT / DELETE Management
    // =====================================================
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ArticleDetailsDto>> Create([FromForm] ArticleUpsertForm form)
    {
        var createdFiles = new List<string>();
        try
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            await using var tx = await _db.Database.BeginTransactionAsync();

            var slug = await EnsureUniqueSlug(form.Slug, form.Title);

            var article = new Article
            {
                Title = form.Title.Trim(),
                Slug = slug,
                ShortDescription = form.ShortDescription?.Trim(),
                Description = form.Description,
                Price = form.Price,
                OldPrice = form.OldPrice,
                Sku = form.Sku?.Trim(),
                IsActive = form.IsActive,
                IsFeatured = form.IsFeatured,
                IsMadeToOrder = form.IsMadeToOrder,
                StockQuantity = form.StockQuantity,
                CategoryId = form.CategoryId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Articles.Add(article);
            await _db.SaveChangesAsync(); // get ID

            // Variants
            var variants = ParseVariants(form.VariantsJson);
            if (variants.Count > 0)
            {
                NormalizeDefaultVariant(variants);
                foreach (var v in variants)
                {
                    article.Variants.Add(new ArticleVariant { ArticleId = article.Id, Name = v.Name.Trim(), Price = v.Price, IsDefault = v.IsDefault, StockQuantity = v.StockQuantity, Sku = v.Sku?.Trim() });
                }
            }

            // Images
            if (form.Images != null && form.Images.Count > 0)
            {
                ValidateImagesOrThrow(form.Images);
                int coverIndex = Math.Clamp(form.CoverIndex ?? 0, 0, form.Images.Count - 1);

                for (int i = 0; i < form.Images.Count; i++)
                {
                    var url = await SaveArticleImage(form.Images[i], article.Id);
                    createdFiles.Add(url);
                    article.Images.Add(new ArticleImage { ArticleId = article.Id, Url = url, IsCover = (i == coverIndex), SortOrder = i });
                }
            }

            if (article.Images.Count > 0 && !article.Images.Any(i => i.IsCover))
            {
                article.Images.OrderBy(i => i.SortOrder).First().IsCover = true;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var fresh = await _db.Articles.AsNoTracking().Include(x => x.Category).Include(x => x.Images).Include(x => x.Variants).FirstAsync(x => x.Id == article.Id);
            return CreatedAtAction(nameof(GetById), new { id = fresh.Id }, ToDetailsDto(fresh));
        }
        catch (Exception ex)
        {
            CleanupCreatedFiles(createdFiles);
            return ExceptionToProblemT<ArticleDetailsDto>("Create", ex);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateArticleDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var a = await _db.Articles.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (a == null) return ProblemX(404, "Introuvable", "Article introuvable.");

            if (dto.Title != null) a.Title = dto.Title.Trim();
            if (dto.Slug != null) a.Slug = await EnsureUniqueSlug(dto.Slug, dto.Title ?? a.Title, id);
            if (dto.ShortDescription != null) a.ShortDescription = dto.ShortDescription.Trim();
            if (dto.Description != null) a.Description = dto.Description;
            if (dto.Price.HasValue) a.Price = dto.Price.Value;
            if (dto.OldPrice.HasValue) a.OldPrice = dto.OldPrice.Value == 0 ? null : dto.OldPrice.Value;
            if (dto.Sku != null) a.Sku = dto.Sku.Trim();
            if (dto.IsActive.HasValue) a.IsActive = dto.IsActive.Value;
            if (dto.IsFeatured.HasValue) a.IsFeatured = dto.IsFeatured.Value;
            if (dto.IsMadeToOrder.HasValue) a.IsMadeToOrder = dto.IsMadeToOrder.Value;
            if (dto.StockQuantity.HasValue) a.StockQuantity = dto.StockQuantity.Value == 0 ? null : dto.StockQuantity.Value;
            if (dto.CategoryId.HasValue) a.CategoryId = dto.CategoryId.Value == 0 ? null : dto.CategoryId.Value;

            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return ExceptionToProblem("UpdateArticle", ex);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var a = await _db.Articles.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (a == null) return ProblemX(404, "Introuvable", "Article introuvable.");

            a.IsDeleted = true;
            a.DeletedAt = DateTime.UtcNow;
            a.IsActive = false;

            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return ExceptionToProblem("DeleteArticle", ex);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/images/{imageId:int}/cover")]
    public async Task<IActionResult> SetCover(int id, int imageId)
    {
        try
        {
            var target = await _db.ArticleImages
                .FirstOrDefaultAsync(i => i.Id == imageId && i.ArticleId == id);

            if (target == null) return ProblemX(404, "Introuvable", "Image introuvable.");

            // Clear cover on all images for this article, then set the target
            await _db.ArticleImages
                .Where(i => i.ArticleId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsCover, false));

            target.IsCover = true;
            await _db.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return ExceptionToProblem("SetCover", ex);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        var img = await _db.ArticleImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ArticleId == id);
        if (img == null) return ProblemX(404, "Introuvable", "Image introuvable.");

        bool wasCover = img.IsCover;
        _db.ArticleImages.Remove(img);
        await _db.SaveChangesAsync();
        TryDeleteOldImage(img.Url);

        if (wasCover)
        {
            var remaining = await _db.ArticleImages.Where(i => i.ArticleId == id).OrderBy(i => i.SortOrder).FirstOrDefaultAsync();
            if (remaining != null)
            {
                remaining.IsCover = true;
                await _db.SaveChangesAsync();
            }
        }
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/images")]
    public async Task<ActionResult<List<ArticleImageDto>>> AddImages(int id, [FromForm] List<IFormFile> images)
    {
        var createdFiles = new List<string>();
        try
        {
            var a = await _db.Articles.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();

            ValidateImagesOrThrow(images);
            int sort = a.Images.Count == 0 ? 0 : a.Images.Max(i => i.SortOrder) + 1;

            foreach (var f in images)
            {
                var url = await SaveArticleImage(f, id);
                createdFiles.Add(url);
                a.Images.Add(new ArticleImage { ArticleId = id, Url = url, IsCover = false, SortOrder = sort++ });
            }
            if (a.Images.Any() && !a.Images.Any(i => i.IsCover)) a.Images.First().IsCover = true;
            await _db.SaveChangesAsync();
            return Ok(a.Images.Select(i => new ArticleImageDto(i.Id, i.Url, i.IsCover, i.SortOrder, i.Alt)).ToList());
        }
        catch (Exception ex) { CleanupCreatedFiles(createdFiles); return ExceptionToProblemT<List<ArticleImageDto>>("AddImages", ex); }
    }

    // =====================================================
    // Helpers
    // =====================================================
    private ArticleDetailsDto ToDetailsDto(Article a) => new(
        a.Id, a.Title, a.Slug, a.ShortDescription, a.Description, a.Price, a.OldPrice, a.Sku,
        a.IsActive, a.IsFeatured, a.IsMadeToOrder, a.StockQuantity, a.CategoryId, a.Category?.Name, a.CreatedAt,
        a.Images.Select(i => new ArticleImageDto(i.Id, i.Url, i.IsCover, i.SortOrder, i.Alt)).ToList(),
        a.Variants.Select(v => new ArticleVariantDto(v.Id, v.Name, v.Price, v.IsDefault, v.StockQuantity, v.Sku)).ToList()
    );

    private List<VariantInput> ParseVariants(string? j)
    {
        if (string.IsNullOrWhiteSpace(j)) return new List<VariantInput>();
        try { return JsonSerializer.Deserialize<List<VariantInput>>(j, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new(); }
        catch { return new List<VariantInput>(); }
    }

    private static void NormalizeDefaultVariant(List<VariantInput> v) { if (v.Count > 0 && !v.Any(x => x.IsDefault)) v[0].IsDefault = true; }

    private static void ValidateImagesOrThrow(List<IFormFile> l) { foreach (var f in l) { if (f.Length > 10000000) throw new Exception("Max 10MB"); if (!AllowedImageExt.Contains(Path.GetExtension(f.FileName))) throw new Exception("Ext invalid"); } }

    private async Task<string> EnsureUniqueSlug(string? desired, string title, int? ignoreId = null)
    {
        string baseSlug = Slugify(string.IsNullOrWhiteSpace(desired) ? title : desired);
        string slug = baseSlug;
        int i = 2;
        while (await _db.Articles.AnyAsync(a => a.Slug == slug && (!ignoreId.HasValue || a.Id != ignoreId.Value) && !a.IsDeleted))
        {
            slug = $"{baseSlug}-{i++}";
        }
        return slug;
    }

    private string Slugify(string text) => text.ToLowerInvariant().Replace(" ", "-").Trim('-');

    private string ResolveWebRoot() => _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");

    private async Task<string> SaveArticleImage(IFormFile f, int id)
    {
        var root = ResolveWebRoot();
        var folder = Path.Combine(root, "uploads", "articles");
        Directory.CreateDirectory(folder);
        var filename = $"a{id}_{Guid.NewGuid():N}{Path.GetExtension(f.FileName)}";
        var path = Path.Combine(folder, filename);
        await using var s = new FileStream(path, FileMode.Create);
        await f.CopyToAsync(s);
        return $"/uploads/articles/{filename}";
    }

    private void TryDeleteOldImage(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        var path = Path.Combine(ResolveWebRoot(), url.TrimStart('/', '\\'));
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }

    private void CleanupCreatedFiles(List<string> urls) { foreach (var u in urls) TryDeleteOldImage(u); }

    // Content endpoints for blobs (optional)
    [HttpGet("{id:int}/images/{imageId:int}/content")]
    public IActionResult GetImageContent(int id, int imageId) { return NotFound(); } // Placeholder if unused
}

