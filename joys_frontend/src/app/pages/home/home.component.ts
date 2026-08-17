import { Component, OnInit, OnDestroy, AfterViewInit, ElementRef } from '@angular/core';
import { ArticleService } from '../../core/services/article.service';
import { Category, Article } from '../../core/models/article.models';
import { Subscription, forkJoin, interval } from 'rxjs';
import { environment } from '../../../environments/environment';

interface CategorySection {
    category: Category;
    articles: Article[];
    currentPage: number;
    totalPages: number;
}

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.scss'],
    standalone: false
})
export class HomeComponent implements OnInit, AfterViewInit, OnDestroy {

    categories: Category[] = [];
    categorySections: CategorySection[] = [];
    featuredArticles: Article[] = [];
    loading = true;
    itemsPerPage = 4;
    currentSlide = 0;

    heroSlides = [
        {
            image: 'https://images.unsplash.com/photo-1469334031218-e382a71b716b?w=1920&q=80&auto=format&fit=crop',
            title: 'Nouvelle Collection',
            subtitle: 'Découvrez les tendances de la saison'
        },
        {
            image: 'https://images.unsplash.com/photo-1490481651871-ab68de25d43d?w=1920&q=80&auto=format&fit=crop',
            title: 'Style & Élégance',
            subtitle: 'Des pièces uniques pour chaque occasion'
        },
        {
            image: 'https://images.unsplash.com/photo-1441984904996-e0b6ba687e04?w=1920&q=80&auto=format&fit=crop',
            title: 'Livraison Rapide',
            subtitle: 'Partout en Tunisie, rapidement chez vous'
        }
    ];

    private observer!: IntersectionObserver;
    private subs: Subscription[] = [];

    private categoryImageMap: { [key: string]: string } = {
        'femme':       'https://images.unsplash.com/photo-1487412720507-e7ab37603c6f?w=600&q=80&auto=format&fit=crop',
        'homme':       'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=600&q=80&auto=format&fit=crop',
        'enfant':      'https://images.unsplash.com/photo-1503919545889-aef636e10ad4?w=600&q=80&auto=format&fit=crop',
        'fille':       'https://images.unsplash.com/photo-1476234251651-f353703a034d?w=600&q=80&auto=format&fit=crop',
        'garçon':      'https://images.unsplash.com/photo-1503919545889-aef636e10ad4?w=600&q=80&auto=format&fit=crop',
        'garcon':      'https://images.unsplash.com/photo-1503919545889-aef636e10ad4?w=600&q=80&auto=format&fit=crop',
        'bébé':        'https://images.unsplash.com/photo-1519689680058-324335c77eba?w=600&q=80&auto=format&fit=crop',
        'bebe':        'https://images.unsplash.com/photo-1519689680058-324335c77eba?w=600&q=80&auto=format&fit=crop',
        'chaussure':   'https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=600&q=80&auto=format&fit=crop',
        'accessoire':  'https://images.unsplash.com/photo-1576566588028-4147f3842f27?w=600&q=80&auto=format&fit=crop',
        'sac':         'https://images.unsplash.com/photo-1548036328-c9fa89d128fa?w=600&q=80&auto=format&fit=crop',
        'robe':        'https://images.unsplash.com/photo-1496747611176-843222e1e57c?w=600&q=80&auto=format&fit=crop',
        'sport':       'https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=600&q=80&auto=format&fit=crop',
        'jean':        'https://images.unsplash.com/photo-1542272454315-4c01d7abdf4a?w=600&q=80&auto=format&fit=crop',
        'veste':       'https://images.unsplash.com/photo-1551028719-00167b16eac5?w=600&q=80&auto=format&fit=crop',
        'manteau':     'https://images.unsplash.com/photo-1551028719-00167b16eac5?w=600&q=80&auto=format&fit=crop',
        'pull':        'https://images.unsplash.com/photo-1434389677669-e08b4cda3a7f?w=600&q=80&auto=format&fit=crop',
        'chemise':     'https://images.unsplash.com/photo-1596755094514-f87e34085b2c?w=600&q=80&auto=format&fit=crop',
        'pantalon':    'https://images.unsplash.com/photo-1624378439575-d8705ad7ae80?w=600&q=80&auto=format&fit=crop',
        'lingerie':    'https://images.unsplash.com/photo-1617331140180-e8262094733a?w=600&q=80&auto=format&fit=crop',
        'maillot':     'https://images.unsplash.com/photo-1570976447640-ac859083963f?w=600&q=80&auto=format&fit=crop',
        'bijou':       'https://images.unsplash.com/photo-1515562141589-67f0d999b439?w=600&q=80&auto=format&fit=crop',
        'montre':      'https://images.unsplash.com/photo-1524592094714-0f0654e20314?w=600&q=80&auto=format&fit=crop',
        'lunette':     'https://images.unsplash.com/photo-1511499767150-a48a237f0083?w=600&q=80&auto=format&fit=crop',
        'parfum':      'https://images.unsplash.com/photo-1541643600914-78b084683601?w=600&q=80&auto=format&fit=crop',
        'épice':       'https://images.unsplash.com/photo-1596040033229-a9821ebd058d?w=600&q=80&auto=format&fit=crop',
        'piment':      'https://images.unsplash.com/photo-1588252303782-cb80119abd6d?w=600&q=80&auto=format&fit=crop',
        'conserve':    'https://images.unsplash.com/photo-1584568694244-14fbdf83bd30?w=600&q=80&auto=format&fit=crop',
        'huile':       'https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=600&q=80&auto=format&fit=crop',
    };

