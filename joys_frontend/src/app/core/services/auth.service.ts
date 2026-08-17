import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, tap } from 'rxjs';
import { API_URL } from './api.config';
import { AuthResponseDto, LoginRequestDto, RegisterRequestDto, AuthUserDto } from '../models/auth.models';
import { StorageService } from './storage.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private _user = new BehaviorSubject<AuthUserDto | null>(this.loadUser());
  user$ = this._user.asObservable();

  constructor(private http: HttpClient, private storage: StorageService) { }

  get user(): AuthUserDto | null { return this._user.value; }
  get isLoggedIn(): boolean { return !!this.user; }
  hasRole(role: string): boolean { return (this.user?.roles || []).includes(role); }

  register(payload: RegisterRequestDto) { return this.http.post<AuthResponseDto>(`${API_URL}/auth/register`, payload).pipe(tap(res => this.persist(res))); }
  login(payload: LoginRequestDto) { return this.http.post<AuthResponseDto>(`${API_URL}/auth/login`, payload).pipe(tap(res => this.persist(res))); }
  getCurrentUser() { return this.http.get<any>(`${API_URL}/auth/me`); }
  logout() { this.storage.clear(); this._user.next(null); }

  private persist(res: AuthResponseDto) {
    this.storage.accessToken = res.token;
    const user: AuthUserDto = {
      userId: res.userId,
      email: res.email,
      fullName: res.fullName,
      roles: res.roles
    };
    this.storage.userJson = JSON.stringify(user);
    this._user.next(user);
  }
  private loadUser(): AuthUserDto | null { try { const raw = this.storage.userJson; return raw ? JSON.parse(raw) : null; } catch { return null; } }
}
