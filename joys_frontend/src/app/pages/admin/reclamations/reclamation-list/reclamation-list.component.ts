import { Component, OnInit } from '@angular/core';
import { ReclamationService } from '../../../../core/services/reclamation.service';
import { Reclamation, ReclamationStatus } from '../../../../core/models/reclamation.models';

@Component({
    selector: 'app-admin-reclamation-list',
    templateUrl: './reclamation-list.component.html',
    standalone: false,
    styleUrls: ['./reclamation-list.component.scss']
})
export class ReclamationListComponent implements OnInit {
    reclamations: Reclamation[] = [];
    loading = false;
    statusFilter: ReclamationStatus | '' = '';
    page = 1;
    pageSize = 20;

    constructor(private reclamationService: ReclamationService) { }

    ngOnInit() {
        this.loadReclamations();
    }

    loadReclamations() {
        this.loading = true;
        this.reclamationService.getReclamations({
            status: this.statusFilter || undefined,
            page: this.page,
            pageSize: this.pageSize
        }).subscribe({
            next: (res) => {
                this.reclamations = res.items;
                this.loading = false;
            },
            error: () => {
                this.loading = false;
            }
        });
    }

    onFilterChange() {
        this.page = 1;
        this.loadReclamations();
    }
}