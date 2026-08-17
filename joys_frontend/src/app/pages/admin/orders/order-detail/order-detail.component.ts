import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { OrderService } from '../../../../core/services/order.service';
import { Order, PAYMENT_METHOD_MAP } from '../../../../core/models/order.models';
import { ToastService } from '../../../../core/services/toast.service';

const STEPS = [
    { label: 'En attente',     css: 'pending',   backendValues: ['Pending', 'AwaitingPayment', 'Paid'] },
    { label: 'En préparation', css: 'preparing',  backendValues: ['Preparing', 'Ready'] },
    { label: 'Expédiée',       css: 'shipped',    backendValues: ['OutForDelivery'] },
    { label: 'Livrée',         css: 'delivered',  backendValues: ['Delivered'] },
];

const STATUS_LABELS: { [key: string]: string } = {
    'Pending': 'En attente',
    'AwaitingPayment': 'Attente paiement',
    'Paid': 'Payée',
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

@Component({
    selector: 'app-admin-order-detail',
    templateUrl: './order-detail.component.html',
    standalone: false,
    styleUrls: ['./order-detail.component.scss']
})
export class OrderDetailComponent implements OnInit {
    order: Order | null = null;
    loading = false;
    adminNoteInput = '';

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
        this.orderService.getOrder(id).subscribe({
            next: (res) => {
                this.order = res;
                this.adminNoteInput = res.adminNote || '';
                this.loading = false;
            },
            error: () => { this.toast.error('Commande introuvable'); this.loading = false; }
        });
    }

    currentStep(): number {
        if (!this.order) return -1;
        const s = String(this.order.status);
        if (s === 'Canceled' || s === 'Refunded') return -1;
        for (let i = 0; i < STEPS.length; i++) {
            if (STEPS[i].backendValues.includes(s)) return i;
        }
        return 0;
    }

    isCanceled(): boolean {
        if (!this.order) return false;
        const s = String(this.order.status);
        return s === 'Canceled' || s === 'Refunded';
    }

    statusLabel(s: any): string { return STATUS_LABELS[String(s)] || `${s}`; }
    paymentLabel(s: any): string { return PAYMENT_LABELS[String(s)] || `${s}`; }
    paymentMethodLabel(m: any): string { return PAYMENT_METHOD_MAP[m] || `${m}`; }

    /** Next step action — respects backend transition rules */
    getNextAction(): { label: string; next: string } | null {
        if (!this.order) return null;
        const s = String(this.order.status);
        if (s === 'Pending')                    return { label: 'Confirmer la commande',    next: 'Preparing' };
        if (s === 'Paid')                       return { label: 'Démarrer la préparation',  next: 'Preparing' };
        if (s === 'Preparing' || s === 'Ready') return { label: 'Marquer comme expédiée',   next: 'OutForDelivery' };
        if (s === 'OutForDelivery')             return { label: 'Marquer comme livrée',     next: 'Delivered' };
        return null;
    }

    canCancel(): boolean {
        if (!this.order) return false;
        const s = String(this.order.status);
        return ['Pending', 'AwaitingPayment', 'Preparing', 'Ready', 'OutForDelivery'].includes(s);
    }

    canRefund(): boolean {
        return !!this.order && String(this.order.status) === 'Paid';
    }

    advance() {
        const action = this.getNextAction();
        if (!action || !this.order) return;
        this.doUpdate(action.next, action.label);
    }

    cancel() {
        if (!this.order) return;
        this.doUpdate('Canceled', 'Annuler la commande');
    }

    refund() {
        if (!this.order) return;
        this.doUpdate('Refunded', 'Rembourser la commande');
    }

    private doUpdate(newStatus: string, actionLabel: string) {
        if (!this.order) return;
        if (!confirm(`${actionLabel} #${this.order.orderNumber} ?`)) return;

        const id = this.order.id;
        this.orderService.updateOrderStatus(id, { status: newStatus as any }).subscribe({
            next: () => { this.toast.success(STATUS_LABELS[newStatus] || 'Mis à jour'); this.loadOrder(id); },
            error: (err) => {
                const detail = err?.error?.detail;
                this.toast.error(detail || 'Erreur lors de la mise à jour.');
            }
        });
    }

    saveAdminNote() {
        if (!this.order) return;
        const id = this.order.id;
        this.orderService.updateAdminNote(id, this.adminNoteInput.trim()).subscribe({
            next: () => { this.toast.success('Note enregistrée'); this.loadOrder(id); },
            error: () => this.toast.error('Erreur')
        });
    }

    updateDeliveryFee() {
        if (!this.order) return;
        const val = prompt('Frais de livraison (TND):', (this.order.deliveryFee || 0).toString());
        if (val === null) return;
        const fee = parseFloat(val);
        if (isNaN(fee) || fee < 0) { this.toast.error('Montant invalide'); return; }

        const id = this.order.id;
        this.orderService.updateDeliveryFees(id, fee, this.order.discountTotal || 0).subscribe({
            next: () => { this.toast.success('Mis à jour'); this.loadOrder(id); },
            error: (err) => {
                const detail = err?.error?.detail;
                this.toast.error(detail || 'Erreur');
            }
        });
    }
}
