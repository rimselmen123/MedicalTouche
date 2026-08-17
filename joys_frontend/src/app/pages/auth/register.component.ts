import { Component, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { RegisterRequestDto } from '../../core/models/auth.models';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent {
  loading = false;
  form = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    phone: [''],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]]
  });

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private toast: ToastService,
    private router: Router
  ) { }

  submit() {
    if (this.form.invalid) return;

    const v = this.form.value as any;
    if (v.password !== v.confirmPassword) {
      this.toast.error('Les mots de passe ne correspondent pas.');
      return;
    }

    const payload: RegisterRequestDto = {
      email: v.email,
      password: v.password,
      fullName: `${v.firstName} ${v.lastName}`.trim(),
      phone: v.phone || undefined
    };

    console.log('📤 Sending registration payload:', payload);

    this.loading = true;
    this.auth.register(payload).subscribe({
      next: () => {
        this.toast.success('Compte créé avec succès. Bienvenue !');
        this.router.navigateByUrl('/');
        this.loading = false;
      },
      error: (err) => {
        console.error('❌ Registration failed:', err);
        console.log('🔍 Error object:', JSON.stringify(err.error, null, 2));

        let msg = 'Inscription échouée.';
        if (err.error) {
          // Identity errors: { errors: { messages: [...] } }
          if (err.error.errors?.messages && Array.isArray(err.error.errors.messages)) {
            msg = err.error.errors.messages.join(' | ');
          }
          // ProblemDetails: { detail, title }
          else if (err.error.detail) {
            msg = err.error.detail;
          }
          // Validation errors: { errors: { field: [...] } }
          else if (err.error.errors) {
            msg = Object.values(err.error.errors).flat().map(String).join(' | ');
          }
          else if (typeof err.error === 'string') {
            msg = err.error;
          }
        }

        this.toast.error(msg);
        this.loading = false;
      }
    });
  }
}