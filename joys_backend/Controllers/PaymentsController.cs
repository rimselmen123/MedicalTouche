using Hlouwa.Models;
using System.Security.Claims;
using System.Text.Json;
using Hlouwa.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hlouwa.Controllers;

// =====================================================
// Provider Abstractions (plug Konnect/Paymee/Flouci later)
// =====================================================
public record PaymentInitResult(string CheckoutUrl, string ProviderPaymentId, string? ProviderTransactionId, string RawResponseJson);

public record PaymentWebhookResult(
    string ProviderPaymentId,
    bool IsPaid,
    bool IsFailed,
    bool IsCanceled,
    string RawWebhookJson,
    string? ProviderTransactionId,
    string? FailureReason
);

public record PaymentInitRequestDto(string? Provider); // optional override provider per request
public record PaymentInitResponseDto(int OrderId, string OrderNumber, decimal Amount, string Currency, string Provider, string CheckoutUrl);

public interface IPaymentProviderClient
{
    PaymentProvider Provider { get; }

    Task<PaymentInitResult> CreatePaymentAsync(Order order, string returnUrl, string webhookUrl, CancellationToken ct);

    /// Parse + verify webhook signature (provider-specific)
    Task<PaymentWebhookResult> ParseAndVerifyWebhookAsync(HttpRequest request, CancellationToken ct);

    /// Parse callback query (optional, provider-specific)
    PaymentWebhookResult ParseCallback(IQueryCollection query);
}

public class DummyPaymentProviderClient : IPaymentProviderClient
{
    public PaymentProvider Provider => PaymentProvider.Konnect; // change to match your default

    public Task<PaymentInitResult> CreatePaymentAsync(Order order, string returnUrl, string webhookUrl, CancellationToken ct)
    {
        // Simulate checkout URL
        var pid = $"dummy_{Guid.NewGuid():N}";
        var checkout = $"{returnUrl}?dummyPaymentId={pid}&status=success";

        var raw = JsonSerializer.Serialize(new
        {
            checkoutUrl = checkout,
            paymentId = pid,
            amount = order.Total,
            currency = order.Currency,
            webhook = webhookUrl
        });

        return Task.FromResult(new PaymentInitResult(checkout, pid, null, raw));
    }

    public async Task<PaymentWebhookResult> ParseAndVerifyWebhookAsync(HttpRequest request, CancellationToken ct)
    {
        using var reader = new StreamReader(request.Body);
        var json = await reader.ReadToEndAsync(ct);

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);

        var pid = doc.RootElement.TryGetProperty("paymentId", out var p) ? p.GetString() : null;
        var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;

        return new PaymentWebhookResult(
            ProviderPaymentId: pid ?? "",
            IsPaid: string.Equals(status, "success", StringComparison.OrdinalIgnoreCase),
            IsFailed: string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase),
            IsCanceled: string.Equals(status, "canceled", StringComparison.OrdinalIgnoreCase),
            RawWebhookJson: json,
            ProviderTransactionId: null,
            FailureReason: string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase) ? "Payment failed" : null
        );
    }

    public PaymentWebhookResult ParseCallback(IQueryCollection query)
    {
        var pid = query["dummyPaymentId"].ToString();
        var status = query["status"].ToString();

        return new PaymentWebhookResult(
            ProviderPaymentId: pid,
            IsPaid: string.Equals(status, "success", StringComparison.OrdinalIgnoreCase),
            IsFailed: string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase),
            IsCanceled: string.Equals(status, "canceled", StringComparison.OrdinalIgnoreCase),
            RawWebhookJson: JsonSerializer.Serialize(new { pid, status }),
            ProviderTransactionId: null,
            FailureReason: string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase) ? "Payment failed" : null
        );
    }
}

