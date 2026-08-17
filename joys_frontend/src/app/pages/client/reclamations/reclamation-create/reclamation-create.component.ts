import { Component, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ReclamationService } from '../../../../core/services/reclamation.service';
import { ToastService } from '../../../../core/services/toast.service';
import { CreateReclamationRequest } from 'src/app/core/models/reclamation.models';

@Component({
    selector: 'app-client-reclamation-create',
    templateUrl: './reclamation-create.component.html',
    standalone: false,
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrls: ['./reclamation-create.component.scss']
})
export class ClientReclamationCreateComponent {
    form = this.fb.group({
        fullName: ['', Validators.required],
        email: ['', [Validators.required, Validators.email]],
        phone: ['', Validators.required],
        subject: ['', Validators.required],
        message: ['', Validators.required]
    });
    submitting = false;

    constructor(
        private fb: FormBuilder,
        private reclamationService: ReclamationService,
        private toast: ToastService,
        private router: Router
    ) { }

    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }
        this.submitting = true;

        const payload: CreateReclamationRequest = this.form.value as CreateReclamationRequest;

        this.reclamationService.createReclamation(payload).subscribe({
            next: () => {
                this.toast.success('Réclamation envoyée.');
                this.router.navigate(['/']);
                this.submitting = false;
            },
            error: () => {
                this.toast.error('Erreur lors de l\'envoi.');
                this.submitting = false;
            }
        });
    }
}