import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from './api.config';
import { Article, Category, PaginatedResponse, ArticleImageDto } from '../models/article.models';

@Injectable({ providedIn: 'root' })
export class ArticleService {

    constructor(private http: HttpClient) { }

    getArticles(params?: {
        categoryId?: number;
        search?: string;
        page?: number;
        pageSize?: number;
        active?: boolean;
        featured?: boolean;
        minPrice?: number;
        maxPrice?: number;
        sort?: string;
    }): Observable<PaginatedResponse<Article>> {
        let httpParams = new HttpParams();

        if (params) {
            if (params.categoryId !== undefined && params.categoryId !== null) {
                httpParams = httpParams.set('categoryId', String(params.categoryId));
            }
            if (params.search) httpParams = httpParams.set('search', params.search);
            if (params.page !== undefined && params.page !== null) httpParams = httpParams.set('page', String(params.page));
            if (params.pageSize !== undefined && params.pageSize !== null) httpParams = httpParams.set('pageSize', String(params.pageSize));
            if (params.active !== undefined) httpParams = httpParams.set('active', String(params.active));
            if (params.featured !== undefined) httpParams = httpParams.set('featured', String(params.featured));
            if (params.minPrice !== undefined && params.minPrice !== null) httpParams = httpParams.set('minPrice', String(params.minPrice));
            if (params.maxPrice !== undefined && params.maxPrice !== null) httpParams = httpParams.set('maxPrice', String(params.maxPrice));
            if (params.sort) httpParams = httpParams.set('sort', params.sort);
        }

        return this.http.get<PaginatedResponse<Article>>(`${API_URL}/articles`, { params: httpParams });
    }

    getArticle(id: number): Observable<Article> {
        return this.http.get<Article>(`${API_URL}/articles/${id}`);
    }

    getArticleBySlug(slug: string): Observable<Article> {
        return this.http.get<Article>(`${API_URL}/articles/by-slug/${slug}`);
    }

    createArticle(data: {
        title: string;
        description: string;
        price: number;
        categoryId: number;
        slug?: string;
        oldPrice?: number;
        isActive?: boolean;
        isFeatured?: boolean;
        isMadeToOrder?: boolean;
        stockQuantity?: number;
        images?: File[];
        coverIndex?: number;
        variantsJson?: string;
        shortDescription?: string;
        sku?: string;
    }): Observable<Article> {
        const formData = new FormData();

        // Required fields (PascalCase for backend)
        formData.append('Title', data.title);
        formData.append('Price', data.price.toString());
        formData.append('CategoryId', data.categoryId.toString());

        // Optional fields
        if (data.description !== undefined && data.description !== null) {
            formData.append('Description', data.description);
        }
        if (data.shortDescription) formData.append('ShortDescription', data.shortDescription);
        if (data.slug) formData.append('Slug', data.slug);
        if (data.sku) formData.append('Sku', data.sku);

        if (data.oldPrice !== null && data.oldPrice !== undefined) {
            formData.append('OldPrice', data.oldPrice.toString());
        }

        // Booleans (backend expects bool)
        formData.append('IsActive', (data.isActive !== false).toString());
        formData.append('IsFeatured', (data.isFeatured === true).toString());
        formData.append('IsMadeToOrder', (data.isMadeToOrder === true).toString());

        if (data.stockQuantity !== null && data.stockQuantity !== undefined) {
            formData.append('StockQuantity', data.stockQuantity.toString());
        }

        // Images
        if (data.images && data.images.length > 0) {
            data.images.forEach(file => formData.append('Images', file, file.name));
            if (data.coverIndex !== null && data.coverIndex !== undefined) {
                formData.append('CoverIndex', data.coverIndex.toString());
            }
        }

        // Variants JSON
        if (data.variantsJson) {
            formData.append('VariantsJson', data.variantsJson);
        }

        return this.http.post<Article>(`${API_URL}/articles`, formData);
    }

    updateArticle(id: number, article: Partial<Article>): Observable<void> {
        // Transform to PascalCase for backend DTO binding
        const dto: any = {};
        if (article.title !== undefined) dto.Title = article.title;
        if (article.description !== undefined) dto.Description = article.description;
        if (article.price !== undefined) dto.Price = article.price;
        if (article.oldPrice !== undefined) dto.OldPrice = article.oldPrice;
        if (article.categoryId !== undefined) dto.CategoryId = article.categoryId;
        if (article.isActive !== undefined) dto.IsActive = article.isActive;
        if (article.isFeatured !== undefined) dto.IsFeatured = article.isFeatured;
        if (article.isMadeToOrder !== undefined) dto.IsMadeToOrder = article.isMadeToOrder;
        if (article.slug !== undefined) dto.Slug = article.slug;
        if ((article as any).shortDescription !== undefined) dto.ShortDescription = (article as any).shortDescription;
        if (article.sku !== undefined) dto.Sku = article.sku;
        if ((article as any).stockQuantity !== undefined) dto.StockQuantity = (article as any).stockQuantity;

        return this.http.put<void>(`${API_URL}/articles/${id}`, dto);
    }

