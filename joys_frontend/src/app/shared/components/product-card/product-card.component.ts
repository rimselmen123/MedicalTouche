import { Component, Input, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Article } from '../../../core/models/article.models';
import { Router } from '@angular/router';

@Component({
    selector: 'app-product-card',
    standalone: false,
    templateUrl: './product-card.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrls: ['./product-card.component.scss']
})
export class ProductCardComponent implements OnInit {
    @Input() article!: Article;
    @Input() showActions = true;

    currentIndex = 0;
    gallery: string[] = [];

    constructor(private router: Router) { }

    ngOnInit() {
        this.initGallery();
    }

    private initGallery() {
        this.gallery = [];
        // 1. Cover
        if (this.article.coverUrl) {
            this.gallery.push(this.article.coverUrl);
        } else {
            this.gallery.push('assets/placeholder.png');
        }
        // 2. Images secondaires (sans doublons)
        if (this.article.images && this.article.images.length > 0) {
            this.article.images.forEach(img => {
                if (img.url !== this.article.coverUrl) {
                    this.gallery.push(img.url);
                }
            });
        }
    }

    get hasMultipleImages(): boolean {
        return this.gallery.length > 1;
    }

    nextImage(event: Event) {
        event.stopPropagation();
        if (!this.hasMultipleImages) return;
        this.currentIndex = (this.currentIndex + 1) % this.gallery.length;
    }

    prevImage(event: Event) {
        event.stopPropagation();
        if (!this.hasMultipleImages) return;
        this.currentIndex = (this.currentIndex - 1 + this.gallery.length) % this.gallery.length;
    }

    getDiscount(): number {
        if (!this.article.oldPrice || this.article.oldPrice <= this.article.price) return 0;
        return Math.round((1 - this.article.price / this.article.oldPrice) * 100);
    }

    onCardClick() {
        this.router.navigate(['/shop/products', this.article.id]);
    }
}