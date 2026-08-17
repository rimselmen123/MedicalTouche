import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';
@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {
  constructor(private auth: AuthService, private router: Router) {}
  canActivate(route: ActivatedRouteSnapshot): boolean | UrlTree {
    const roles = (route.data['roles'] as string[] | undefined) || [];
    if (!roles.length) return true;
    return roles.some(r => this.auth.hasRole(r)) ? true : this.router.createUrlTree(['/forbidden']);
  }
}
