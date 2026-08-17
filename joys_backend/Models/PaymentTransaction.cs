using System.ComponentModel.DataAnnotations;
using Hlouwa.Enums;

namespace Hlouwa.Models
{
    // Each online attempt produces a transaction record
    public class PaymentTransaction : AuditableEntity
    {
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public PaymentProvider Provider { get; set; }

        public PaymentTxnStatus Status { get; set; } = PaymentTxnStatus.Initiated;

        public decimal Amount { get; set; }

        [Required, MaxLength(3)]
        public string Currency { get; set; } = "TND";

        // Provider references (tokens/ids)
        [MaxLength(120)]
        public string? ProviderPaymentId { get; set; } // e.g. paymee token, konnect payment id

        [MaxLength(120)]
        public string? ProviderTransactionId { get; set; }

        [MaxLength(600)]
        public string? CheckoutUrl { get; set; } // redirect user here

        [MaxLength(600)]
        public string? CallbackUrl { get; set; } // where provider returns user

        public DateTime? PaidAt { get; set; }

        [MaxLength(400)]
        public string? FailureReason { get; set; }

        // Raw JSON payload logs (webhooks / create payment response)
        public string? ProviderRequestJson { get; set; }
        public string? ProviderResponseJson { get; set; }
        public string? ProviderWebhookJson { get; set; }
    }
}
