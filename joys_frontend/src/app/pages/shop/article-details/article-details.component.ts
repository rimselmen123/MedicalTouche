import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ArticleService } from '../../../core/services/article.service';
import { CartService } from '../../../core/services/cart.service';
import { ToastService } from '../../../core/services/toast.service';
import { Article, ArticleVariant, ArticleImageDto } from '../../../core/models/article.models';

@Component({
    selector: 'app-article-details',
    templateUrl: './article-details.component.html',
    standalone: false,
    styleUrls: ['./article-details.component.scss'],
    changeDetection: ChangeDetectionStrategy.Eager,
    encapsulation: ViewEncapsulation.None
})
export class ArticleDetailsComponent implements OnInit {
    article: Article | null = null;
    loading = false;

    // Gallery
    gallery: string[] = [];
    currentIndex = 0;

    // Cart
    selectedVariant: ArticleVariant | null = null;
    quantity = 1;
    addingToCart = false;

    constructor(
        private route: ActivatedRoute,
        private articleService: ArticleService,
        private cartService: CartService,
        private toast: ToastService
    ) { }

    ngOnInit() {
        this.route.paramMap.subscribe(params => {
            const id = params.get('id');
            if (id) this.loadArticle(+id);
        });
    }

    loadArticle(id: number) {
        this.loading = true;
        this.articleService.getArticle(id).subscribe({
            next: (res) => {
                this.article = res;
                if (res.variants && res.variants.length > 0) {
                    this.selectVariant(res.variants[0]);
                }
                // Fetch all images separately to ensure we have the full list
                this.articleService.getArticleImages(id).subscribe({
                    next: (images) => {
                        this.buildGallery(res, images);
                        this.loading = false;
                    },
                    error: () => {
                        // Fallback: use images from article response
                        this.buildGalleryFromArticle(res);
                        this.loading = false;
                    }
                });
            },
            error: () => {
                this.toast.error('Produit introuvable');
                this.loading = false;
            }
        });
    }

    buildGallery(article: Article, images: ArticleImageDto[]) {
        this.gallery = [];
        this.currentIndex = 0;

        if (images && images.length > 0) {
            // Sort: cover first, then by sortOrder
            const sorted = [...images].sort((a, b) => {
                if (a.isCover && !b.isCover) return -1;
                if (!a.isCover && b.isCover) return 1;
                return a.sortOrder - b.sortOrder;
            });
            sorted.forEach(img => this.gallery.push(img.url));
        } else if (article.coverUrl) {
            this.gallery.push(article.coverUrl);
        }

        if (this.gallery.length === 0) {
            this.gallery.push('assets/placeholder.png');
        }
    }

    buildGalleryFromArticle(article: Article) {
        this.gallery = [];
        this.currentIndex = 0;

        if (article.coverUrl) this.gallery.push(article.coverUrl);
        if (article.images && article.images.length > 0) {
            article.images.forEach(img => {
                if (img.url !== article.coverUrl) this.gallery.push(img.url);
            });
        }
        if (this.gallery.length === 0) this.gallery.push('assets/placeholder.png');
    }

    nextImage() {
        if (this.gallery.length > 1) {
            this.currentIndex = (this.currentIndex + 1) % this.gallery.length;
        }
    }

    prevImage() {
        if (this.gallery.length > 1) {
            this.currentIndex = (this.currentIndex - 1 + this.gallery.length) % this.gallery.length;
        }
    }

    selectImage(index: number) {
        this.currentIndex = index;
    }

    selectVariant(v: ArticleVariant) {
        this.selectedVariant = v;
    }

    get displayPrice(): number {
        return this.selectedVariant ? this.selectedVariant.price : (this.article?.price || 0);
    }

    addToCart() {
        if (!this.article) return;
        if (this.article.variants.length > 0 && !this.selectedVariant) {
            this.toast.error('Veuillez choisir une option.');
            return;
        }

        this.addingToCart = true;
        this.cartService.addToCart({
            articleId: this.article.id,
            variantId: this.selectedVariant?.id || null,
            quantity: this.quantity
        }).subscribe({
            next: () => {
                this.toast.success('Ajouté au panier');
                this.addingToCart = false;
            },
            error: () => {
                this.toast.error('Erreur lors de l\'ajout');
                this.addingToCart = false;
            }
        });
    }
}