// =====================================================
// PaymentsController (coherent responses + stable)
// =====================================================
[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEnumerable<IPaymentProviderClient> _providers;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        AppDbContext db,
        IEnumerable<IPaymentProviderClient> providers,
        IConfiguration config,
        IWebHostEnvironment env,
        ILogger<PaymentsController> logger)
    {
        _db = db;
        _providers = providers;
        _config = config;
        _env = env;
        _logger = logger;
    }

    // -----------------------------
    // Response helpers (same spirit as OrdersController)
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

    private IPaymentProviderClient? TryGetProvider(PaymentProvider p)
        => _providers.FirstOrDefault(x => x.Provider == p);

    private static PaymentProvider ParseProvider(string? s, PaymentProvider fallback)
    {
        s = (s ?? "").Trim().ToLowerInvariant();
        return s switch
        {
            "paymee" => PaymentProvider.Paymee,
            "konnect" => PaymentProvider.Konnect,
            "flouci" => PaymentProvider.Flouci,
            "clictopaysmt" => PaymentProvider.ClicToPaySMT,
            _ => fallback
        };
    }

    private string GetPublicBaseUrl()
        => _config["App:PublicBaseUrl"]?.TrimEnd('/')
           ?? $"{Request.Scheme}://{Request.Host}";

    // -----------------------------------------------------
    // POST /api/payments/init/{orderId}
    // Create a payment transaction and return checkoutUrl
    // -----------------------------------------------------
    [Authorize]
    [HttpPost("init/{orderId:int}")]
    public async Task<ActionResult<PaymentInitResponseDto>> Init(
        int orderId,
        [FromBody] PaymentInitRequestDto? dto,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return ProblemT<PaymentInitResponseDto>(401, "Non authentifié", "Utilisateur non authentifié.");

            var order = await _db.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct);

            if (order == null)
                return ProblemT<PaymentInitResponseDto>(404, "Introuvable", "Commande introuvable.");

            if (order.PaymentMethod != PaymentMethod.Online)
                return ProblemT<PaymentInitResponseDto>(400, "Requête invalide", "Cette commande n'est pas en paiement en ligne.");

            if (order.Status == OrderStatus.Canceled)
                return ProblemT<PaymentInitResponseDto>(400, "Requête invalide", "Commande annulée.");

            if (order.PaymentStatus == PaymentStatus.Paid)
                return ProblemT<PaymentInitResponseDto>(409, "Conflit", "Commande déjà payée.");

            // provider can be overridden per request
            var providerEnum = ParseProvider(dto?.Provider, order.PaymentProvider);

            if (providerEnum == PaymentProvider.None)
                return ProblemT<PaymentInitResponseDto>(400, "Requête invalide", "Provider invalide ou manquant.");

            // Idempotency: return existing redirected transaction if still active
            var existingTxn = order.Payments
                .Where(p => !p.IsDeleted && p.Provider == providerEnum && p.Status == PaymentTxnStatus.Redirected && p.CheckoutUrl != null)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            if (existingTxn != null)
            {
                return Ok(new PaymentInitResponseDto(
                    order.Id,
                    order.OrderNumber,
                    order.Total,
                    order.Currency,
                    providerEnum.ToString(),
                    existingTxn.CheckoutUrl!
                ));
            }

            var client = TryGetProvider(providerEnum);
            if (client == null)
                return ProblemT<PaymentInitResponseDto>(400, "Requête invalide", $"Provider non configuré: {providerEnum}");

            // Persist chosen provider
            order.PaymentProvider = providerEnum;

            // Return/callback URLs (front)
            var publicBaseUrl = GetPublicBaseUrl();
            var returnUrl = $"{publicBaseUrl}/payment/return/{providerEnum.ToString().ToLowerInvariant()}";
            var webhookUrl = $"{publicBaseUrl}/api/payments/webhook/{providerEnum.ToString().ToLowerInvariant()}";

            // Create transaction row first (Initiated)
            var txn = new PaymentTransaction
            {
                OrderId = order.Id,
                Provider = providerEnum,
                Status = PaymentTxnStatus.Initiated,
                Amount = order.Total,
                Currency = order.Currency,
                CallbackUrl = returnUrl
            };

            _db.PaymentTransactions.Add(txn);
            await _db.SaveChangesAsync(ct);

            PaymentInitResult init;

            try
            {
                init = await client.CreatePaymentAsync(order, returnUrl, webhookUrl, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider CreatePayment failed. Provider={Provider} OrderId={OrderId}", providerEnum, order.Id);

                txn.Status = PaymentTxnStatus.Failed;
                txn.FailureReason = "Provider init failed";
                txn.ProviderResponseJson = _env.IsDevelopment() ? ex.ToString() : null;
                await _db.SaveChangesAsync(ct);

                return ProblemT<PaymentInitResponseDto>(502, "Erreur paiement", "Impossible d'initialiser le paiement (provider).");
            }

            if (string.IsNullOrWhiteSpace(init.CheckoutUrl) || string.IsNullOrWhiteSpace(init.ProviderPaymentId))
            {
                txn.Status = PaymentTxnStatus.Failed;
                txn.FailureReason = "Invalid provider init response";
                txn.ProviderResponseJson = init.RawResponseJson;
                await _db.SaveChangesAsync(ct);

                return ProblemT<PaymentInitResponseDto>(502, "Erreur paiement", "Réponse provider invalide.");
            }

            txn.ProviderPaymentId = init.ProviderPaymentId;
            txn.ProviderTransactionId = init.ProviderTransactionId;
            txn.CheckoutUrl = init.CheckoutUrl;
            txn.ProviderResponseJson = init.RawResponseJson;
            txn.Status = PaymentTxnStatus.Redirected;

            // Put order in awaiting payment
            order.Status = OrderStatus.AwaitingPayment;
            order.PaymentStatus = PaymentStatus.Pending;

            await _db.SaveChangesAsync(ct);

            return Ok(new PaymentInitResponseDto(
                order.Id,
                order.OrderNumber,
                order.Total,
                order.Currency,
                providerEnum.ToString(),
                init.CheckoutUrl
            ));
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<PaymentInitResponseDto>("InitPayment", ex);
        }
    }

    // -----------------------------------------------------
    // GET /api/payments/callback/{provider}
    // Browser redirect. Webhook remains the truth.
    // -----------------------------------------------------
    [AllowAnonymous]
    [HttpGet("callback/{provider}")]
    public async Task<IActionResult> Callback(string provider, CancellationToken ct)
    {
        try
        {
            var providerEnum = ParseProvider(provider, PaymentProvider.None);
            if (providerEnum == PaymentProvider.None)
                return ProblemX(400, "Requête invalide", "Provider invalide.");

            var client = TryGetProvider(providerEnum);
            if (client == null)
                return ProblemX(400, "Requête invalide", $"Provider non configuré: {providerEnum}");

            var parsed = client.ParseCallback(Request.Query);

            if (string.IsNullOrWhiteSpace(parsed.ProviderPaymentId))
                return ProblemX(400, "Requête invalide", "Callback incomplet.");

            // Update transaction/order (best effort)
            await ApplyPaymentResult(providerEnum, parsed, ct);

            // Redirect to frontend result page
            var publicBaseUrl = GetPublicBaseUrl();
            return Redirect($"{publicBaseUrl}/payment/result?provider={providerEnum}&paymentId={parsed.ProviderPaymentId}");
        }
        catch (Exception ex)
        {
            return ExceptionToProblemX("PaymentCallback", ex);
        }
    }

    // -----------------------------------------------------
    // POST /api/payments/webhook/{provider}
    // Must verify signature in real provider client
    // -----------------------------------------------------
    [AllowAnonymous]
    [HttpPost("webhook/{provider}")]
    public async Task<ActionResult<object>> Webhook(string provider, CancellationToken ct)
    {
        try
        {
            var providerEnum = ParseProvider(provider, PaymentProvider.None);
            if (providerEnum == PaymentProvider.None)
                return ProblemT<object>(400, "Requête invalide", "Provider invalide.");

            var client = TryGetProvider(providerEnum);
            if (client == null)
                return ProblemT<object>(400, "Requête invalide", $"Provider non configuré: {providerEnum}");

            var parsed = await client.ParseAndVerifyWebhookAsync(Request, ct);

            if (string.IsNullOrWhiteSpace(parsed.ProviderPaymentId))
                return ProblemT<object>(400, "Requête invalide", "Webhook incomplet.");

            await ApplyPaymentResult(providerEnum, parsed, ct);

            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return ExceptionToProblemT<object>("PaymentWebhook", ex);
        }
    }

    // -----------------------------------------------------
    // Core update logic (idempotent-ish)
    // -----------------------------------------------------
    private async Task ApplyPaymentResult(PaymentProvider provider, PaymentWebhookResult parsed, CancellationToken ct)
    {
        var txn = await _db.PaymentTransactions
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.Provider == provider && t.ProviderPaymentId == parsed.ProviderPaymentId, ct);

        if (txn == null)
        {
            _logger.LogWarning("Webhook/Callback received but txn not found. Provider={Provider} PaymentId={PaymentId}",
                provider, parsed.ProviderPaymentId);
            return;
        }

        // Save raw payloads
        txn.ProviderWebhookJson = parsed.RawWebhookJson;
        txn.ProviderTransactionId ??= parsed.ProviderTransactionId;

        // Idempotency: if already succeeded, ignore later failures/cancels
        if (txn.Status == PaymentTxnStatus.Succeeded || txn.Order.PaymentStatus == PaymentStatus.Paid)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        if (parsed.IsPaid)
        {
            txn.Status = PaymentTxnStatus.Succeeded;
            txn.PaidAt = DateTime.UtcNow;

            txn.Order.PaymentStatus = PaymentStatus.Paid;
            txn.Order.Status = OrderStatus.Paid;

            // Commit stock reservation
            try
            {
                await CommitStockForOrderAsync(txn.OrderId, "SYSTEM");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock commit failed for order {OrderId}", txn.OrderId);
            }
        }
        else if (parsed.IsCanceled)
        {
            txn.Status = PaymentTxnStatus.Canceled;
            txn.FailureReason = parsed.FailureReason ?? "Canceled";

            txn.Order.PaymentStatus = PaymentStatus.Canceled;

            // Option business-rule:
            // - keep Order as AwaitingPayment to allow retry
            // - OR mark Order as Canceled
            txn.Order.Status = OrderStatus.AwaitingPayment;

            // Release stock reservation
            try
            {
                await ReleaseStockForOrderAsync(txn.OrderId, "SYSTEM", "Payment canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock release failed for order {OrderId}", txn.OrderId);
            }
        }
        else if (parsed.IsFailed)
        {
            txn.Status = PaymentTxnStatus.Failed;
            txn.FailureReason = parsed.FailureReason ?? "Failed";

            txn.Order.PaymentStatus = PaymentStatus.Failed;

            // Keep order awaiting payment to allow retry
            txn.Order.Status = OrderStatus.AwaitingPayment;

            // Release stock reservation
            try
            {
                await ReleaseStockForOrderAsync(txn.OrderId, "SYSTEM", parsed.FailureReason ?? "Payment failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock release failed for order {OrderId}", txn.OrderId);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    // =====================================================
    // INTERNAL: Stock Management Methods
    // =====================================================
    private async Task CommitStockForOrderAsync(int orderId, string actorUserId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var reservation = await _db.StockReservations
            .Include(r => r.Order)
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.OrderId == orderId);

        if (reservation == null)
            return;

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

    private async Task ReleaseStockForOrderAsync(int orderId, string actorUserId, string reason)
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
}
