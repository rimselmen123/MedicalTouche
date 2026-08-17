export interface CartItem {
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

export interface Cart {
  id: number;
  currency: string;
  subtotal: number;
  discountTotal: number;
  deliveryFee: number;
  total: number;
  items: CartItem[];
  updatedAt: string;
}

export interface AddToCartRequest {
  articleId: number;
  variantId: number | null;
  quantity: number;
}

export interface UpdateCartItemRequest {
  quantity: number;
}
