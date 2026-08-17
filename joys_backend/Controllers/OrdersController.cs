using Hlouwa.DTOs;
using Hlouwa.Enums;
using Hlouwa.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Hlouwa.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(AppDbContext db, IWebHostEnvironment env, ILogger<OrdersController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

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

    // =====================================================
    // POST /api/orders  (Client) create order
    // =====================================================
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<OrderSummaryDto>> PlaceOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return ProblemT<OrderSummaryDto>(401, "Non authentifié", "Utilisateur non authentifié.");

            if (dto == null)
                return ProblemT<OrderSummaryDto>(400, "Requête invalide", "Payload invalide.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (dto.Items == null || dto.Items.Count == 0)
                return ProblemT<OrderSummaryDto>(400, "Requête invalide", "Commande vide.");

            if (dto.ShippingAddress == null)
                return ProblemT<OrderSummaryDto>(400, "Requête invalide", "Adresse de livraison requise.");

            var method = dto.PaymentMethod;
            var provider = dto.Provider ?? PaymentProvider.None;

            if (method == PaymentMethod.Online && provider == PaymentProvider.None)
                return ProblemT<OrderSummaryDto>(400, "Requête invalide", "PaymentProvider requis pour paiement en ligne.");

            // Load articles + images + variants
            var articleIds = dto.Items.Select(i => i.ArticleId).Distinct().ToList();

            var articles = await _db.Articles
                .Include(a => a.Images)
                .Include(a => a.Variants)
                .Where(a => !a.IsDeleted && articleIds.Contains(a.Id))
                .ToListAsync();

            if (articles.Count != articleIds.Count)
                return ProblemT<OrderSummaryDto>(400, "Requête invalide", "Un ou plusieurs articles introuvables.");

            if (articles.Any(a => !a.IsActive))
                return ProblemT<OrderSummaryDto>(400, "Requête invalide", "Un ou plusieurs produits sont indisponibles.");

            var order = new Order
            {
                UserId = userId,
                OrderNumber = GenerateOrderNumberSafe(),
                Status = method == PaymentMethod.Online ? OrderStatus.AwaitingPayment : OrderStatus.Pending,
                PaymentMethod = method,
                PaymentProvider = method == PaymentMethod.Online ? provider : PaymentProvider.None,
                PaymentStatus = PaymentStatus.Pending,
                Currency = "TND",

                CustomerNote = dto.CustomerNote?.Trim(),
                RequestedDeliveryDate = dto.RequestedDeliveryDate,
                DeliveryTimeSlot = dto.DeliveryTimeSlot?.Trim(),

                ShippingAddress = new OrderAddressSnapshot
                {
                    FullName = dto.ShippingAddress.FullName?.Trim(),
                    Phone = dto.ShippingAddress.Phone?.Trim(),
                    Line1 = dto.ShippingAddress.Line1?.Trim(),
                    Line2 = dto.ShippingAddress.Line2?.Trim(),
                    City = dto.ShippingAddress.City?.Trim(),
                    PostalCode = dto.ShippingAddress.PostalCode?.Trim(),
                    Governorate = dto.ShippingAddress.Governorate?.Trim(),
                    CountryCode = "TN"
                }
            };

            foreach (var item in dto.Items)
            {
                var a = articles.First(x => x.Id == item.ArticleId);
                var qty = Math.Max(1, item.Quantity);

                ArticleVariant? variant = null;
                if (item.VariantId.HasValue)
                {
                    variant = a.Variants.FirstOrDefault(v => v.Id == item.VariantId.Value);
                    if (variant == null)
                        return ProblemT<OrderSummaryDto>(400, "Requête invalide",
                            $"Variant introuvable pour l'article '{a.Title}'.");
                }

                var unitPrice = variant?.Price ?? a.Price;

                var imageUrl =
                    a.Images.OrderBy(i => i.SortOrder).FirstOrDefault(i => i.IsCover)?.Url
                    ?? a.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault();

                order.Items.Add(new OrderItem
                {
                    ArticleId = a.Id,
                    ArticleVariantId = variant?.Id,
                    Name = a.Title,
                    VariantName = variant?.Name,
                    Sku = variant?.Sku ?? a.Sku,
                    ImageUrl = imageUrl,
                    UnitPrice = unitPrice,
                    Quantity = qty
                });
            }

            // Totals (Sprint 0: simple)
            order.Subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
            order.DiscountTotal = 0;
            order.DeliveryFee = 0;
            order.Total = order.Subtotal - order.DiscountTotal + order.DeliveryFee;

            order.StatusHistory.Add(new OrderStatusHistory
            {
                FromStatus = OrderStatus.Pending,
                ToStatus = order.Status,
                PaymentStatusSnapshot = order.PaymentStatus,
                Note = "Order created",
                ChangedByUserId = userId
            });

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // Reserve stock for online payment orders
            if (method == PaymentMethod.Online)
            {
                try
                {
                    await ReserveStockForOrderAsync(order.Id, userId);
                }
                catch (Exception reserveEx)
                {
                    _logger.LogError(reserveEx, "Stock reservation failed for order {OrderId}", order.Id);
                    return ProblemT<OrderSummaryDto>(409, "Stock insuffisant",
                        "La commande a été créée mais le stock est insuffisant. Annulation en cours.");
                }
            }

            // Clear the customer's cart after successful order placement
            try
            {
                var cart = await _db.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart != null && cart.Items.Count > 0)
                {
                    _db.CartItems.RemoveRange(cart.Items);
                    cart.Subtotal = 0;
                    cart.DiscountTotal = 0;
                    cart.DeliveryFee = 0;
                    cart.Total = 0;
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception cartEx)
            {
                _logger.LogWarning(cartEx, "Cart clear after order failed (non-critical). OrderId={OrderId}", order.Id);
            }

            return Ok(new OrderSummaryDto(
                order.Id,
                order.OrderNumber,
                order.Status,
                order.PaymentMethod,
                order.PaymentStatus,
                order.Total,
                order.Currency,
                order.CreatedAt
            ));
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<OrderSummaryDto>("PlaceOrder", ex);
        }
    }

    // =====================================================
    // GET /api/orders/me  (Client)
    // =====================================================
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<List<OrderSummaryDto>>> GetMyOrders()
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return ProblemT<List<OrderSummaryDto>>(401, "Non authentifié", "Utilisateur non authentifié.");

            var orders = await _db.Orders
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderSummaryDto(
                    o.Id,
                    o.OrderNumber,
                    o.Status,
                    o.PaymentMethod,
                    o.PaymentStatus,
                    o.Total,
                    o.Currency,
                    o.CreatedAt
                ))
                .ToListAsync();

            return Ok(orders);
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<List<OrderSummaryDto>>("GetMyOrders", ex);
        }
    }

    // =====================================================
    // GET /api/orders/me/{id}  (Client details)
    // =====================================================
    [Authorize]
    [HttpGet("me/{id:int}")]
    public async Task<ActionResult<OrderDetailsDto>> GetMyOrderDetails(int id)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return ProblemT<OrderDetailsDto>(401, "Non authentifié", "Utilisateur non authentifié.");

            var o = await _db.Orders
                .AsNoTracking()
                .Include(x => x.Items)
                .Include(x => x.Payments.OrderByDescending(p => p.CreatedAt))
                .Include(x => x.StatusHistory.OrderBy(h => h.CreatedAt))
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (o == null)
                return ProblemT<OrderDetailsDto>(404, "Introuvable", "Commande introuvable.");

            return Ok(ToDetailsDto(o));
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<OrderDetailsDto>("GetMyOrderDetails", ex);
        }
    }

    // =====================================================
    // PATCH /api/orders/me/{id}/cancel
    // =====================================================
    [Authorize]
    [HttpPatch("me/{id:int}/cancel")]
    public async Task<IActionResult> CancelMyOrder(int id)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return ProblemX(401, "Non authentifié", "Utilisateur non authentifié.");

            var o = await _db.Orders
                .Include(x => x.StatusHistory)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (o == null)
                return ProblemX(404, "Introuvable", "Commande introuvable.");

            if (o.Status is OrderStatus.Preparing or OrderStatus.OutForDelivery or OrderStatus.Delivered)
                return ProblemX(400, "Annulation impossible", "Commande déjà en traitement, annulation impossible.");

            if (o.PaymentStatus == PaymentStatus.Paid)
                return ProblemX(400, "Annulation impossible", "Commande payée. Utilise le process de remboursement.");

            var from = o.Status;

            o.Status = OrderStatus.Canceled;
            if (o.PaymentStatus == PaymentStatus.Pending)
                o.PaymentStatus = PaymentStatus.Canceled;

            o.StatusHistory.Add(new OrderStatusHistory
            {
                FromStatus = from,
                ToStatus = o.Status,
                PaymentStatusSnapshot = o.PaymentStatus,
                Note = "Canceled by customer",
                ChangedByUserId = userId
            });

            // Release any active stock reservation
            var reservation = await _db.StockReservations
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.OrderId == o.Id && r.Status == StockReservationStatus.Active);

            if (reservation != null)
            {
                foreach (var ri in reservation.Items)
                {
                    var stock = await _db.StockItems
                        .FirstOrDefaultAsync(s => s.ArticleId == ri.ArticleId && s.ArticleVariantId == ri.ArticleVariantId);
                    if (stock != null)
                    {
                        stock.Reserved -= ri.Quantity;
                        _db.StockMovements.Add(new StockMovement
                        {
                            ArticleId = ri.ArticleId,
                            ArticleVariantId = ri.ArticleVariantId,
                            Type = StockMovementType.Release,
                            Quantity = ri.Quantity,
                            RefType = "Order",
                            RefId = o.OrderNumber,
                            Note = "Canceled by customer",
                            ActorUserId = userId
                        });
                    }
                }
                reservation.Status = StockReservationStatus.Released;
                reservation.Reason = "Canceled by customer";
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return ExceptionToProblemX("CancelMyOrder", ex);
        }
    }

    // =====================================================
    // ADMIN: GET /api/orders (PAGED + FILTERS)
    // =====================================================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderAdminListDto>>> GetAllAdmin([FromQuery] OrderAdminQuery q)
    {
        try
        {
            q.Page = Math.Max(1, q.Page);
            q.PageSize = Math.Clamp(q.PageSize, 1, 100);

            var query = _db.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.Items)
                .AsQueryable();

            if (q.Status.HasValue) query = query.Where(o => o.Status == q.Status.Value);
            if (q.PaymentStatus.HasValue) query = query.Where(o => o.PaymentStatus == q.PaymentStatus.Value);
            if (q.PaymentMethod.HasValue) query = query.Where(o => o.PaymentMethod == q.PaymentMethod.Value);

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = q.Search.Trim();
                query = query.Where(o =>
                    o.OrderNumber.Contains(s) ||
                    (o.User != null && (
                        (o.User.Email != null && o.User.Email.Contains(s)) ||
                        (o.User.FullName != null && o.User.FullName.Contains(s)) ||
                        (o.User.PhoneNumber != null && o.User.PhoneNumber.Contains(s))
                    ))
                );
            }

            if (q.From.HasValue) query = query.Where(o => o.CreatedAt >= q.From.Value);
            if (q.To.HasValue) query = query.Where(o => o.CreatedAt <= q.To.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .Select(o => new OrderAdminListDto(
                    o.Id,
                    o.OrderNumber,
                    o.Status,
                    o.PaymentMethod,
                    o.PaymentStatus,
                    o.Total,
                    o.Currency,
                    o.CreatedAt,
                    o.Items.Count,
                    o.User != null ? o.User.Email : null,
                    o.User != null ? o.User.FullName : null,
                    o.User != null ? o.User.PhoneNumber : null
                ))
                .ToListAsync();

            return Ok(new PagedResult<OrderAdminListDto>(items, q.Page, q.PageSize, total));
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<PagedResult<OrderAdminListDto>>("GetAllOrdersAdmin", ex);
        }
    }

    // =====================================================
    // ADMIN: GET /api/orders/{id} (Details)
    // =====================================================
    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDetailsDto>> GetByIdAdmin(int id)
    {
        try
        {
            var o = await _db.Orders
                .AsNoTracking()
                .Include(x => x.Items)
                .Include(x => x.Payments.OrderByDescending(p => p.CreatedAt))
                .Include(x => x.StatusHistory.OrderBy(h => h.CreatedAt))
                .FirstOrDefaultAsync(x => x.Id == id);

            if (o == null)
                return ProblemT<OrderDetailsDto>(404, "Introuvable", "Commande introuvable.");

            return Ok(ToDetailsDto(o));
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<OrderDetailsDto>("GetOrderByIdAdmin", ex);
        }
    }

    // =====================================================
    // ADMIN: PUT /api/orders/{id}/status
    // =====================================================
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> AdminUpdateStatus(int id, [FromBody] AdminUpdateOrderStatusDto dto)
    {
        try
        {
            if (dto == null)
                return ProblemX(400, "Requête invalide", "Payload invalide.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var adminId = GetUserId();
            if (string.IsNullOrWhiteSpace(adminId))
                return ProblemX(401, "Non authentifié", "Utilisateur non authentifié.");

            var o = await _db.Orders
                .Include(x => x.StatusHistory)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (o == null)
                return ProblemX(404, "Introuvable", "Commande introuvable.");

            if (!IsValidTransition(o.Status, dto.Status))
                return ProblemX(422, "Transition invalide",
                    $"Impossible de passer de '{o.Status}' à '{dto.Status}'.");

            var from = o.Status;
            o.Status = dto.Status;

            if (!string.IsNullOrWhiteSpace(dto.AdminNote))
                o.AdminNote = dto.AdminNote.Trim();

            o.StatusHistory.Add(new OrderStatusHistory
            {
                FromStatus = from,
                ToStatus = o.Status,
                PaymentStatusSnapshot = o.PaymentStatus,
                Note = dto.Note,
                ChangedByUserId = adminId
            });

            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return ExceptionToProblemX("AdminUpdateOrderStatus", ex);
        }
    }

    // =====================================================
    // ADMIN: PUT /api/orders/{id}/fees
    // =====================================================
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/fees")]
    public async Task<IActionResult> AdminUpdateFees(int id, [FromBody] AdminUpdateFeesDto dto)
    {
        try
        {
            if (dto == null)
                return ProblemX(400, "Requête invalide", "Payload invalide.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var o = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
            if (o == null)
                return ProblemX(404, "Introuvable", "Commande introuvable.");

            if (dto.DiscountTotal > o.Subtotal)
                return ProblemX(422, "Remise invalide", "La remise ne peut pas dépasser le sous-total.");

            o.DeliveryFee = dto.DeliveryFee;
            o.DiscountTotal = dto.DiscountTotal;
            o.Total = o.Subtotal - o.DiscountTotal + o.DeliveryFee;

            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return ExceptionToProblemX("AdminUpdateFees", ex);
        }
    }

    private static OrderDetailsDto ToDetailsDto(Order o)
    {
        var items = o.Items
            .Where(i => !i.IsDeleted)
            .Select(i => new OrderItemDto(
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

        var addr = new OrderShippingAddressDto(
            o.ShippingAddress.FullName,
            o.ShippingAddress.Phone,
            o.ShippingAddress.Line1,
            o.ShippingAddress.Line2,
            o.ShippingAddress.City,
            o.ShippingAddress.PostalCode,
            o.ShippingAddress.Governorate,
            o.ShippingAddress.CountryCode
        );

        var history = o.StatusHistory
            .Where(h => !h.IsDeleted)
            .OrderBy(h => h.CreatedAt)
            .Select(h => new OrderStatusHistoryDto(
                h.Id,
                h.FromStatus,
                h.ToStatus,
                h.PaymentStatusSnapshot,
                h.Note,
                h.CreatedAt
            ))
            .ToList();

        var payments = o.Payments
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new OrderPaymentDto(
                p.Id,
                p.Provider,
                p.Status,
                p.Amount,
                p.Currency,
                p.PaidAt,
                p.FailureReason,
                p.CreatedAt
            ))
            .ToList();

        return new OrderDetailsDto(
            o.Id,
            o.OrderNumber,
            o.Status,
            o.PaymentMethod,
            o.PaymentStatus,
            o.PaymentProvider,
            o.Subtotal,
            o.DeliveryFee,
            o.DiscountTotal,
            o.Total,
            o.Currency,
            addr,
            o.CustomerNote,
            o.AdminNote,
            o.CreatedAt,
            o.UpdatedAt,
            items,
            history,
            payments
        );
    }

    private static bool IsValidTransition(OrderStatus from, OrderStatus to)
    {
        // Terminal states cannot transition
        if (from is OrderStatus.Delivered or OrderStatus.Canceled or OrderStatus.Refunded)
            return false;

        return (from, to) switch
        {
            // COD: confirm (Preparing) or cancel
            (OrderStatus.Pending, OrderStatus.Preparing) => true,
            (OrderStatus.Pending, OrderStatus.Canceled)  => true,

            // Online: awaiting payment → cancel only (webhook handles Paid)
            (OrderStatus.AwaitingPayment, OrderStatus.Canceled) => true,

            // After paid → admin starts preparing or issues refund
            (OrderStatus.Paid, OrderStatus.Preparing) => true,
            (OrderStatus.Paid, OrderStatus.Refunded)  => true,

            // Fulfillment pipeline
            (OrderStatus.Preparing, OrderStatus.Ready)          => true,
            (OrderStatus.Preparing, OrderStatus.OutForDelivery) => true,  // direct ship
            (OrderStatus.Preparing, OrderStatus.Canceled)       => true,
            (OrderStatus.Ready, OrderStatus.OutForDelivery)     => true,
            (OrderStatus.Ready, OrderStatus.Canceled)           => true,
            (OrderStatus.OutForDelivery, OrderStatus.Delivered) => true,
            (OrderStatus.OutForDelivery, OrderStatus.Canceled)  => true,

            _ => false
        };
    }

    private static string GenerateOrderNumberSafe()
    {
        var now = DateTime.UtcNow;
        var stamp = now.ToString("yyyyMMdd-HHmmss");
        var rnd = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return $"HLW-{stamp}-{rnd}";
    }

    // =====================================================
    // ADMIN: PATCH /api/orders/{id}/admin-note
    // =====================================================
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/admin-note")]
    public async Task<IActionResult> AdminUpdateNote(int id, [FromBody] AdminUpdateNoteDto dto)
    {
        try
        {
            if (dto == null)
                return ProblemX(400, "Requête invalide", "Payload invalide.");

            var o = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
            if (o == null)
                return ProblemX(404, "Introuvable", "Commande introuvable.");

            o.AdminNote = dto.Note?.Trim();
            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return ExceptionToProblemX("AdminUpdateNote", ex);
        }
    }

    // =====================================================
    // INTERNAL: Stock Management Methods
    // =====================================================
    private async Task ReserveStockForOrderAsync(int orderId, string actorUserId)
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

        var expiresAt = DateTime.UtcNow.AddMinutes(15);

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
}
