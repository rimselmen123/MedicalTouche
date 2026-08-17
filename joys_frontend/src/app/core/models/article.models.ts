export interface Category {
    id: number;
    name: string;
    slug: string;
    imageUrl?: string;
    isActive: boolean;
    sortOrder: number;
    articlesCount?: number;
}

export interface ArticleImage {
    id: number;
    url: string;
    isCover: boolean;
    sortOrder: number;
    alt?: string;
}

export interface ArticleImageDto {
    id: number;
    url: string;
    isCover: boolean;
    sortOrder: number;
    alt?: string;
}

export interface ArticleVariant {
    id: number;
    name: string;
    price: number;
    isDefault: boolean;
    stockQuantity?: number;
    sku?: string;
}

export interface Article {
    id: number;
    title: string;
    slug: string;
    shortDescription?: string;
    description?: string;
    price: number;
    oldPrice: number | null;
    sku?: string;
    categoryId?: number;
    categoryName?: string;
    isActive: boolean;
    isFeatured: boolean;
    isMadeToOrder: boolean;
    stockQuantity?: number;
    images: ArticleImage[];
    variants: ArticleVariant[];
    createdAt: string;
    coverUrl?: string;
}

export interface PaginatedResponse<T> {
    items: T[];
    page: number;
    pageSize: number;
    totalCount: number;
}
