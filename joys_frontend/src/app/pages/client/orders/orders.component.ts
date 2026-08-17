import { Component, OnInit } from '@angular/core';
import { Order } from 'src/app/core/models/order.models';
import { OrderService } from 'src/app/core/services/order.service';

const STATUS_MAP: { [key: string]: { label: string; css: string } } = {
    'Pending':          { label: 'En attente',      css: 'pending' },
    'AwaitingPayment':  { label: 'Attente paiement', css: 'pending' },
    'Paid':             { label: 'Payée',            css: 'pending' },
    'Preparing':        { label: 'En préparation',  css: 'preparing' },
    'Ready':            { label: 'Prête',            css: 'preparing' },
    'OutForDelivery':   { label: 'Expédiée',        css: 'shipped' },
    'Delivered':        { label: 'Livrée',           css: 'delivered' },
    'Canceled':         { label: 'Annulée',         css: 'canceled' },
    'Refunded':         { label: 'Remboursée',      css: 'canceled' },
};

const DEFAULT_STATUS = { label: '—', css: 'pending' };

@Component({
    selector: 'app-orders',
    templateUrl: './orders.component.html',
    standalone: false,
    styleUrls: ['./orders.component.scss']
})
export class OrdersComponent implements OnInit {
    orders: Order[] = [];
    loading = false;

    constructor(private orderService: OrderService) { }

    ngOnInit() { this.loadOrders(); }

    loadOrders() {
        this.loading = true;
        this.orderService.getMyOrders().subscribe({
            next: (res) => { this.orders = res; this.loading = false; },
            error: () => this.loading = false
        });
    }

    status(s: any) { return STATUS_MAP[String(s)] || DEFAULT_STATUS; }
    isPaid(ps: any): boolean { return String(ps) === 'Paid'; }
}
