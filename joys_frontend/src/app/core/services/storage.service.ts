import { Injectable } from '@angular/core';
@Injectable({ providedIn: 'root' })
export class StorageService {
  get accessToken(): string | null { return localStorage.getItem('accessToken'); }
  set accessToken(v: string | null) { v ? localStorage.setItem('accessToken', v) : localStorage.removeItem('accessToken'); }
  get userJson(): string | null { return localStorage.getItem('authUser'); }
  set userJson(v: string | null) { v ? localStorage.setItem('authUser', v) : localStorage.removeItem('authUser'); }
  clear() { localStorage.removeItem('accessToken'); localStorage.removeItem('authUser'); }
}
