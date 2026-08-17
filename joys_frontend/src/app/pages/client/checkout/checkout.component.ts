import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CartService } from '../../../core/services/cart.service';
import { OrderService } from '../../../core/services/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { Cart } from '../../../core/models/cart.models';
import { Address, CreateOrderRequest } from '../../../core/models/order.models';

@Component({
  selector: 'app-checkout',
  templateUrl: './checkout.component.html',
  standalone: false,
  styleUrls: ['./checkout.component.scss']
})
export class CheckoutComponent implements OnInit {
  cart: Cart | null = null;
  addresses: Address[] = [];
  selectedAddressId: number | null = null;
  showAddressForm = false;

  addressForm: FormGroup;
  loadingCart = true;
  loadingAddresses = true;
  submitting = false;

  paymentMethod: number = 0; // 0 = CashOnDelivery, 1 = Online
  notes: string = '';

  sliderIndices: { [key: number]: number } = {};

  constructor(
    private fb: FormBuilder,
    private cartService: CartService,
    private orderService: OrderService,
    private toast: ToastService,
    private router: Router
  ) {
    this.addressForm = this.fb.group({
      label: ['Maison'],
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
    this.loadCart();
    this.loadAddresses();
  }

  loadCart() {
    this.loadingCart = true;
    this.cartService.getCart().subscribe({
      next: (cart) => {
        if (!cart || cart.items.length === 0) {
          this.toast.error('Votre panier est vide.');
          this.router.navigate(['/client/cart']);
          return;
        }
        this.cart = cart;
        cart.items.forEach(item => this.sliderIndices[item.id] = 0);
        this.loadingCart = false;
      },
      error: () => {
        this.loadingCart = false;
        this.router.navigate(['/client/cart']);
      }
    });
  }

  loadAddresses() {
    this.loadingAddresses = true;
    this.orderService.getAddresses().subscribe({
      next: (addresses) => {
        this.addresses = addresses;
        const defaultAddr = addresses.find(a => a.isDefault);
        if (defaultAddr) {
          this.selectedAddressId = defaultAddr.id;
        } else if (addresses.length > 0) {
          this.selectedAddressId = addresses[0].id;
        }
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
      next: (newAddress) => {
        this.toast.success('Adresse ajoutée.');
        this.addresses.push(newAddress);
        this.selectedAddressId = newAddress.id;
        this.showAddressForm = false;
        this.addressForm.reset({ label: 'Maison' });
      },
      error: () => this.toast.error('Erreur lors de l\'ajout de l\'adresse.')
    });
  }

  submit() {
    if (!this.cart || !this.selectedAddressId) {
      this.toast.error('Veuillez sélectionner une adresse de livraison.');
      return;
    }

    const selectedAddress = this.addresses.find(a => a.id === this.selectedAddressId);
    if (!selectedAddress) {
      this.toast.error('Adresse non trouvée.');
      return;
    }

    this.submitting = true;

    const request: CreateOrderRequest = {
      paymentMethod: this.paymentMethod,
      customerNote: this.notes || undefined,
      shippingAddress: {
        fullName: selectedAddress.fullName,
        phone: selectedAddress.phone,
        line1: selectedAddress.line1,
        line2: selectedAddress.line2,
        city: selectedAddress.city,
        postalCode: selectedAddress.postalCode,
        governorate: selectedAddress.governorate
      },
      items: this.cart.items.map(item => ({
        articleId: item.articleId,
        variantId: item.variantId,
        quantity: item.quantity
      }))
    };

    this.orderService.createOrder(request).subscribe({
      next: (order) => {
        this.toast.success('Commande confirmée !');
        this.cartService.clearCart().subscribe();
        this.router.navigate(['/client/orders', order.id]);
        this.submitting = false;
      },
      error: (err) => {
        console.error('Order error:', err);
        this.toast.error('Erreur lors de la commande.');
        this.submitting = false;
      }
    });
  }

  getActiveImage(item: any): string {
    const index = this.sliderIndices[item.id] || 0;
    if (item.images && item.images.length > 0) {
      return item.images[index].url || item.images[index];
    }
    return item.imageUrl || 'assets/placeholder.png';
  }

  hasMultipleImages(item: any): boolean {
    return item.images && item.images.length > 1;
  }

  nextImage(item: any, event: Event) {
    event.stopPropagation();
    event.preventDefault();
    if (!this.hasMultipleImages(item)) return;
    const count = item.images.length;
    const current = this.sliderIndices[item.id] || 0;
    this.sliderIndices[item.id] = (current + 1) % count;
  }

  prevImage(item: any, event: Event) {
    event.stopPropagation();
    event.preventDefault();
    if (!this.hasMultipleImages(item)) return;
    const count = item.images.length;
    const current = this.sliderIndices[item.id] || 0;
    this.sliderIndices[item.id] = (current - 1 + count) % count;
  }
}
