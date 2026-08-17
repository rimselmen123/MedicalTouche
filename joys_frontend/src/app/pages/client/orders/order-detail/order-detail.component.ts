import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { OrderService } from '../../../../core/services/order.service';
import { Order, PAYMENT_METHOD_MAP } from '../../../../core/models/order.models';
import { ToastService } from '../../../../core/services/toast.service';

const STEPS = [
    { label: 'Confirmée',      icon: '✓' },
    { label: 'En préparation', icon: '📦' },
    { label: 'Expédiée',       icon: '🚚' },
    { label: 'Livrée',         icon: '🎉' },
];

const STATUS_LABELS: { [key: string]: string } = {
    'Pending': 'En attente',
    'AwaitingPayment': 'Attente paiement',
    'Paid': 'Confirmée',
    'Preparing': 'En préparation',
    'Ready': 'Prête',
    'OutForDelivery': 'Expédiée',
    'Delivered': 'Livrée',
    'Canceled': 'Annulée',
    'Refunded': 'Remboursée',
};

const PAYMENT_LABELS: { [key: string]: string } = {
    'Pending': 'En attente',
    'Authorized': 'Autorisé',
    'Paid': 'Payé',
    'Failed': 'Échoué',
    'Canceled': 'Annulé',
    'Refunded': 'Remboursé',
};

/** Order of statuses for step progression */
const STEP_ORDER = ['Pending', 'AwaitingPayment', 'Paid', 'Preparing', 'Ready', 'OutForDelivery', 'Delivered'];

@Component({
    selector: 'app-client-order-detail',
    templateUrl: './order-detail.component.html',
    standalone: false,
    styleUrls: ['./order-detail.component.scss']
})
export class ClientOrderDetailComponent implements OnInit {
    order: Order | null = null;
    loading = false;
    steps = STEPS;

    constructor(
        private route: ActivatedRoute,
        private orderService: OrderService,
        private toast: ToastService
    ) { }

    ngOnInit() {
        this.route.paramMap.subscribe(params => {
            const id = params.get('id');
            if (id) this.loadOrder(+id);
        });
    }

    loadOrder(id: number) {
        this.loading = true;
        this.orderService.getMyOrder(id).subscribe({
            next: (res) => { this.order = res; this.loading = false; },
            error: () => { this.toast.error('Commande introuvable'); this.loading = false; }
        });
    }

    private statusRank(): number {
        if (!this.order) return -1;
        const idx = STEP_ORDER.indexOf(String(this.order.status));
        return idx >= 0 ? idx : -1;
    }

    currentStep(): number {
        if (!this.order) return -1;
        const s = String(this.order.status);
        if (s === 'Canceled' || s === 'Refunded') return -1;
        const rank = this.statusRank();
        if (rank <= 2) return 0;                     // Pending/AwaitingPayment/Paid
        if (s === 'Preparing' || s === 'Ready') return 1;
        if (s === 'OutForDelivery') return 2;
        if (s === 'Delivered') return 3;
        return 0;
    }

    isStepDone(stepIndex: number): boolean {
        if (!this.order) return false;
        const s = String(this.order.status);
        if (s === 'Canceled' || s === 'Refunded') return false;
        const rank = this.statusRank();
        if (stepIndex === 0) return rank >= 2;  // Paid+
        if (stepIndex === 1) return rank >= 5;  // OutForDelivery+
        if (stepIndex === 2) return rank >= 6;  // Delivered
        if (stepIndex === 3) return rank >= 6;  // Delivered
        return false;
    }

    isStepActive(stepIndex: number): boolean {
        return this.currentStep() === stepIndex && !this.isStepDone(stepIndex);
    }

    isCanceled(): boolean {
        if (!this.order) return false;
        const s = String(this.order.status);
        return s === 'Canceled' || s === 'Refunded';
    }

    statusLabel(s: any): string { return STATUS_LABELS[String(s)] || `${s}`; }
    paymentLabel(s: any): string { return PAYMENT_LABELS[String(s)] || `${s}`; }
    paymentMethodLabel(m: any): string { return PAYMENT_METHOD_MAP[m] || `${m}`; }

    canCancel(): boolean {
        if (!this.order) return false;
        const s = String(this.order.status);
        return s === 'Pending' || s === 'AwaitingPayment';
    }

    cancelOrder() {
        if (!this.order || !confirm('Annuler cette commande ?')) return;
        const id = this.order.id;
        this.orderService.cancelOrder(id).subscribe({
            next: () => { this.toast.success('Commande annulée'); this.loadOrder(id); },
            error: () => this.toast.error('Impossible d\'annuler')
        });
    }
}
