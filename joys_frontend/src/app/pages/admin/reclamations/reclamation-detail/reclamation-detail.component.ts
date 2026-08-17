import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ReclamationService } from '../../../../core/services/reclamation.service';
import { Reclamation, ReclamationStatus } from '../../../../core/models/reclamation.models';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-admin-reclamation-detail',
  templateUrl: './reclamation-detail.component.html',
  standalone: false,
  styleUrls: ['./reclamation-detail.component.scss']
})
export class ReclamationDetailComponent implements OnInit {
  reclamation: Reclamation | null = null;
  loading = false;
  replyForm: FormGroup;
  statusOptions: ReclamationStatus[] = ['New', 'InProgress', 'Resolved'];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private reclamationService: ReclamationService,
    private fb: FormBuilder,
    private toast: ToastService
  ) {
    this.replyForm = this.fb.group({
      status: ['New' as ReclamationStatus, Validators.required],
      adminNote: ['']
    });
  }

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) this.loadReclamation(+id);
    });
  }

  loadReclamation(id: number) {
    this.loading = true;
    this.reclamationService.getReclamation(id).subscribe({
      next: (res) => {
        this.reclamation = res;
        this.replyForm.patchValue({
          status: res.status,
          adminNote: res.adminNote || ''
        });
        this.loading = false;
      },
      error: () => {
        this.toast.error('Réclamation introuvable');
        this.router.navigate(['/admin/reclamations']);
      }
    });
  }

  submitReply() {
    if (!this.reclamation || this.replyForm.invalid) return;

    this.reclamationService.respondReclamation(this.reclamation.id, this.replyForm.value).subscribe({
      next: () => {
        this.toast.success('Réponse envoyée avec succès');
        this.router.navigate(['/admin/reclamations']);
      },
      error: () => {
        this.toast.error('Erreur lors de l\'envoi de la réponse');
      }
    });
  }
}