    deleteArticle(id: number): Observable<void> {
        return this.http.delete<void>(`${API_URL}/articles/${id}`);
    }

    /**
     * Update active status (toggle)
     * Backend requires PUT for updates, so we send { IsActive: newStatus } (PascalCase)
     */
    toggleArticle(id: number, newStatus: boolean): Observable<void> {
        return this.http.put<void>(`${API_URL}/articles/${id}`, { IsActive: newStatus });
    }

    /** GET /api/articles/{id}/images  => returns ArticleImageDto[] */
    getArticleImages(id: number): Observable<ArticleImageDto[]> {
        const timestamp = Date.now();
        return this.http.get<ArticleImageDto[]>(
            `${API_URL}/articles/${id}/images?t=${timestamp}`
        );
    }

    /** Returns the cover image by filtering from the images list */
    getArticleCoverMeta(id: number): Observable<ArticleImageDto | undefined> {
        return new Observable(observer => {
            this.getArticleImages(id).subscribe({
                next: (images) => {
                    observer.next(images.find(img => img.isCover));
                    observer.complete();
                },
                error: (err) => observer.error(err)
            });
        });
    }


    addArticleImages(articleId: number, files: File[]): Observable<any> {
        const fd = new FormData();
        files.forEach(f => fd.append('images', f, f.name));
        return this.http.post(`${API_URL}/articles/${articleId}/images`, fd);
    }

    setArticleCover(articleId: number, imageId: number): Observable<void> {
        return this.http.patch<void>(`${API_URL}/articles/${articleId}/images/${imageId}/cover`, {});
    }

    deleteArticleImage(articleId: number, imageId: number): Observable<void> {
        return this.http.delete<void>(`${API_URL}/articles/${articleId}/images/${imageId}`);
    }

    // =====================================================
    // Categories
    // =====================================================

    /** Public: Get all active categories */
    getCategories(): Observable<Category[]> {
        return this.http.get<Category[]>(`${API_URL}/categories`);
    }

    /** Public: Get category by slug */
    getCategoryBySlug(slug: string): Observable<Category> {
        return this.http.get<Category>(`${API_URL}/categories/by-slug/${slug}`);
    }

    /** Admin: Get all categories with stats (articlesCount) */
    getAdminCategories(): Observable<Category[]> {
        return this.http.get<Category[]>(`${API_URL}/categories/admin`);
    }

    /** Admin: Get category by ID */
    getCategoryByIdAdmin(id: number): Observable<Category> {
        return this.http.get<Category>(`${API_URL}/categories/admin/${id}`);
    }

    /** Admin: Create category (multipart/form-data with optional image) */
    createCategory(data: { name: string; slug?: string; isActive?: boolean; sortOrder?: number; image?: File }): Observable<Category> {
        const fd = new FormData();
        fd.append('Name', data.name);
        if (data.slug) fd.append('Slug', data.slug);
        fd.append('IsActive', String(data.isActive !== false));
        fd.append('SortOrder', String(data.sortOrder || 0));
        if (data.image) fd.append('Image', data.image, data.image.name);
        return this.http.post<Category>(`${API_URL}/categories`, fd);
    }

    /** Admin: Update category (multipart/form-data with optional image) */
    updateCategory(id: number, data: { name: string; slug?: string; isActive?: boolean; sortOrder?: number; image?: File; removeImage?: boolean }): Observable<Category> {
        const fd = new FormData();
        fd.append('Name', data.name);
        if (data.slug) fd.append('Slug', data.slug);
        fd.append('IsActive', String(data.isActive !== false));
        fd.append('SortOrder', String(data.sortOrder || 0));
        if (data.removeImage) fd.append('RemoveImage', 'true');
        if (data.image) fd.append('Image', data.image, data.image.name);
        return this.http.put<Category>(`${API_URL}/categories/${id}`, fd);
    }

    /** Admin: Patch category (isActive, sortOrder, name, slug) */
    patchCategory(id: number, patch: { isActive?: boolean; sortOrder?: number; name?: string; slug?: string }): Observable<void> {
        return this.http.patch<void>(`${API_URL}/categories/${id}`, patch);
    }

    /** 
     * Admin: Toggle category (Shortcut for patch) 
     */
    toggleCategory(id: number, newStatus: boolean): Observable<void> {
        return this.patchCategory(id, { isActive: newStatus });
    }

    /** Admin: Delete category */
    deleteCategory(id: number): Observable<void> {
        return this.http.delete<void>(`${API_URL}/categories/${id}`);
    }

    /** Admin: Reorder categories */
    reorderCategories(ids: number[]): Observable<void> {
        return this.http.put<void>(`${API_URL}/categories/reorder`, ids);
    }

}
