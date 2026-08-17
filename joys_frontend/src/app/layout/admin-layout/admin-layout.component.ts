import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

interface MenuItem {
    label: string;
    route: string;
    exactMatch?: boolean;
}

interface MenuSection {
    title: string;
    items: MenuItem[];
}

@Component({
    selector: 'app-admin-layout',
    templateUrl: './admin-layout.component.html',
    standalone: false,
    styleUrls: ['./admin-layout.component.scss']
})
export class AdminLayoutComponent {
    sidebarOpen = false;
    user$ = this.auth.user$;

    menuSections: MenuSection[] = [
        {
            title: 'Vue d\'ensemble', // J'ai traduit les titres pour le "chic"
            items: [
                { label: 'Tableau de Bord', route: '/admin', exactMatch: true }
            ]
        },
        {
            title: 'Catalogue',
            items: [
                { label: 'Catégories', route: '/admin/categories' },
                { label: 'Produits', route: '/admin/products' },
                { label: 'Inventaire & Stock', route: '/admin/stock' }
            ]
        },
        {
            title: 'Commercial',
            items: [
                { label: 'Commandes', route: '/admin/orders' }
            ]
        },
        {
            title: 'Relation Client',
            items: [
                { label: 'Réclamations', route: '/admin/reclamations' }
            ]
        }
    ];

    constructor(
        private auth: AuthService,
        private router: Router
    ) { }

    toggleSidebar() {
        this.sidebarOpen = !this.sidebarOpen;
    }

    closeSidebar() {
        this.sidebarOpen = false;
    }

    logout() {
        this.auth.logout();
        this.router.navigateByUrl('/');
    }
}