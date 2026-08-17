import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from './api.config';
import {
  Order,
  CreateOrderRequest,
  UpdateOrderStatusRequest,
  UpdateOrderFeesRequest,
  Address,
  CreateAddressRequest
} from '../models/order.models';
import { PaginatedResponse } from '../models/article.models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  constructor(private http: HttpClient) { }

  // ── Client Orders ─────────────────────────────────────

  createOrder(request: CreateOrderRequest): Observable<Order> {
    return this.http.post<Order>(`${API_URL}/orders`, request);
  }

  getMyOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(`${API_URL}/orders/me`);
  }

  getMyOrder(id: number): Observable<Order> {
    return this.http.get<Order>(`${API_URL}/orders/me/${id}`);
  }

  cancelOrder(id: number): Observable<void> {
    return this.http.patch<void>(`${API_URL}/orders/me/${id}/cancel`, {});
  }

  // ── Admin Orders ──────────────────────────────────────

  getOrders(params?: {
    status?: number;
    paymentStatus?: number;
    search?: string;
    from?: string;
    to?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResponse<Order>> {
    let httpParams = new HttpParams();
    if (params) {
      if (params.status !== undefined && params.status !== null) httpParams = httpParams.set('status', String(params.status));
      if (params.paymentStatus !== undefined && params.paymentStatus !== null) httpParams = httpParams.set('paymentStatus', String(params.paymentStatus));
      if (params.search) httpParams = httpParams.set('search', params.search);
      if (params.from) httpParams = httpParams.set('from', params.from);
      if (params.to) httpParams = httpParams.set('to', params.to);
      if (params.page !== undefined) httpParams = httpParams.set('page', String(params.page));
      if (params.pageSize !== undefined) httpParams = httpParams.set('pageSize', String(params.pageSize));
    }
    return this.http.get<PaginatedResponse<Order>>(`${API_URL}/orders`, { params: httpParams });
  }

  getOrder(id: number): Observable<Order> {
    return this.http.get<Order>(`${API_URL}/orders/${id}`);
  }

  updateOrderStatus(id: number, request: UpdateOrderStatusRequest): Observable<void> {
    return this.http.put<void>(`${API_URL}/orders/${id}/status`, request);
  }

  updateDeliveryFees(id: number, deliveryFee: number, discountTotal: number): Observable<void> {
    const body: UpdateOrderFeesRequest = { deliveryFee, discountTotal };
    return this.http.put<void>(`${API_URL}/orders/${id}/fees`, body);
  }

  updateAdminNote(id: number, note: string): Observable<void> {
    return this.http.patch<void>(`${API_URL}/orders/${id}/admin-note`, { note });
  }

  // ── Addresses ─────────────────────────────────────────

  getAddresses(): Observable<Address[]> {
    return this.http.get<Address[]>(`${API_URL}/addresses`);
  }

  createAddress(request: CreateAddressRequest): Observable<Address> {
    return this.http.post<Address>(`${API_URL}/addresses`, request);
  }

  getAddress(id: number): Observable<Address> {
    return this.http.get<Address>(`${API_URL}/addresses/${id}`);
  }

  updateAddress(id: number, request: CreateAddressRequest): Observable<void> {
    return this.http.put<void>(`${API_URL}/addresses/${id}`, request);
  }

  setDefaultAddress(id: number): Observable<void> {
    return this.http.patch<void>(`${API_URL}/addresses/${id}/default`, {});
  }

  deleteAddress(id: number): Observable<void> {
    return this.http.delete<void>(`${API_URL}/addresses/${id}`);
  }
}
