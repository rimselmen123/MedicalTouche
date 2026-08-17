import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { RoleGuard } from './core/guards/role.guard';
import { ROLES } from './core/models/auth.models';
import { AdminLayoutComponent } from './layout/admin-layout/admin-layout.component';

import { HomeComponent } from './pages/home/home.component';
import { LoginComponent } from './pages/auth/login.component';
import { RegisterComponent } from './pages/auth/register.component';

import { CatalogComponent } from './pages/shop/catalog/catalog.component';
import { ArticleDetailsComponent } from './pages/shop/article-details/article-details.component';

import { CartComponent } from './pages/client/cart/cart.component';
import { CheckoutComponent } from './pages/client/checkout/checkout.component';
import { OrdersComponent } from './pages/client/orders/orders.component';
import { ClientOrderDetailComponent } from './pages/client/orders/order-detail/order-detail.component';
import { ProfileComponent } from './pages/client/profile/profile.component';
import { ClientReclamationListComponent } from './pages/client/reclamations/reclamation-list/reclamation-list.component';
import { ClientReclamationCreateComponent } from './pages/client/reclamations/reclamation-create/reclamation-create.component';

import { DashboardComponent } from './pages/admin/dashboard/dashboard.component';
import { ProductListComponent } from './pages/admin/products/product-list/product-list.component';
import { ProductEditComponent } from './pages/admin/products/product-edit/product-edit.component';
import { OrderListComponent } from './pages/admin/orders/order-list/order-list.component';
import { OrderDetailComponent } from './pages/admin/orders/order-detail/order-detail.component';
import { StockComponent } from './pages/admin/stock/stock.component';
import { ReclamationListComponent } from './pages/admin/reclamations/reclamation-list/reclamation-list.component';
import { ReclamationDetailComponent } from './pages/admin/reclamations/reclamation-detail/reclamation-detail.component';
import { CategoryListComponent } from './pages/admin/categories/category-list/category-list.component';

import { ForbiddenComponent } from './pages/misc/forbidden.component';
import { NotFoundComponent } from './pages/misc/not-found.component';

const routes: Routes = [
  { path: '', component: HomeComponent },

  // Shop
  { path: 'catalog', component: CatalogComponent },
  { path: 'shop/catalog', component: CatalogComponent },
  { path: 'shop/products/:id', component: ArticleDetailsComponent },
  { path: 'article/:id', redirectTo: 'shop/products/:id' },

  // Auth
  { path: 'auth/login', component: LoginComponent },
  { path: 'auth/register', component: RegisterComponent },

  // Client
  {
    path: 'client',
    canActivate: [AuthGuard],
    children: [
      { path: 'cart', component: CartComponent },
      { path: 'checkout', component: CheckoutComponent },
      { path: 'orders', component: OrdersComponent },
      { path: 'orders/:id', component: ClientOrderDetailComponent },
      { path: 'profile', component: ProfileComponent },
      { path: 'reclamations', component: ClientReclamationListComponent },
      { path: 'reclamations/new', component: ClientReclamationCreateComponent }
    ]
  },
  { path: 'cart', redirectTo: 'client/cart' },
  { path: 'checkout', redirectTo: 'client/checkout' },
  { path: 'orders', redirectTo: 'client/orders' },
  { path: 'profile', redirectTo: 'client/profile' },

  // Admin
  {
    path: 'admin',
    component: AdminLayoutComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: [ROLES.Admin] },
    children: [
      { path: '', component: DashboardComponent },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'products', component: ProductListComponent },
      { path: 'products/new', component: ProductEditComponent },
      { path: 'products/:id', component: ProductEditComponent },
      { path: 'orders', component: OrderListComponent },
      { path: 'orders/:id', component: OrderDetailComponent },
      { path: 'stock', component: StockComponent },
      { path: 'categories', component: CategoryListComponent },
      { path: 'reclamations', component: ReclamationListComponent },
      { path: 'reclamations/:id', component: ReclamationDetailComponent }
    ]
  },

  { path: 'forbidden', component: ForbiddenComponent },
  { path: '**', component: NotFoundComponent }
];

@NgModule({ imports: [RouterModule.forRoot(routes, { scrollPositionRestoration: 'enabled' })], exports: [RouterModule] })
export class AppRoutingModule { }
