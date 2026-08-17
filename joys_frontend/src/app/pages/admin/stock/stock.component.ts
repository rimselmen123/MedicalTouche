import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { StockService } from '../../../core/services/stock.service';
import { ArticleService } from '../../../core/services/article.service';
import { ToastService } from '../../../core/services/toast.service';
import { StockItem, StockMovement, StockReservation } from '../../../core/models/stock.models';
import { Article } from '../../../core/models/article.models';

@Component({
    selector: 'app-stock',
    standalone: false,
    templateUrl: './stock.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrls: ['./stock.component.scss']
})
export class StockComponent implements OnInit {
    activeTab: 'items' | 'movements' | 'reservations' = 'items';
    stockItems: StockItem[] = [];
    movements: StockMovement[] = [];
    reservations: StockReservation[] = [];
    articles: Article[] = [];
    loading = false;
    showAdjustModal = false;
    selectedItem: StockItem | null = null;

    adjustForm: FormGroup;

    constructor(
        private fb: FormBuilder,
        private stockService: StockService,
        private articleService: ArticleService,
        private toast: ToastService
    ) {
        this.adjustForm = this.fb.group({
            articleId: ['', Validators.required],
            delta: ['', [Validators.required, Validators.pattern(/^-?\d+$/)]],
            note: ['', Validators.required]
        });
    }

    ngOnInit() {
        this.loadData();
        this.loadArticles();
    }

    setTab(tab: 'items' | 'movements' | 'reservations') {
        this.activeTab = tab;
        this.loadData();
    }

    loadData() {
        this.loading = true;
        if (this.activeTab === 'items') {
            this.stockService.getStockItems().subscribe({
                next: (items) => {
                    this.stockItems = items;
                    this.loading = false;
                },
                error: () => {
                    this.toast.error('Erreur chargement stock');
                    this.loading = false;
                }
            });
        } else if (this.activeTab === 'movements') {
            this.stockService.getMovements({ limit: 50 }).subscribe({
                next: (movements) => {
                    this.movements = movements;
                    this.loading = false;
                },
                error: () => {
                    this.toast.error('Erreur chargement mouvements');
                    this.loading = false;
                }
            });
        } else if (this.activeTab === 'reservations') {
            this.stockService.getReservations('Active').subscribe({
                next: (reservations) => {
                    this.reservations = reservations;
                    this.loading = false;
                },
                error: () => {
                    this.toast.error('Erreur chargement réservations');
                    this.loading = false;
                }
            });
        }
    }

    loadArticles() {
        this.articleService.getArticles({ pageSize: 100 }).subscribe({
            next: (res) => {
                this.articles = res.items;
            }
        });
    }

    openAdjustModal(item: StockItem) {
        this.selectedItem = item;
        this.adjustForm.patchValue({
            articleId: item.articleId,
            delta: 0,
            note: ''
        });
        this.showAdjustModal = true;
    }

    closeAdjustModal() {
        this.showAdjustModal = false;
        this.selectedItem = null;
        this.adjustForm.reset();
    }

    submitAdjust() {
        if (this.adjustForm.invalid) return;

        const request = this.adjustForm.value;
        request.delta = parseInt(request.delta, 10);

        if (request.delta === 0) {
            this.toast.error('Le delta ne peut pas être 0');
            return;
        }

        this.stockService.adjustStock(request).subscribe({
            next: () => {
                this.toast.success('Stock ajusté avec succès');
                this.closeAdjustModal();
                this.loadData();
            },
            error: () => {
                this.toast.error('Erreur ajustement stock');
            }
        });
    }

    expireReservations() {
        if (!confirm('Êtes-vous sûr de vouloir expirer toutes les réservations périmées ?')) return;

        this.loading = true;
        this.stockService.expireReservations().subscribe({
            next: (res) => {
                this.toast.success(`${res.expired} réservations expirées.`);
                this.loadData();
            },
            error: () => {
                this.toast.error('Erreur expiration');
                this.loading = false;
            }
        });
    }

    getArticleName(articleId: number): string {
        const article = this.articles.find(a => a.id === articleId);
        return article ? article.title : `Article #${articleId}`;
    }
}