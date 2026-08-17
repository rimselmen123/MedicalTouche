import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { ArticleService } from '../../../../core/services/article.service';
import { Article } from '../../../../core/models/article.models';
import { ToastService } from '../../../../core/services/toast.service';
import { Subscription } from 'rxjs';
import { environment } from '../../../../../environments/environment';

@Component({
    selector: 'app-admin-product-list',
    templateUrl: './product-list.component.html',
    standalone: false,
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrls: ['./product-list.component.scss']
})
export class ProductListComponent implements OnInit, OnDestroy {
    articles: Article[] = [];
    loading = false;
    page = 1;
    total = 0;

    private subs: Subscription[] = [];

    constructor(
        private articleService: ArticleService,
        private toast: ToastService
    ) { }

    ngOnInit() {
        this.loadArticles();
    }

    ngOnDestroy(): void {
        this.subs.forEach(s => s.unsubscribe());
    }

    loadArticles() {
        this.loading = true;

        const sub = this.articleService.getArticles({ page: 1, pageSize: 100 }).subscribe({
            next: (res) => {
                this.articles = res.items;
                this.total = res.totalCount;
                this.loading = false;
            },
            error: () => {
                this.loading = false;
                this.toast.error('Erreur lors du chargement.');
            }
        });

        this.subs.push(sub);
    }

    // Delete Modal
    showDeleteModal = false;
    itemToDeleteId: number | null = null;

    deleteArticle(id: number) {
        this.itemToDeleteId = id;
        this.showDeleteModal = true;
    }

    confirmDelete() {
        if (this.itemToDeleteId === null) return;

        const id = this.itemToDeleteId;
        const sub = this.articleService.deleteArticle(id).subscribe({
            next: () => {
                this.toast.success('Article supprimé.');
                this.articles = this.articles.filter(a => a.id !== id);
                this.closeDeleteModal();
            },
            error: (err) => {
                const msg = err?.error?.detail || 'Erreur lors de la suppression.';
                this.toast.error(msg);
                this.closeDeleteModal();
            }
        });
        this.subs.push(sub);
    }

    cancelDelete() {
        this.closeDeleteModal();
    }

    private closeDeleteModal() {
        this.showDeleteModal = false;
        this.itemToDeleteId = null;
    }

    // Toggle Status
    toggleActive(id: number) {
        const item = this.articles.find(a => a.id === id);
        if (!item) return;

        const newStatus = !item.isActive;
        const sub = this.articleService.toggleArticle(id, newStatus).subscribe({
            next: () => {
                this.toast.success('Statut mis à jour.');
                item.isActive = newStatus;
            },
            error: () => this.toast.error('Erreur lors de la mise à jour.')
        });
        this.subs.push(sub);
    }

}
