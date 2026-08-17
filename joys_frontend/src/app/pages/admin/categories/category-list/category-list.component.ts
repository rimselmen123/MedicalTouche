import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ArticleService } from '../../../../core/services/article.service';
import { Category } from '../../../../core/models/article.models';
import { ToastService } from '../../../../core/services/toast.service';
import { Subscription } from 'rxjs';
import { environment } from '../../../../../environments/environment';

@Component({
    selector: 'app-admin-category-list',
    templateUrl: './category-list.component.html',
    standalone: false,
    styleUrls: ['./category-list.component.scss']
})
export class CategoryListComponent implements OnInit, OnDestroy {
    categories: Category[] = [];
    loading = false;
    showModal = false;
    form: FormGroup;
    submitting = false;

    isEdit = false;
    editingId: number | null = null;

    // Image handling
    selectedImage: File | null = null;
    imagePreview: string | null = null;
    removeImage = false;
    existingImageUrl: string | null = null;

    private subs: Subscription[] = [];

    constructor(
        private articleService: ArticleService,
        private toast: ToastService,
        private fb: FormBuilder
    ) {
        this.form = this.fb.group({
            name: ['', Validators.required]
        });
    }

    ngOnInit() {
        this.loadCategories();
    }

    ngOnDestroy() {
        this.subs.forEach(s => s.unsubscribe());
    }

    getImageUrl(cat: Category): string | null {
        if (!cat.imageUrl) return null;
        if (cat.imageUrl.startsWith('http')) return cat.imageUrl;
        const base = environment.apiBaseUrl.replace(/\/api$/, '');
        return base + cat.imageUrl;
    }

    loadCategories() {
        this.loading = true;
        const sub = this.articleService.getAdminCategories().subscribe({
            next: (res) => {
                this.categories = res;
                this.loading = false;
            },
            error: () => {
                this.loading = false;
                this.toast.error('Erreur chargement catégories.');
            }
        });
        this.subs.push(sub);
    }

    openModal() {
        this.isEdit = false;
        this.editingId = null;
        this.form.reset();
        this.resetImage();
        this.showModal = true;
    }

    openEditModal(cat: Category) {
        this.isEdit = true;
        this.editingId = cat.id;
        this.form.patchValue({ name: cat.name });
        this.resetImage();
        this.existingImageUrl = this.getImageUrl(cat);
        this.showModal = true;
    }

    onImageSelected(event: Event) {
        const input = event.target as HTMLInputElement;
        if (!input.files || input.files.length === 0) return;
        const file = input.files[0];

        if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
            this.toast.error('Format non supporté (JPG, PNG, WebP uniquement).');
            return;
        }
        if (file.size > 5 * 1024 * 1024) {
            this.toast.error('Image trop volumineuse (max 5 Mo).');
            return;
        }

        this.selectedImage = file;
        this.removeImage = false;

        const reader = new FileReader();
        reader.onload = () => this.imagePreview = reader.result as string;
        reader.readAsDataURL(file);
    }

    clearImage() {
        this.selectedImage = null;
        this.imagePreview = null;
        if (this.isEdit && this.existingImageUrl) {
            this.removeImage = true;
            this.existingImageUrl = null;
        }
    }

    private resetImage() {
        this.selectedImage = null;
        this.imagePreview = null;
        this.removeImage = false;
        this.existingImageUrl = null;
    }

    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        this.submitting = true;
        const name = this.form.value.name;

        if (this.isEdit && this.editingId !== null) {
            const sub = this.articleService.updateCategory(this.editingId, {
                name,
                image: this.selectedImage || undefined,
                removeImage: this.removeImage
            }).subscribe({
                next: () => {
                    this.toast.success('Catégorie mise à jour !');
                    this.showModal = false;
                    this.submitting = false;
                    this.loadCategories();
                },
                error: () => {
                    this.toast.error('Erreur mise à jour.');
                    this.submitting = false;
                }
            });
            this.subs.push(sub);
        } else {
            const sub = this.articleService.createCategory({
                name,
                image: this.selectedImage || undefined
            }).subscribe({
                next: (res) => {
                    this.toast.success('Catégorie créée !');
                    res.articlesCount = 0;
                    this.categories.push(res);
                    this.showModal = false;
                    this.submitting = false;
                },
                error: () => {
                    this.toast.error('Erreur création catégorie.');
                    this.submitting = false;
                }
            });
            this.subs.push(sub);
        }
    }

    toggleActive(id: number) {
        const cat = this.categories.find(c => c.id === id);
        if (!cat) return;
        const newStatus = !cat.isActive;
        const sub = this.articleService.toggleCategory(id, newStatus).subscribe({
            next: () => {
                this.toast.success('Statut mis à jour.');
                cat.isActive = newStatus;
            },
            error: () => this.toast.error('Erreur toggle.')
        });
        this.subs.push(sub);
    }

    // Delete Modal
    showDeleteModal = false;
    itemToDeleteId: number | null = null;

    delete(id: number) {
        this.itemToDeleteId = id;
        this.showDeleteModal = true;
    }

    confirmDelete() {
        if (this.itemToDeleteId === null) return;
        const id = this.itemToDeleteId;
        const sub = this.articleService.deleteCategory(id).subscribe({
            next: () => {
                this.toast.success('Catégorie supprimée.');
                this.categories = this.categories.filter(c => c.id !== id);
                this.closeDeleteModal();
            },
            error: () => {
                this.toast.error('Erreur suppression.');
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
}
