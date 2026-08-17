import { Component, HostListener, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../core/services/auth.service';
import { CartService } from '../core/services/cart.service';
import { ArticleService } from '../core/services/article.service';
import { Category } from '../core/models/article.models';
import { Subscription, interval } from 'rxjs';

@Component({
    selector: 'app-navbar',
    templateUrl: './navbar.component.html',
    styleUrls: ['./navbar.component.scss'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class NavbarComponent implements OnInit, OnDestroy {
    isScrolled = false;
    searchQuery = '';
    showMobileMenu = false;

    categories: Category[] = [];
    bannerTexts: string[] = [
        'Bienvenue chez Joy\'s — Story Mode',
        'Nouveautés disponibles chaque semaine',
        'Livraison rapide partout en Tunisie',
        'Qualité premium · Prix imbattables'
    ];
    currentBannerIndex = 0;

    cartItemCount$ = this.cartService.cartItemCount$;
    cartTotal = 0;
    user$ = this.auth.user$;

    private subs: Subscription[] = [];

    constructor(
        public auth: AuthService,
        private cartService: CartService,
        private articleService: ArticleService,
        private router: Router
    ) { }

    ngOnInit(): void {
        this.subs.push(
            this.articleService.getCategories().subscribe({
                next: cats => {
                    this.categories = cats;
                    if (cats.length > 0) {
                        this.bannerTexts = cats.map(c => c.name);
                    }
                },
                error: () => { /* keep default bannerTexts */ }
            })
        );

        this.subs.push(
            interval(3000).subscribe(() => {
                this.currentBannerIndex = (this.currentBannerIndex + 1) % this.bannerTexts.length;
            })
        );
    }

    ngOnDestroy(): void {
        this.subs.forEach(s => s.unsubscribe());
    }

    @HostListener('window:scroll', [])
    onWindowScroll() {
        this.isScrolled = window.scrollY > 20;
    }

    prevBanner() {
        this.currentBannerIndex =
            (this.currentBannerIndex - 1 + this.bannerTexts.length) % this.bannerTexts.length;
    }

    nextBanner() {
        this.currentBannerIndex =
            (this.currentBannerIndex + 1) % this.bannerTexts.length;
    }

    onSearch() {
        if (this.searchQuery.trim()) {
            this.router.navigate(['/shop/catalog'], {
                queryParams: { search: this.searchQuery.trim() }
            });
            this.searchQuery = '';
        }
    }

    onSearchKey(event: KeyboardEvent) {
        if (event.key === 'Enter') {
            this.onSearch();
        }
    }

    toggleMobileMenu() {
        this.showMobileMenu = !this.showMobileMenu;
    }

    logout() {
        this.auth.logout();
        this.router.navigateByUrl('/');
    }
}
