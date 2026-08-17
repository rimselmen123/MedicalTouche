import { Component, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  loading = false;
  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private toast: ToastService,
    private router: Router
  ) { }

  submit() {
    if (this.form.invalid) return;
    this.loading = true;

    this.auth.login(this.form.value as any).subscribe({
      next: (res) => {
        this.toast.success('Bon retour, ' + res.fullName + ' !');
        const isAdmin = res.roles.includes('Admin');
        this.router.navigateByUrl(isAdmin ? '/admin/dashboard' : '/');
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.toast.error('Email ou mot de passe invalide.');
        this.loading = false;
      }
    });
  }
}