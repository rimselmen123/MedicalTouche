import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
    selector: 'app-confirmation-modal',
    templateUrl: './confirmation-modal.component.html',
    styleUrls: ['./confirmation-modal.component.scss'],
    standalone: false
})
export class ConfirmationModalComponent {
    @Input() title: string = 'Confirmer';
    @Input() message: string = 'Êtes-vous sûr de vouloir continuer ?';
    @Input() confirmText: string = 'Confirmer';
    @Input() cancelText: string = 'Annuler';
    @Input() isDestructive: boolean = false; // If true, confirm button is red

    @Output() confirm = new EventEmitter<void>();
    @Output() cancel = new EventEmitter<void>();

    onConfirm() {
        this.confirm.emit();
    }

    onCancel() {
        this.cancel.emit();
    }
}
