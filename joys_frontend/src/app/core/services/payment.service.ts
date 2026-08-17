import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from './api.config';
import { InitPaymentRequest, InitPaymentResponse } from '../models/payment.models';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  constructor(private http: HttpClient) { }

  initializePayment(orderId: number, request: InitPaymentRequest): Observable<InitPaymentResponse> {
    return this.http.post<InitPaymentResponse>(`${API_URL}/payments/init/${orderId}`, request);
  }
}
