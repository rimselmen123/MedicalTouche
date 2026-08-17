import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { OrderService } from '../../../core/services/order.service';
import { Address } from '../../../core/models/order.models';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
  user$ = this.auth.user$;
  addresses: Address[] = [];
  addressForm: FormGroup;
  showAddressForm = false;
  loadingAddresses = false;

  constructor(
    private auth: AuthService,
    private orderService: OrderService,
    private fb: FormBuilder,
    private toast: ToastService
  ) {
    this.addressForm = this.fb.group({
      label: ['Domicile'],
      fullName: ['', Validators.required],
      phone: ['', Validators.required],
      line1: ['', Validators.required],
      line2: [''],
      city: ['', Validators.required],
      postalCode: [''],
      governorate: ['']
    });
  }

  ngOnInit() {
    this.loadAddresses();
  }

  loadAddresses() {
    this.loadingAddresses = true;
    this.orderService.getAddresses().subscribe({
      next: (res) => {
        this.addresses = res;
        this.loadingAddresses = false;
      },
      error: () => this.loadingAddresses = false
    });
  }

  createAddress() {
    if (this.addressForm.invalid) {
      this.addressForm.markAllAsTouched();
      return;
    }

    const v = this.addressForm.value;
    this.orderService.createAddress({
      label: v.label || undefined,
      fullName: v.fullName,
      phone: v.phone,
      line1: v.line1,
      line2: v.line2 || undefined,
      city: v.city,
      postalCode: v.postalCode || undefined,
      governorate: v.governorate || undefined
    }).subscribe({
      next: (res) => {
        this.toast.success('Adresse ajoutée.');
        this.addresses.push(res);
        this.showAddressForm = false;
        this.addressForm.reset({ label: 'Domicile' });
      },
      error: () => {
        this.toast.error('Erreur lors de l\'ajout.');
      }
    });
  }

  setDefault(id: number) {
    this.orderService.setDefaultAddress(id).subscribe({
      next: () => {
        this.addresses.forEach(a => a.isDefault = a.id === id);
        this.toast.success('Adresse par défaut mise à jour.');
      },
      error: () => this.toast.error('Erreur lors de la mise à jour.')
    });
  }

  deleteAddress(id: number) {
    if (!confirm('Supprimer cette adresse ?')) return;
    this.orderService.deleteAddress(id).subscribe({
      next: () => {
        this.addresses = this.addresses.filter(a => a.id !== id);
        this.toast.success('Adresse supprimée.');
      },
      error: () => this.toast.error('Impossible de supprimer l\'adresse.')
    });
  }

  logout() {
    this.auth.logout();
  }
}
