import { Component, OnInit } from '@angular/core';
import { ReclamationService } from '../../../../core/services/reclamation.service';
import { Reclamation } from '../../../../core/models/reclamation.models';

@Component({
    selector: 'app-client-reclamation-list',
    templateUrl: './reclamation-list.component.html',
    standalone: false,
    styleUrls: ['./reclamation-list.component.scss']
})
export class ClientReclamationListComponent implements OnInit {
    reclamations: Reclamation[] = [];
    loading = false;

    constructor(private reclamationService: ReclamationService) { }

    ngOnInit() {
        this.loading = true;
        this.reclamationService.getMyReclamations().subscribe({
            next: (res: Reclamation[]) => {
                this.reclamations = res;
                this.loading = false;
            },
            error: () => this.loading = false
        });
    }
}