import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { ArticleService } from '../../../core/services/article.service';
import { Category, Article } from '../../../core/models/article.models';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { environment } from 'src/environments/environment';

@Component({
    selector: 'app-catalog',
    standalone: false,
    templateUrl: './catalog.component.html',
    styleUrls: ['./catalog.component.scss']
})
export class CatalogComponent implements OnInit {
    categories: Category[] = [];
    articles: Article[] = [];

    // Filters
    selectedCategoryId: number | null = null; // null au lieu de undefined pour la logique des boutons
    searchQuery: string = '';
    private searchSubject = new Subject<string>();

    // Pagination
    currentPage = 1;
    pageSize = 12;
    totalCount = 0;
    totalPages = 0;
    loading = false;

    constructor(
        private articleService: ArticleService,
        private router: Router,
        private route: ActivatedRoute
    ) {
        this.searchSubject.pipe(
            debounceTime(400),
            distinctUntilChanged()
        ).subscribe(query => {
            this.searchQuery = query;
            this.currentPage = 1;
            this.loadArticles();
        });
    }

    ngOnInit() {
        this.loadCategories();

        // Check query params for initial state
        this.route.queryParams.subscribe(params => {
            this.selectedCategoryId = params['category'] ? +params['category'] : null;
            this.searchQuery = params['term'] || '';
            this.currentPage = params['page'] ? +params['page'] : 1;
            this.loadArticles();
        });
    }

    loadCategories() {
        this.articleService.getCategories().subscribe(res => this.categories = res);
    }

    loadArticles() {
        this.loading = true;
        this.articleService.getArticles({
            categoryId: this.selectedCategoryId || undefined,
            search: this.searchQuery || undefined,
            page: this.currentPage,
            pageSize: this.pageSize
        }).subscribe({
            next: (res) => {
                this.articles = res.items;
                this.totalCount = res.totalCount;
                this.totalPages = Math.ceil(this.totalCount / this.pageSize);
                this.loading = false;
            },
            error: () => this.loading = false
        });
    }

    onSearch(event: any) {
        this.searchSubject.next(event.target.value);
    }

    selectCategory(id: number | null) {
        this.selectedCategoryId = id;
        this.currentPage = 1;
        this.updateUrl();
        this.loadArticles();
    }

    onPageChange(page: number) {
        this.currentPage = page;
        this.updateUrl();
        this.loadArticles();
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    resetFilters() {
        this.searchQuery = '';
        this.selectedCategoryId = null;
        this.currentPage = 1;
        this.updateUrl();
        this.loadArticles();
    }

    private updateUrl() {
        this.router.navigate([], {
            relativeTo: this.route,
            queryParams: {
                category: this.selectedCategoryId,
                term: this.searchQuery || null,
                page: this.currentPage
            },
            queryParamsHandling: 'merge'
        });
    }

    get pages(): number[] {
        return Array.from({ length: this.totalPages }, (_, i) => i + 1);
    }
}