import { Component, OnInit, OnDestroy, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ArticleService } from '../../../../core/services/article.service';
import { ToastService } from '../../../../core/services/toast.service';
import { Category, ArticleImageDto } from '../../../../core/models/article.models';
import { environment } from '../../../../../environments/environment';
import { Subscription, of } from 'rxjs';
import { catchError, finalize, switchMap, delay } from 'rxjs/operators';

@Component({
    selector: 'app-admin-product-edit',
    templateUrl: './product-edit.component.html',
    standalone: false,
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrls: ['./product-edit.component.scss']
})
export class ProductEditComponent implements OnInit, OnDestroy {
    form: FormGroup;
    categories: Category[] = [];
    loading = false;
    isEdit = false;
    articleId: number | null = null;

    selectedFiles: File[] = [];
    coverIndex: number | null = null;
    existingImages: ArticleImageDto[] = [];

    /* ── Wizard ── */
    currentStep = 1;
    totalSteps = 3;
    steps = [
        { num: 1, title: 'Informations', desc: 'Titre, description, catégorie' },
        { num: 2, title: 'Médias', desc: 'Photos et images du produit' },
        { num: 3, title: 'Tarification', desc: 'Prix, stock et options' }
    ];

    private subs: Subscription[] = [];

    constructor(
        private fb: FormBuilder,
        private route: ActivatedRoute,
        private router: Router,
        private articleService: ArticleService,
        private toast: ToastService,
        private cdr: ChangeDetectorRef
    ) {
        this.form = this.fb.group({
            categoryId: [null, Validators.required],
            title: ['', Validators.required],
            description: ['', Validators.required],
            price: [0, [Validators.required, Validators.min(0)]],
            oldPrice: [null],
            isFeatured: [false],
            isMadeToOrder: [false],
            stockQuantity: [null],
            isActive: [true]
        });
    }

    ngOnInit() {
        this.loadCategories();

        const sub = this.route.paramMap.subscribe(params => {
            const id = params.get('id');
            if (id && id !== 'new') {
                this.isEdit = true;
                this.articleId = +id;
                this.loadArticle(this.articleId);
            } else {
                this.isEdit = false;
                this.articleId = null;
            }
        });
        this.subs.push(sub);
    }

    ngOnDestroy(): void {
        this.subs.forEach(s => s.unsubscribe());
    }

    /* ── Wizard Navigation ── */
    nextStep(): void {
        if (this.currentStep === 1 && !this.validateStep1()) return;
        if (this.currentStep < this.totalSteps) this.currentStep++;
    }

    prevStep(): void {
        if (this.currentStep > 1) this.currentStep--;
    }

    goToStep(step: number): void {
        if (step < this.currentStep) {
            this.currentStep = step;
        } else if (step === this.currentStep + 1) {
            this.nextStep();
        }
    }

    private validateStep1(): boolean {
        const controls = ['title', 'description', 'categoryId'];
        let valid = true;
        controls.forEach(c => {
            const ctrl = this.form.get(c);
            if (ctrl) {
                ctrl.markAsTouched();
                if (ctrl.invalid) valid = false;
            }
        });
        if (!valid) this.toast.error('Veuillez remplir tous les champs obligatoires.');
        return valid;
    }

    /* ── Files ── */
    onFilesSelected(event: Event) {
        const input = event.target as HTMLInputElement;
        if (input.files && input.files.length > 0) {
            const newFiles = Array.from(input.files);
            this.selectedFiles = [...this.selectedFiles, ...newFiles];
            if (!this.isEdit && this.coverIndex === null) {
                this.coverIndex = 0;
            }
            // Reset input so same file can be re-selected
            input.value = '';
        }
    }

    removeSelectedFile(index: number) {
        this.selectedFiles.splice(index, 1);
        if (this.coverIndex !== null) {
            if (this.coverIndex === index) {
                this.coverIndex = this.selectedFiles.length > 0 ? 0 : null;
            } else if (this.coverIndex > index) {
                this.coverIndex--;
            }
        }
    }

    setCreateCoverIndex(idx: number) {
        this.coverIndex = idx;
    }

    /* ── Data Loading ── */
    loadCategories() {
        const sub = this.articleService.getCategories().subscribe({
            next: res => (this.categories = res),
            error: () => this.toast.error('Erreur chargement catégories.')
        });
        this.subs.push(sub);
    }

