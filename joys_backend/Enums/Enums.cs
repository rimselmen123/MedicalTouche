namespace Hlouwa.Enums
{
    public enum OrderStatus
    {
        Pending = 0,
        AwaitingPayment = 1,
        Paid = 2,
        Preparing = 3,
        Ready = 4,
        OutForDelivery = 5,
        Delivered = 6,
        Canceled = 7,
        Refunded = 8
    }

    public enum PaymentMethod
    {
        CashOnDelivery = 0,
        Online = 1
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Authorized = 1,
        Paid = 2,
        Failed = 3,
        Canceled = 4,
        Refunded = 5
    }

    public enum PaymentProvider
    {
        None = 0,
        Paymee = 10,
        Konnect = 20,
        Flouci = 30,
        ClicToPaySMT = 40
    }

    public enum PaymentTxnStatus
    {
        Initiated = 0,
        Redirected = 1,
        Succeeded = 2,
        Failed = 3,
        Canceled = 4,
        Refunded = 5
    }

    public enum ReclamationStatus
    {
        New = 0,
        InProgress = 1,
        Resolved = 2
    }

    public enum StockMovementType
    {
        In = 0,        // achat / réception stock
        Out = 1,       // sortie réelle (vente, perte)
        Reserve = 2,   // réservation (online pending)
        Release = 3,   // libération (payment failed/timeout)
        Adjust = 4     // correction manuelle
    }

    public enum StockReservationStatus
    {
        Active = 0,
        Committed = 1,
        Released = 2,
        Expired = 3
    }
}
