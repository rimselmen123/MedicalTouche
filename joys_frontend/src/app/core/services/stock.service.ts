import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from './api.config';
import { StockItem, AdjustStockRequest, StockMovement, StockReservation, StockReservationStatus } from '../models/stock.models';

@Injectable({ providedIn: 'root' })
export class StockService {
    constructor(private http: HttpClient) { }

    getStockItems(): Observable<StockItem[]> {
        return this.http.get<StockItem[]>(`${API_URL}/stock/items`);
    }

    adjustStock(request: AdjustStockRequest): Observable<void> {
        return this.http.post<void>(`${API_URL}/stock/adjust`, request);
    }

    getMovements(params?: {
        articleId?: number;
        variantId?: number;
        limit?: number;
    }): Observable<StockMovement[]> {
        let httpParams = new HttpParams();
        if (params) {
            if (params.articleId) httpParams = httpParams.set('articleId', params.articleId);
            if (params.variantId) httpParams = httpParams.set('variantId', params.variantId);
            if (params.limit) httpParams = httpParams.set('limit', params.limit);
        }
        return this.http.get<StockMovement[]>(`${API_URL}/stock/movements`, { params: httpParams });
    }

    getReservations(status?: StockReservationStatus): Observable<StockReservation[]> {
        let httpParams = new HttpParams();
        if (status) {
            httpParams = httpParams.set('status', status);
        }
        return this.http.get<StockReservation[]>(`${API_URL}/stock/reservations`, { params: httpParams });
    }

    expireReservations(): Observable<{ expired: number }> {
        return this.http.post<{ expired: number }>(`${API_URL}/stock/expire-reservations`, {});
    }
}
