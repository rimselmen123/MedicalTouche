export interface PaymentTransaction {
    id: number;
    orderId: number;
    provider: string;
    status: 'Pending' | 'Succeeded' | 'Failed' | 'Canceled' | 'Expired';
    amount: number;
    currency: string;
    providerPaymentId?: string;
    providerTransactionId?: string;
    checkoutUrl?: string;
    paidAt?: string;
    createdAt: string;
}

export interface InitPaymentRequest {
    provider?: string;
}

export interface InitPaymentResponse {
    orderId: number;
    orderNumber: string;
    amount: number;
    currency: string;
    provider: string;
    checkoutUrl: string;
}
