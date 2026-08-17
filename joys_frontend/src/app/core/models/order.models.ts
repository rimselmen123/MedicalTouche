// ── Type aliases (kept for compatibility) ──
export type PaymentMethod = 'CashOnDelivery' | 'Online';
export type PaymentProvider = 'None' | 'Paymee' | 'Konnect' | 'Flouci' | 'ClicToPaySMT';
export type OrderStatus =
    | 'Pending'
    | 'AwaitingPayment'
    | 'Paid'
    | 'Preparing'
    | 'Ready'
    | 'OutForDelivery'
    | 'Delivered'
    | 'Canceled'
    | 'Refunded';
export type PaymentStatus = 'Pending' | 'Authorized' | 'Paid' | 'Failed' | 'Canceled' | 'Refunded';

// ── Integer → String maps (backend sends integers) ──

export const ORDER_STATUS_MAP: { [key: number]: OrderStatus } = {
    0: 'Pending',
    1: 'AwaitingPayment',
    2: 'Paid',
    3: 'Preparing',
    4: 'Ready',
    5: 'OutForDelivery',
    6: 'Delivered',
    7: 'Canceled',
    8: 'Refunded'
};

export const ORDER_STATUS_LABELS: { [key: string]: string } = {
    'Pending': 'En attente',
    'AwaitingPayment': 'Attente paiement',
    'Paid': 'Payée',
    'Preparing': 'En préparation',
    'Ready': 'Prête',
    'OutForDelivery': 'En livraison',
    'Delivered': 'Livrée',
    'Canceled': 'Annulée',
    'Refunded': 'Remboursée'
};

export const PAYMENT_STATUS_MAP: { [key: number]: PaymentStatus } = {
    0: 'Pending',
    1: 'Authorized',
    2: 'Paid',
    3: 'Failed',
    4: 'Canceled',
    5: 'Refunded'
};

export const PAYMENT_STATUS_LABELS: { [key: string]: string } = {
    'Pending': 'En attente',
    'Authorized': 'Autorisé',
    'Paid': 'Payé',
    'Failed': 'Échoué',
    'Canceled': 'Annulé',
    'Refunded': 'Remboursé'
};

export const PAYMENT_METHOD_MAP: { [key: number]: string } = {
    0: 'Paiement à la livraison',
    1: 'Paiement en ligne'
};

// ── Reverse: string → integer (for API calls) ──

export const ORDER_STATUS_INT: { [key: string]: number } = {
    'Pending': 0,
    'AwaitingPayment': 1,
    'Paid': 2,
    'Preparing': 3,
    'Ready': 4,
    'OutForDelivery': 5,
    'Delivered': 6,
    'Canceled': 7,
    'Refunded': 8
};

// ── Interfaces ──

export interface Address {
    id: number;
    label?: string;
    fullName: string;
    phone: string;
    line1: string;
    line2?: string;
    city: string;
    postalCode?: string;
    governorate?: string;
    countryCode: string;
    isDefault: boolean;
    createdAt?: string;
}

export interface OrderItem {
    id: number;
    articleId: number;
    variantId: number | null;
    name: string;
    variantName: string | null;
    sku: string | null;
    imageUrl: string | null;
    unitPrice: number;
    quantity: number;
    lineTotal: number;
}

export interface OrderStatusHistoryItem {
    id: number;
    fromStatus: number;
    toStatus: number;
    paymentStatusSnapshot?: number;
    note?: string;
    createdAt: string;
}

export interface OrderPaymentItem {
    id: number;
    provider: number;
    status: number;
    amount: number;
    currency: string;
    paidAt?: string;
    failureReason?: string;
    createdAt: string;
}

export interface Order {
    id: number;
    orderNumber: string;
    status: number;
    paymentMethod: number;
    paymentStatus: number;
    provider?: number;
    subtotal: number;
    deliveryFee: number;
    discountTotal: number;
    total: number;
    currency: string;
    shippingAddress: ShippingAddressSnapshot;
    customerNote?: string;
    adminNote?: string;
    createdAt: string;
    updatedAt?: string;
    items: OrderItem[];
    statusHistory?: OrderStatusHistoryItem[];
    payments?: OrderPaymentItem[];
    // Admin list-only fields
    customerEmail?: string;
    customerFullName?: string;
    customerPhone?: string;
    itemsCount?: number;
}

export interface ShippingAddressSnapshot {
    fullName?: string;
    phone?: string;
    line1?: string;
    line2?: string;
    city?: string;
    postalCode?: string;
    governorate?: string;
    countryCode?: string;
}

export interface CreateOrderRequest {
    paymentMethod: number;
    provider?: number | null;
    customerNote?: string;
    shippingAddress: ShippingAddressRequest;
    requestedDeliveryDate?: string;
    deliveryTimeSlot?: string;
    items: OrderItemRequest[];
}

export interface ShippingAddressRequest {
    fullName: string;
    phone: string;
    line1: string;
    line2?: string;
    city: string;
    postalCode?: string;
    governorate?: string;
}

export interface OrderItemRequest {
    articleId: number;
    variantId?: number | null;
    quantity: number;
}

export interface CreateAddressRequest {
    label?: string;
    fullName: string;
    phone: string;
    line1: string;
    line2?: string;
    city: string;
    postalCode?: string;
    governorate?: string;
    countryCode?: string;
    isDefault?: boolean;
}

export interface UpdateOrderStatusRequest {
    status: number;
    note?: string;
    adminNote?: string;
}

export interface UpdateOrderFeesRequest {
    deliveryFee: number;
    discountTotal: number;
}
