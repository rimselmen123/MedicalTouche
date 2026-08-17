import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { AuthInterceptor } from './core/interceptors/auth.interceptor';

import { ShellComponent } from './layout/shell.component';
import { NavbarComponent } from './layout/navbar.component';
import { FooterComponent } from './layout/footer.component';
import { AdminLayoutComponent } from './layout/admin-layout/admin-layout.component';
import { PageHeaderComponent } from './shared/page-header.component';
import { LoadingComponent } from './shared/loading.component';
import { EmptyStateComponent } from './shared/empty-state.component';
import { FileUrlPipe } from './shared/pipes/file-url.pipe';
import { PremiumImgPipe } from './shared/pipes/premium-img.pipe';
import { ConfirmationModalComponent } from './shared/components/confirmation-modal/confirmation-modal.component';
import { ProductCardComponent } from './shared/components/product-card/product-card.component';

// Public / Shop
import { HomeComponent } from './pages/home/home.component';
import { CatalogComponent } from './pages/shop/catalog/catalog.component';
import { ArticleDetailsComponent } from './pages/shop/article-details/article-details.component';

// Auth
import { LoginComponent } from './pages/auth/login.component';
import { RegisterComponent } from './pages/auth/register.component';

// Client
import { CartComponent } from './pages/client/cart/cart.component';
import { CheckoutComponent } from './pages/client/checkout/checkout.component';
import { OrdersComponent } from './pages/client/orders/orders.component';
import { ProfileComponent } from './pages/client/profile/profile.component';
import { ClientOrderDetailComponent } from './pages/client/orders/order-detail/order-detail.component';
import { ClientReclamationListComponent } from './pages/client/reclamations/reclamation-list/reclamation-list.component';
import { ClientReclamationCreateComponent } from './pages/client/reclamations/reclamation-create/reclamation-create.component';

// Admin
import { DashboardComponent } from './pages/admin/dashboard/dashboard.component';
import { ProductListComponent } from './pages/admin/products/product-list/product-list.component';
import { ProductEditComponent } from './pages/admin/products/product-edit/product-edit.component';
import { OrderListComponent } from './pages/admin/orders/order-list/order-list.component';
import { OrderDetailComponent } from './pages/admin/orders/order-detail/order-detail.component';
import { StockComponent } from './pages/admin/stock/stock.component';
import { ReclamationListComponent } from './pages/admin/reclamations/reclamation-list/reclamation-list.component';
import { ReclamationDetailComponent } from './pages/admin/reclamations/reclamation-detail/reclamation-detail.component';
import { CategoryListComponent } from './pages/admin/categories/category-list/category-list.component';

// Misc
import { ForbiddenComponent } from './pages/misc/forbidden.component';
import { NotFoundComponent } from './pages/misc/not-found.component';

@NgModule({
  declarations: [
    AppComponent,
    ShellComponent, NavbarComponent, FooterComponent, AdminLayoutComponent,
    PageHeaderComponent, LoadingComponent, EmptyStateComponent,

    // Pages
    HomeComponent, CatalogComponent, ArticleDetailsComponent,
    LoginComponent, RegisterComponent,
    CartComponent, CheckoutComponent, OrdersComponent, ProfileComponent,
    ClientReclamationListComponent, ClientReclamationCreateComponent, ClientOrderDetailComponent,

    DashboardComponent, ProductListComponent, ProductEditComponent,
    OrderListComponent, OrderDetailComponent, StockComponent,
    ReclamationListComponent, ReclamationDetailComponent, CategoryListComponent,

    ForbiddenComponent, NotFoundComponent,
    FileUrlPipe,
    PremiumImgPipe,
    ConfirmationModalComponent,
    ProductCardComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    ReactiveFormsModule,
    FormsModule,
    AppRoutingModule
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
