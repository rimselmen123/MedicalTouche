export type StockMovementType = 'In' | 'Out' | 'Reserve' | 'Release' | 'Adjust';
export type StockReservationStatus = 'Active' | 'Committed' | 'Released' | 'Expired';

export interface StockItem {
    articleId: number;
    articleVariantId: number | null;
    onHand: number;
    reserved: number;
    available: number;
}

export interface StockMovement {
    id: number;
    articleId: number;
    articleVariantId: number | null;
    type: StockMovementType;
    quantity: number;
    refType: string;
    refId: string;
    note?: string;
    actorUserId: string;
    createdAt: string;
}

export interface StockReservation {
    id: number;
    orderId: number;
    orderNumber: string;
    status: StockReservationStatus;
    expiresAt: string;
    reason: string;
    createdAt: string;
    items: {
        articleId: number;
        articleVariantId: number | null;
        name: string;
        variantName?: string;
        quantity: number;
    }[];
}

export interface AdjustStockRequest {
    articleId: number;
    variantId: number | null;
    delta: number;
    note: string;
}
