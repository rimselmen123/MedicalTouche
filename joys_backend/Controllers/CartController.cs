using System.Security.Claims;
using Hlouwa.DTOs;
using Hlouwa.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hlouwa.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<CartController> _logger;

    public CartController(AppDbContext db, IWebHostEnvironment env, ILogger<CartController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
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

    private string GetUserId()
        => User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    // -----------------------------
    // GET /api/cart
    // -----------------------------
    [HttpGet]
    public async Task<ActionResult<CartDto>> Get(CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return ProblemT<CartDto>(401, "Non authentifié", "Utilisateur non authentifié.");

            var cart = await GetOrCreateCart(userId, ct);

            // Always recalc server-side (price changes etc.)
            await RecalculateCart(cart, ct);

            return Ok(ToDto(cart));
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<CartDto>("GetCart", ex);
        }
    }

    // -----------------------------
    // POST /api/cart/items
    // (merge if same article+variant exists)
    // -----------------------------
    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddCartItemDto dto, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return ProblemT<CartDto>(401, "Non authentifié", "Utilisateur non authentifié.");

            if (dto == null) return ProblemT<CartDto>(400, "Requête invalide", "Payload invalide.");
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var qty = Math.Clamp(dto.Quantity, 1, 999);

            var cart = await GetOrCreateCart(userId, ct);

            // Load article (+variants +images)
            var article = await _db.Articles
                .Include(a => a.Variants)
                .Include(a => a.Images)
                .FirstOrDefaultAsync(a => !a.IsDeleted && a.Id == dto.ArticleId, ct);

            if (article == null)
                return ProblemT<CartDto>(400, "Requête invalide", "Article introuvable.");

            if (!article.IsActive)
                return ProblemT<CartDto>(400, "Requête invalide", "Produit indisponible.");

            ArticleVariant? variant = null;
            if (dto.VariantId.HasValue)
            {
                variant = article.Variants.FirstOrDefault(v => v.Id == dto.VariantId.Value);
                if (variant == null)
                    return ProblemT<CartDto>(400, "Requête invalide", "Variant introuvable.");
            }

            // merge logic
            var existing = cart.Items.FirstOrDefault(i =>
                i.ArticleId == article.Id && i.ArticleVariantId == variant?.Id);

            if (existing != null)
            {
                existing.Quantity = Math.Clamp(existing.Quantity + qty, 1, 999);
            }
            else
            {
                var imageUrl =
                    article.Images.OrderBy(i => i.SortOrder).FirstOrDefault(i => i.IsCover)?.Url
                    ?? article.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault();

                cart.Items.Add(new CartItem
                {
                    ArticleId = article.Id,
                    ArticleVariantId = variant?.Id,

                    Name = article.Title,
                    VariantName = variant?.Name,
                    Sku = variant?.Sku ?? article.Sku,
                    ImageUrl = imageUrl,

                    Quantity = qty
                });
            }

            await RecalculateCart(cart, ct);
            return Ok(ToDto(cart));
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<CartDto>("AddCartItem", ex);
        }
    }

    // -----------------------------
    // PUT /api/cart/items/{id}
    // -----------------------------
    [HttpPut("items/{id:int}")]
    public async Task<ActionResult<CartDto>> UpdateItem(int id, [FromBody] UpdateCartItemDto dto, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return ProblemT<CartDto>(401, "Non authentifié", "Utilisateur non authentifié.");

            if (dto == null) return ProblemT<CartDto>(400, "Requête invalide", "Payload invalide.");
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var cart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, ct);

            if (cart == null)
                cart = await GetOrCreateCart(userId, ct);

            var item = cart.Items.FirstOrDefault(i => i.Id == id);
            if (item == null)
                return ProblemT<CartDto>(404, "Introuvable", "Item introuvable.");

            item.Quantity = Math.Clamp(dto.Quantity, 1, 999);

            await RecalculateCart(cart, ct);
            return Ok(ToDto(cart));
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<CartDto>("UpdateCartItem", ex);
        }
    }

    // -----------------------------
    // DELETE /api/cart/items/{id}
    // -----------------------------
    [HttpDelete("items/{id:int}")]
    public async Task<ActionResult<CartDto>> DeleteItem(int id, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return ProblemT<CartDto>(401, "Non authentifié", "Utilisateur non authentifié.");

            var cart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, ct);

            if (cart == null)
                cart = await GetOrCreateCart(userId, ct);

            var item = cart.Items.FirstOrDefault(i => i.Id == id);
            if (item == null)
                return ProblemT<CartDto>(404, "Introuvable", "Item introuvable.");

            _db.CartItems.Remove(item);

            await RecalculateCart(cart, ct);
            return Ok(ToDto(cart));
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<CartDto>("DeleteCartItem", ex);
        }
    }

    // -----------------------------
    // POST /api/cart/clear
    // -----------------------------
    [HttpPost("clear")]
    public async Task<ActionResult<CartDto>> Clear(CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return ProblemT<CartDto>(401, "Non authentifié", "Utilisateur non authentifié.");

            var cart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, ct);

            if (cart == null)
                cart = await GetOrCreateCart(userId, ct);

            if (cart.Items.Count > 0)
                _db.CartItems.RemoveRange(cart.Items);

            cart.Subtotal = 0;
            cart.DiscountTotal = 0;
            cart.DeliveryFee = 0;
            cart.Total = 0;

            await _db.SaveChangesAsync(ct);
            return Ok(ToDto(cart));
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<CartDto>("ClearCart", ex);
        }
    }

    // =====================================================
    // Internal helpers
    // =====================================================
    private async Task<Cart> GetOrCreateCart(string userId, CancellationToken ct)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart != null) return cart;

        cart = new Cart
        {
            UserId = userId,
            Currency = "TND"
        };

        _db.Carts.Add(cart);
        await _db.SaveChangesAsync(ct);

        // reload with items
        return await _db.Carts.Include(c => c.Items).FirstAsync(c => c.Id == cart.Id, ct);
    }

    private async Task RecalculateCart(Cart cart, CancellationToken ct)
    {
        // reload needed relations for repricing
        var itemIds = cart.Items.Select(i => i.Id).ToList();

        var items = await _db.CartItems
            .Where(i => itemIds.Contains(i.Id))
            .ToListAsync(ct);

        // Load all needed articles/variants in one go (avoid N+1)
        var articleIds = items.Select(i => i.ArticleId).Distinct().ToList();
        var articles = await _db.Articles
            .Include(a => a.Variants)
            .Where(a => !a.IsDeleted && articleIds.Contains(a.Id))
            .ToListAsync(ct);

        var articlesMap = articles.ToDictionary(a => a.Id);

        foreach (var it in items)
        {
            if (!articlesMap.TryGetValue(it.ArticleId, out var a) || !a.IsActive)
            {
                // product removed or inactive -> remove item
                _db.CartItems.Remove(it);
                continue;
            }

            ArticleVariant? v = null;
            if (it.ArticleVariantId.HasValue)
                v = a.Variants.FirstOrDefault(x => x.Id == it.ArticleVariantId.Value);

            // variant missing -> remove
            if (it.ArticleVariantId.HasValue && v == null)
            {
                _db.CartItems.Remove(it);
                continue;
            }

            it.UnitPrice = v?.Price ?? a.Price;

            // keep names/snap in sync (optional but clean)
            it.Name = a.Title;
            it.VariantName = v?.Name;
            it.Sku = v?.Sku ?? a.Sku;
        }

        // totals (Sprint 1 = base, no promo/shipping yet)
        // NOTE: if items removed above, subtotal must be computed after SaveChanges
        await _db.SaveChangesAsync(ct);

        // reload cart items after removals
        cart = await _db.Carts.Include(c => c.Items).FirstAsync(c => c.Id == cart.Id, ct);

        cart.Subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        cart.DiscountTotal = 0;
        cart.DeliveryFee = 0;
        cart.Total = cart.Subtotal - cart.DiscountTotal + cart.DeliveryFee;

        await _db.SaveChangesAsync(ct);
    }

    private static CartDto ToDto(Cart c)
    {
        var items = c.Items
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new CartItemDto(
                i.Id,
                i.ArticleId,
                i.ArticleVariantId,
                i.Name,
                i.VariantName,
                i.Sku,
                i.ImageUrl,
                i.UnitPrice,
                i.Quantity,
                i.UnitPrice * i.Quantity
            ))
            .ToList();

        return new CartDto(
            c.Id,
            c.Currency,
            c.Subtotal,
            c.DiscountTotal,
            c.DeliveryFee,
            c.Total,
            items,
            c.UpdatedAt ?? c.CreatedAt
        );
    }
}
