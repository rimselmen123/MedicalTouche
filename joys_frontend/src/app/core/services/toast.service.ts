import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
export interface ToastItem { id: string; type: 'success' | 'error' | 'info'; message: string; }
@Injectable({ providedIn: 'root' })
export class ToastService {
  private _items = new BehaviorSubject<ToastItem[]>([]);
  items$ = this._items.asObservable();
  push(type: ToastItem['type'], message: string) {
    const id = (typeof crypto !== 'undefined' && 'randomUUID' in crypto) ? crypto.randomUUID() : String(Date.now()) + Math.random();
    const item: ToastItem = { id, type, message };
    this._items.next([...this._items.value, item]);
    setTimeout(() => this.remove(id), 3500);
  }

  success(message: string) { this.push('success', message); }
  error(message: string) { this.push('error', message); }
  info(message: string) { this.push('info', message); }

  remove(id: string) { this._items.next(this._items.value.filter(x => x.id !== id)); }
}
