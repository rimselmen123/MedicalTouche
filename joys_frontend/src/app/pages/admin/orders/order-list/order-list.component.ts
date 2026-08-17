import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { OrderService } from '../../../../core/services/order.service';
import { Order } from '../../../../core/models/order.models';
import { ToastService } from '../../../../core/services/toast.service';

const STATUS_INFO: { [key: string]: { label: string; css: string; step: number } } = {
    'Pending':          { label: 'En attente',      css: 'pending',    step: 1 },
    'AwaitingPayment':  { label: 'Attente paiement', css: 'pending',   step: 1 },
    'Paid':             { label: 'Payée',            css: 'pending',    step: 1 },
    'Preparing':        { label: 'En préparation',  css: 'preparing',  step: 2 },
    'Ready':            { label: 'Prête',            css: 'preparing',  step: 2 },
    'OutForDelivery':   { label: 'Expédiée',        css: 'shipped',    step: 3 },
    'Delivered':        { label: 'Livrée',           css: 'delivered',  step: 4 },
    'Canceled':         { label: 'Annulée',         css: 'canceled',   step: 0 },
    'Refunded':         { label: 'Remboursée',      css: 'canceled',   step: 0 },
};

const DEFAULT_STATUS = { label: '—', css: 'pending', step: 0 };

@Component({
    selector: 'app-admin-order-list',
    templateUrl: './order-list.component.html',
    standalone: false,
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrls: ['./order-list.component.scss']
})
export class OrderListComponent implements OnInit {
    orders: Order[] = [];
    loading = false;

    filterStatus: string = '';
    searchQuery: string = '';

    statusFilters = [
        { label: 'En attente',     values: 'Pending' },
        { label: 'Payée',          values: 'Paid' },
        { label: 'En préparation', values: 'Preparing' },
        { label: 'Expédiée',       values: 'OutForDelivery' },
        { label: 'Livrée',         values: 'Delivered' },
        { label: 'Annulée',        values: 'Canceled' },
    ];

    currentPage = 1;
    pageSize = 20;
    totalCount = 0;
    totalPages = 0;

    constructor(
        private orderService: OrderService,
        private toast: ToastService
    ) { }

    ngOnInit() { this.loadOrders(); }

    loadOrders() {
        this.loading = true;
        const params: any = { page: this.currentPage, pageSize: this.pageSize };
        if (this.filterStatus !== '') params.status = this.filterStatus;
        if (this.searchQuery) params.search = this.searchQuery;

        this.orderService.getOrders(params).subscribe({
            next: (res) => {
                this.orders = res.items;
                this.totalCount = res.totalCount;
                this.totalPages = Math.ceil(res.totalCount / this.pageSize);
                this.loading = false;
            },
            error: () => {
                this.loading = false;
                this.toast.error('Erreur de chargement.');
            }
        });
    }

    status(s: any) { return STATUS_INFO[String(s)] || DEFAULT_STATUS; }

    getNextAction(order: Order): { label: string; icon: string; next: string } | null {
        const s = String(order.status);
        if (s === 'Pending')                    return { label: 'Confirmer',  icon: '✓', next: 'Preparing' };
        if (s === 'Paid')                       return { label: 'Préparer',   icon: '✓', next: 'Preparing' };
        if (s === 'Preparing' || s === 'Ready') return { label: 'Expédier',   icon: '🚚', next: 'OutForDelivery' };
        if (s === 'OutForDelivery')             return { label: 'Livrée',     icon: '✓',  next: 'Delivered' };
        return null;
    }

    canCancel(order: Order): boolean {
        const s = String(order.status);
        return ['Pending', 'AwaitingPayment', 'Preparing', 'Ready', 'OutForDelivery'].includes(s);
    }

    advance(order: Order) {
        const action = this.getNextAction(order);
        if (!action) return;
        this.updateStatus(order, action.next);
    }

    cancel(order: Order) {
        this.updateStatus(order, 'Canceled');
    }

    private updateStatus(order: Order, newStatus: string) {
        const label = STATUS_INFO[newStatus]?.label || newStatus;
        if (!confirm(`#${order.orderNumber} → ${label} ?`)) return;

        this.orderService.updateOrderStatus(order.id, { status: newStatus as any }).subscribe({
            next: () => {
                this.toast.success(`#${order.orderNumber} → ${label}`);
                this.loadOrders();
            },
            error: (err) => {
                const detail = err?.error?.detail;
                this.toast.error(detail || 'Erreur.');
            }
        });
    }

    onFilterChange() { this.currentPage = 1; this.loadOrders(); }
    onSearch()       { this.currentPage = 1; this.loadOrders(); }
    nextPage()       { if (this.currentPage < this.totalPages) { this.currentPage++; this.loadOrders(); } }
    prevPage()       { if (this.currentPage > 1) { this.currentPage--; this.loadOrders(); } }
}
