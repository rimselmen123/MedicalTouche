import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { API_URL } from './api.config';
import { Cart, AddToCartRequest, UpdateCartItemRequest } from '../models/cart.models';

@Injectable({ providedIn: 'root' })
export class CartService {
  private cartItemCountSubject = new BehaviorSubject<number>(0);
  cartItemCount$ = this.cartItemCountSubject.asObservable();

  constructor(private http: HttpClient) {
    this.getCart().subscribe({ error: () => {} });
  }

  getCart(): Observable<Cart> {
    return this.http.get<Cart>(`${API_URL}/cart`).pipe(
      tap(cart => this.updateCount(cart))
    );
  }

  addToCart(request: AddToCartRequest): Observable<Cart> {
    return this.http.post<Cart>(`${API_URL}/cart/items`, request).pipe(
      tap(cart => this.updateCount(cart))
    );
  }

  updateItem(itemId: number, request: UpdateCartItemRequest): Observable<Cart> {
    return this.http.put<Cart>(`${API_URL}/cart/items/${itemId}`, request).pipe(
      tap(cart => this.updateCount(cart))
    );
  }

  removeItem(itemId: number): Observable<Cart> {
    return this.http.delete<Cart>(`${API_URL}/cart/items/${itemId}`).pipe(
      tap(cart => this.updateCount(cart))
    );
  }

  clearCart(): Observable<Cart> {
    return this.http.post<Cart>(`${API_URL}/cart/clear`, {}).pipe(
      tap(cart => this.updateCount(cart))
    );
  }

  private updateCount(cart: Cart) {
    const count = cart?.items ? cart.items.length : 0;
    this.cartItemCountSubject.next(count);
  }
}