    loadArticle(id: number) {
        this.loading = true;
        this.existingImages = [];

        const sub = this.articleService.getArticle(id).pipe(
            switchMap((res: any) => {
                this.form.patchValue({
                    categoryId: res.categoryId,
                    title: res.title,
                    description: res.description,
                    price: res.price,
                    oldPrice: res.oldPrice,
                    isFeatured: res.isFeatured,
                    isMadeToOrder: res.isMadeToOrder,
                    stockQuantity: res.stockQuantity,
                    isActive: res.isActive
                });
                return this.articleService.getArticleImages(id).pipe(
                    catchError(() => of([] as ArticleImageDto[])),
                    switchMap((imgs) => {
                        this.existingImages = imgs || [];
                        return of(true);
                    })
                );
            }),
            finalize(() => (this.loading = false))
        ).subscribe({
            next: () => { },
            error: () => this.toast.error('Erreur chargement article.')
        });
        this.subs.push(sub);
    }

    /* ── Submit ── */
    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            this.toast.error('Veuillez remplir tous les champs obligatoires.');
            this.currentStep = 1;
            return;
        }

        this.loading = true;
        const val = this.form.value;

        if (this.isEdit) {
            const id = this.articleId!;
            const sub = this.articleService.updateArticle(id, val).pipe(
                switchMap(() => {
                    if (this.selectedFiles.length === 0) return of(null);
                    return this.articleService.addArticleImages(id, this.selectedFiles);
                }),
                finalize(() => (this.loading = false))
            ).subscribe({
                next: () => {
                    this.toast.success('Article mis à jour !');
                    this.selectedFiles = [];
                    this.loadArticle(id);
                },
                error: () => this.toast.error('Erreur lors de la mise à jour.')
            });
            this.subs.push(sub);
        } else {
            const createData: any = {
                title: val.title,
                description: val.description,
                price: val.price,
                categoryId: val.categoryId,
                oldPrice: val.oldPrice,
                isActive: val.isActive,
                isFeatured: val.isFeatured,
                isMadeToOrder: val.isMadeToOrder,
                stockQuantity: val.stockQuantity,
                images: this.selectedFiles.length > 0 ? this.selectedFiles : undefined,
                coverIndex: this.coverIndex ?? 0
            };

            const sub = this.articleService.createArticle(createData).pipe(
                finalize(() => (this.loading = false))
            ).subscribe({
                next: () => {
                    this.toast.success('Article créé avec succès !');
                    this.router.navigate(['/admin/products']);
                },
                error: () => this.toast.error('Erreur lors de la création.')
            });
            this.subs.push(sub);
        }
    }

    /* ── Image Helpers ── */
    getExistingImageSrc(imageId: number): string {
        const img = this.existingImages.find(i => i.id === imageId);
        if (img && img.url) {
            if (img.url.startsWith('http')) return img.url;
            const baseUrl = environment.apiBaseUrl.replace(/\/api\/?$/, '');
            return `${baseUrl}${img.url}`;
        }
        return 'assets/placeholder.png';
    }

    private reloadImagesOnly() {
        if (!this.articleId) return;
        this.existingImages = [];
        const sub = this.articleService.getArticleImages(this.articleId).pipe(
            catchError(() => of([] as ArticleImageDto[])),
            finalize(() => this.cdr.detectChanges())
        ).subscribe({
            next: (imgs) => this.existingImages = imgs || [],
            error: () => this.toast.error('Erreur rechargement images.')
        });
        this.subs.push(sub);
    }

    setAsCover(imageId: number) {
        if (!this.articleId) return;
        this.existingImages = this.existingImages.map(img => ({ ...img, isCover: img.id === imageId }));
        this.cdr.detectChanges();

        const sub = this.articleService.setArticleCover(this.articleId, imageId).pipe(
            delay(500)
        ).subscribe({
            next: () => this.toast.success('Cover mis à jour.'),
            error: () => { this.toast.error('Erreur mise à jour cover.'); this.reloadImagesOnly(); }
        });
        this.subs.push(sub);
    }

    deleteExistingImage(imageId: number) {
        if (!this.articleId) return;
        if (!confirm('Supprimer cette image ?')) return;

        const sub = this.articleService.deleteArticleImage(this.articleId, imageId).subscribe({
            next: () => {
                this.toast.success('Image supprimée.');
                this.existingImages = this.existingImages.filter(i => i.id !== imageId);
            },
            error: () => this.toast.error('Erreur suppression image.')
        });
        this.subs.push(sub);
    }
}