    private defaultImage = 'https://images.unsplash.com/photo-1441984904996-e0b6ba687e04?w=600&q=80&auto=format&fit=crop';

    constructor(
        private el: ElementRef,
        private articleService: ArticleService
    ) { }

    ngOnInit(): void {
        this.loadData();
        this.subs.push(
            interval(5000).subscribe(() => {
                this.currentSlide = (this.currentSlide + 1) % this.heroSlides.length;
            })
        );
    }

    ngAfterViewInit(): void {
        setTimeout(() => this.setupRevealObserver(), 100);
    }

    ngOnDestroy(): void {
        if (this.observer) this.observer.disconnect();
        this.subs.forEach(s => s.unsubscribe());
    }

    goToHeroSlide(index: number): void {
        this.currentSlide = index;
    }

    getCategoryImage(cat: Category): string {
        if (cat.imageUrl) {
            if (cat.imageUrl.startsWith('http')) return cat.imageUrl;
            const base = environment.apiBaseUrl.replace(/\/api$/, '');
            return base + cat.imageUrl;
        }
        const name = cat.name.toLowerCase();
        for (const key of Object.keys(this.categoryImageMap)) {
            if (name.includes(key)) return this.categoryImageMap[key];
        }
        return this.defaultImage;
    }

    scrollToProducts(): void {
        const el = document.getElementById('categories-section');
        if (el) el.scrollIntoView({ behavior: 'smooth' });
    }

    getVisibleArticles(section: CategorySection): Article[] {
        const start = section.currentPage * this.itemsPerPage;
        return section.articles.slice(start, start + this.itemsPerPage);
    }

    prevSlide(section: CategorySection): void {
        if (section.currentPage > 0) section.currentPage--;
    }

    nextSlide(section: CategorySection): void {
        if (section.currentPage < section.totalPages - 1) section.currentPage++;
    }

    goToPage(section: CategorySection, page: number): void {
        section.currentPage = page;
    }

    private loadData(): void {
        this.subs.push(
            this.articleService.getCategories().subscribe({
                next: (cats) => {
                    this.categories = cats.filter(c => c.isActive);
                    this.loadArticlesPerCategory();
                    this.loadFeaturedArticles();
                },
                error: () => this.loading = false
            })
        );
    }

    private loadFeaturedArticles(): void {
        this.subs.push(
            this.articleService.getArticles({
                featured: true, pageSize: 8, page: 1, active: true
            }).subscribe({
                next: (res) => this.featuredArticles = res.items,
                error: () => { }
            })
        );
    }

    private loadArticlesPerCategory(): void {
        if (this.categories.length === 0) { this.loading = false; return; }

        const requests = this.categories.map(cat =>
            this.articleService.getArticles({
                categoryId: cat.id, pageSize: 12, page: 1, active: true
            })
        );

        this.subs.push(
            forkJoin(requests).subscribe({
                next: (responses) => {
                    this.categorySections = responses
                        .map((res, i) => ({
                            category: this.categories[i],
                            articles: res.items,
                            currentPage: 0,
                            totalPages: Math.ceil(res.items.length / this.itemsPerPage)
                        }))
                        .filter(s => s.articles.length > 0);
                    this.loading = false;
                    setTimeout(() => this.setupRevealObserver(), 100);
                },
                error: () => this.loading = false
            })
        );
    }

    private setupRevealObserver(): void {
        if (this.observer) this.observer.disconnect();
        this.observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    (entry.target as HTMLElement).classList.add('is-visible');
                    this.observer.unobserve(entry.target);
                }
            });
        }, { root: null, rootMargin: '0px 0px -40px 0px', threshold: 0.08 });

        const targets = this.el.nativeElement.querySelectorAll('.reveal-on-scroll');
        targets.forEach((el: Element) => this.observer.observe(el));
    }
}
