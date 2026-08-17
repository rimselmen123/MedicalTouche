import { Component, OnInit } from '@angular/core';
import { CartService } from '../../../core/services/cart.service';
import { ToastService } from '../../../core/services/toast.service';
import { Cart, CartItem } from '../../../core/models/cart.models';
import { Router } from '@angular/router';

@Component({
  selector: 'app-cart',
  templateUrl: './cart.component.html',
  standalone: false,
  styleUrls: ['./cart.component.scss']
})
export class CartComponent implements OnInit {
  cart: Cart | null = null;
  loading = false;

  constructor(
    private cartService: CartService,
    private toast: ToastService,
    private router: Router
  ) { }

  ngOnInit() {
    this.loadCart();
  }

  loadCart() {
    this.loading = true;
    this.cartService.getCart().subscribe({
      next: (cart) => {
        this.cart = cart;
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  getItemImage(item: CartItem): string {
    return item.imageUrl || 'assets/placeholder.png';
  }

  updateQuantity(itemId: number, newQuantity: number) {
    if (newQuantity < 1) return;
    this.cartService.updateItem(itemId, { quantity: newQuantity }).subscribe({
      next: (cart) => { this.cart = cart; },
      error: () => this.toast.error('Impossible de modifier la quantité.')
    });
  }

  removeItem(itemId: number) {
    this.cartService.removeItem(itemId).subscribe({
      next: (cart) => {
        this.cart = cart;
        this.toast.success('Article retiré.');
      },
      error: () => this.toast.error('Impossible de retirer l\'article.')
    });
  }

  clearCart() {
    if (!confirm('Vider le panier ?')) return;
    this.cartService.clearCart().subscribe({
      next: (cart) => {
        this.cart = cart;
        this.toast.info('Panier vidé.');
      },
      error: () => this.toast.error('Erreur lors du nettoyage du panier.')
    });
  }

  goToCheckout() {
    this.router.navigate(['/client/checkout']);
  }
}
