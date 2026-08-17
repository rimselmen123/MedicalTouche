using Hlouwa.Enums;
using Hlouwa.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Hlouwa.Controllers
{
    [ApiController]
    [Route("api/stock")]
    public class StockController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<StockController> _logger;

        public StockController(AppDbContext db, IWebHostEnvironment env, ILogger<StockController> logger)
        {
            _db = db;
            _env = env;
            _logger = logger;
        }

        // -----------------------------
        // Response helpers
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
        // DTOs
        // -----------------------------
        public record AdjustStockDto(int ArticleId, int? VariantId, decimal Delta, string? Note);

        // -----------------------------
        // ADMIN: Adjust Stock
        // POST /api/stock/adjust
        // -----------------------------
        [Authorize(Roles = "Admin")]
        [HttpPost("adjust")]
        public async Task<IActionResult> Adjust([FromBody] AdjustStockDto dto)
        {
            try
            {
                if (dto.Delta == 0) 
                    return ProblemX(400, "Requête invalide", "Delta ne peut pas être 0.");

                var stock = await _db.StockItems
                    .FirstOrDefaultAsync(s => s.ArticleId == dto.ArticleId && s.ArticleVariantId == dto.VariantId);

                if (stock == null)
                {
                    stock = new StockItem
                    {
                        ArticleId = dto.ArticleId,
                        ArticleVariantId = dto.VariantId,
                        OnHand = 0,
                        Reserved = 0
                    };
                    _db.StockItems.Add(stock);
                }

                if (stock.OnHand + dto.Delta < 0)
                    return ProblemX(400, "Requête invalide", "OnHand ne peut pas devenir négatif.");

                stock.OnHand += dto.Delta;

                _db.StockMovements.Add(new StockMovement
                {
                    ArticleId = dto.ArticleId,
                    ArticleVariantId = dto.VariantId,
                    Type = StockMovementType.Adjust,
                    Quantity = Math.Abs(dto.Delta),
                    RefType = "Admin",
                    RefId = GetUserId(),
                    Note = dto.Note ?? (dto.Delta > 0 ? "Adjust IN" : "Adjust OUT"),
                    ActorUserId = GetUserId()
                });

                await _db.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return ExceptionToProblemX("AdjustStock", ex);
            }
        }

        // -----------------------------
        // ADMIN: Get Stock Items
        // GET /api/stock/items
        // -----------------------------
        [Authorize(Roles = "Admin")]
        [HttpGet("items")]
        public async Task<IActionResult> GetItems()
        {
            try
            {
                var items = await _db.StockItems
                    .AsNoTracking()
                    .OrderBy(x => x.ArticleId)
                    .Select(x => new
                    {
                        x.ArticleId,
                        x.ArticleVariantId,
                        x.OnHand,
                        x.Reserved,
                        Available = x.OnHand - x.Reserved
                    })
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return ExceptionToProblemX("GetStockItems", ex);
            }
        }

        // -----------------------------
        // ADMIN: Get Stock Movements (historique)
        // GET /api/stock/movements?articleId=5&limit=100
        // -----------------------------
        [Authorize(Roles = "Admin")]
        [HttpGet("movements")]
        public async Task<IActionResult> GetMovements([FromQuery] int? articleId, [FromQuery] int? variantId, [FromQuery] int limit = 100)
        {
            try
            {
                var query = _db.StockMovements.AsNoTracking();

                if (articleId.HasValue)
                    query = query.Where(m => m.ArticleId == articleId.Value);

                if (variantId.HasValue)
                    query = query.Where(m => m.ArticleVariantId == variantId.Value);

                var movements = await query
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(Math.Min(limit, 1000))
                    .Select(m => new
                    {
                        m.Id,
                        m.ArticleId,
                        m.ArticleVariantId,
                        m.Type,
                        m.Quantity,
                        m.RefType,
                        m.RefId,
                        m.Note,
                        m.ActorUserId,
                        m.CreatedAt
                    })
                    .ToListAsync();

                return Ok(movements);
            }
            catch (Exception ex)
            {
                return ExceptionToProblemX("GetStockMovements", ex);
            }
        }

        // -----------------------------
        // ADMIN: Get Stock Reservations
        // GET /api/stock/reservations?status=Active
        // -----------------------------
        [Authorize(Roles = "Admin")]
        [HttpGet("reservations")]
        public async Task<IActionResult> GetReservations([FromQuery] StockReservationStatus? status)
        {
            try
            {
                var query = _db.StockReservations
                    .Include(r => r.Items)
                    .Include(r => r.Order)
                    .AsNoTracking();

                if (status.HasValue)
                    query = query.Where(r => r.Status == status.Value);

                var reservations = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new
                    {
                        r.Id,
                        r.OrderId,
                        OrderNumber = r.Order.OrderNumber,
                        r.Status,
                        r.ExpiresAt,
                        r.Reason,
                        r.CreatedAt,
                        Items = r.Items.Select(i => new
                        {
                            i.ArticleId,
                            i.ArticleVariantId,
                            i.Name,
                            i.VariantName,
                            i.Quantity
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(reservations);
            }
            catch (Exception ex)
            {
                return ExceptionToProblemX("GetReservations", ex);
            }
        }

        // -----------------------------
        // INTERNAL: Reserve for Order
        // -----------------------------
        [NonAction]
        public async Task ReserveForOrderAsync(int orderId, string actorUserId, TimeSpan ttl)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new InvalidOperationException("Order not found.");

            if (order.PaymentMethod != PaymentMethod.Online)
                return;

            if (await _db.StockReservations.AnyAsync(r => r.OrderId == orderId))
                return;

            var expiresAt = DateTime.UtcNow.Add(ttl);

            var reservation = new StockReservation
            {
                OrderId = orderId,
                Status = StockReservationStatus.Active,
                ExpiresAt = expiresAt,
                Reason = "Online checkout reserve"
            };

            foreach (var it in order.Items)
            {
                var qty = (decimal)it.Quantity;

                var stock = await _db.StockItems
                    .FirstOrDefaultAsync(s => s.ArticleId == it.ArticleId && s.ArticleVariantId == it.ArticleVariantId);

                if (stock == null)
                {
                    stock = new StockItem
                    {
                        ArticleId = it.ArticleId,
                        ArticleVariantId = it.ArticleVariantId,
                        OnHand = 0,
                        Reserved = 0
                    };
                    _db.StockItems.Add(stock);
                    await _db.SaveChangesAsync();
                }

                var available = stock.OnHand - stock.Reserved;
                if (available < qty)
                    throw new InvalidOperationException($"Stock insuffisant pour '{it.Name}'.");

                stock.Reserved += qty;

                reservation.Items.Add(new StockReservationItem
                {
                    ArticleId = it.ArticleId,
                    ArticleVariantId = it.ArticleVariantId,
                    Name = it.Name,
                    VariantName = it.VariantName,
                    Quantity = qty
                });

                _db.StockMovements.Add(new StockMovement
                {
                    ArticleId = it.ArticleId,
                    ArticleVariantId = it.ArticleVariantId,
                    Type = StockMovementType.Reserve,
                    Quantity = qty,
                    RefType = "Order",
                    RefId = order.OrderNumber,
                    Note = $"Reserve until {expiresAt:O}",
                    ActorUserId = actorUserId
                });
            }

            _db.StockReservations.Add(reservation);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        // -----------------------------
        // INTERNAL: Commit for Order
        // -----------------------------
        [NonAction]
        public async Task CommitForOrderAsync(int orderId, string actorUserId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            var reservation = await _db.StockReservations
                .Include(r => r.Order)
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.OrderId == orderId);

            if (reservation == null)
                throw new InvalidOperationException("Reservation not found.");

            if (reservation.Status == StockReservationStatus.Committed)
                return;

            if (reservation.Status is StockReservationStatus.Released or StockReservationStatus.Expired)
                throw new InvalidOperationException("Reservation already released/expired.");

            foreach (var ri in reservation.Items)
            {
                var stock = await _db.StockItems
                    .FirstOrDefaultAsync(s => s.ArticleId == ri.ArticleId && s.ArticleVariantId == ri.ArticleVariantId);

                if (stock == null)
                    throw new InvalidOperationException("StockItem missing.");

                stock.Reserved -= ri.Quantity;
                stock.OnHand -= ri.Quantity;

                _db.StockMovements.Add(new StockMovement
                {
                    ArticleId = ri.ArticleId,
                    ArticleVariantId = ri.ArticleVariantId,
                    Type = StockMovementType.Out,
                    Quantity = ri.Quantity,
                    RefType = "Order",
                    RefId = reservation.Order.OrderNumber,
                    Note = "Commit after payment success",
                    ActorUserId = actorUserId
                });
            }

            reservation.Status = StockReservationStatus.Committed;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        // -----------------------------
        // INTERNAL: Release for Order
        // -----------------------------
        [NonAction]
        public async Task ReleaseForOrderAsync(int orderId, string actorUserId, string reason)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            var reservation = await _db.StockReservations
                .Include(r => r.Order)
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.OrderId == orderId);

            if (reservation == null)
                return;

            if (reservation.Status is StockReservationStatus.Released or StockReservationStatus.Expired)
                return;

            if (reservation.Status == StockReservationStatus.Committed)
                throw new InvalidOperationException("Cannot release committed reservation.");

            foreach (var ri in reservation.Items)
            {
                var stock = await _db.StockItems
                    .FirstOrDefaultAsync(s => s.ArticleId == ri.ArticleId && s.ArticleVariantId == ri.ArticleVariantId);

                if (stock == null) continue;

                stock.Reserved -= ri.Quantity;

                _db.StockMovements.Add(new StockMovement
                {
                    ArticleId = ri.ArticleId,
                    ArticleVariantId = ri.ArticleVariantId,
                    Type = StockMovementType.Release,
                    Quantity = ri.Quantity,
                    RefType = "Order",
                    RefId = reservation.Order.OrderNumber,
                    Note = reason,
                    ActorUserId = actorUserId
                });
            }

            reservation.Status = StockReservationStatus.Released;
            reservation.Reason = reason;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        // -----------------------------
        // SYSTEM: Expire Reservations
        // POST /api/stock/expire-reservations
        // -----------------------------
        [Authorize(Roles = "Admin")]
        [HttpPost("expire-reservations")]
        public async Task<IActionResult> ExpireReservations()
        {
            try
            {
                var utcNow = DateTime.UtcNow;
                var toExpire = await _db.StockReservations
                    .Include(r => r.Order)
                    .Include(r => r.Items)
                    .Where(r => r.Status == StockReservationStatus.Active && r.ExpiresAt <= utcNow)
                    .ToListAsync();

                var count = 0;

                foreach (var r in toExpire)
                {
                    await ReleaseForOrderAsync(r.OrderId, actorUserId: "SYSTEM", reason: "Reservation expired");
                    r.Status = StockReservationStatus.Expired;
                    count++;
                }

                await _db.SaveChangesAsync();
                return Ok(new { expired = count });
            }
            catch (Exception ex)
            {
                return ExceptionToProblemX("ExpireReservations", ex);
            }
        }
    }
}
