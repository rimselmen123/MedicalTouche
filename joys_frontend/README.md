# 📚 Hlouwa E-commerce API Documentation

**Version:** 1.0  
**Base URL:** `http://localhost:5273/api`  
**Last Updated:** February 9, 2026

---

## 🔐 Authentication

This API uses **JWT (JSON Web Token)** authentication. Include the token in the `Authorization` header:

```http
Authorization: Bearer YOUR_JWT_TOKEN
```

### Token Expiration
- **Duration:** 2 hours
- **Refresh:** Re-authenticate via `/api/auth/login` when expired

---

## 📋 Table of Contents

1. [Authentication Endpoints](#1-authentication-endpoints)
2. [Categories Endpoints](#2-categories-endpoints)
3. [Articles Endpoints](#3-articles-endpoints)
4. [Cart Endpoints](#4-cart-endpoints)
5. [Addresses Endpoints](#5-addresses-endpoints)
6. [Orders Endpoints](#6-orders-endpoints)
7. [Payments Endpoints](#7-payments-endpoints)
8. [Reclamations Endpoints](#8-reclamations-endpoints)
9. [Stock Endpoints](#9-stock-endpoints)
10. [Error Handling](#10-error-handling)

---

## 1. Authentication Endpoints

### 1.1 Register User

**Endpoint:** `POST /api/auth/register`  
**Access:** Public  
**Description:** Register a new user account (automatically assigned "Client" role)

#### Request Body
```json
{
  "email": "user@example.com",
  "password": "SecurePass123",
  "fullName": "John Doe",
  "phoneNumber": "+21612345678"
}
```

#### Response (200 OK)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-02-09T16:30:00Z",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "fullName": "John Doe",
  "roles": ["Client"]
}
```

#### TypeScript Example
```typescript
interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  phoneNumber: string;
}

async function register(data: RegisterRequest) {
  const response = await fetch('http://localhost:5273/api/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data)
  });
  return await response.json();
}
```

---

### 1.2 Login

**Endpoint:** `POST /api/auth/login`  
**Access:** Public  
**Description:** Authenticate user and receive JWT token

#### Request Body
```json
{
  "email": "user@example.com",
  "password": "SecurePass123"
}
```

#### Response (200 OK)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-02-09T16:30:00Z",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "fullName": "John Doe",
  "roles": ["Client"]
}
```

#### Error Response (401 Unauthorized)
```json
{
  "title": "Non autorisé",
  "status": 401,
  "detail": "Identifiants invalides.",
  "instance": "/api/auth/login",
  "traceId": "0HNJ7S4674263:00000001"
}
```

#### TypeScript Example
```typescript
async function login(email: string, password: string) {
  const response = await fetch('http://localhost:5273/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  });
  
  if (!response.ok) throw new Error('Login failed');
  
  const data = await response.json();
  localStorage.setItem('token', data.token);
  return data;
}
```

---

### 1.3 Get Current User Profile

**Endpoint:** `GET /api/auth/me`  
**Access:** Authenticated (Client or Admin)  
**Description:** Get current authenticated user's profile

#### Request Headers
```http
Authorization: Bearer YOUR_JWT_TOKEN
```

#### Response (200 OK)
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "fullName": "John Doe",
  "phoneNumber": "+21612345678",
  "roles": ["Client"]
}
```

#### TypeScript Example
```typescript
async function getCurrentUser(token: string) {
  const response = await fetch('http://localhost:5273/api/auth/me', {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  return await response.json();
}
```

---

## 2. Categories Endpoints

### 2.1 Get All Categories (Public)

**Endpoint:** `GET /api/categories`  
**Access:** Public  
**Description:** Get list of all active categories

#### Response (200 OK)
```json
[
  {
    "id": 1,
    "name": "Electronics",
    "slug": "electronics",
    "isActive": true,
    "sortOrder": 0
  },
  {
    "id": 2,
    "name": "Fashion",
    "slug": "fashion",
    "isActive": true,
    "sortOrder": 0
  }
]
```

#### TypeScript Example
```typescript
interface Category {
  id: number;
  name: string;
  slug: string;
  isActive: boolean;
  sortOrder: number;
}

async function getCategories(): Promise<Category[]> {
  const response = await fetch('http://localhost:5273/api/categories');
  return await response.json();
}
```

---

### 2.2 Get Category by Slug

**Endpoint:** `GET /api/categories/by-slug/{slug}`  
**Access:** Public  
**Description:** Get single category by slug

#### URL Parameters
- `slug` (string): Category slug (e.g., "electronics")

#### Response (200 OK)
```json
{
  "id": 1,
  "name": "Electronics",
  "slug": "electronics",
  "description": "Electronic devices and gadgets",
  "isActive": true,
  "sortOrder": 0
}
```

#### TypeScript Example
```typescript
async function getCategoryBySlug(slug: string) {
  const response = await fetch(`http://localhost:5273/api/categories/by-slug/${slug}`);
  return await response.json();
}
```

---

### 2.3 Create Category (Admin)

**Endpoint:** `POST /api/categories`
**Access:** Admin only
**Description:** Create a new category

#### Request Headers
```http
Authorization: Bearer ADMIN_JWT_TOKEN
Content-Type: application/json
```

#### Request Body
```json
{
  "name": "Electronics",
  "slug": "electronics",
  "description": "Electronic devices and gadgets"
}
```

#### Response (201 Created)
```json
{
  "id": 1,
  "name": "Electronics",
  "slug": "electronics",
  "isActive": true,
  "sortOrder": 0
}
```

---

### 2.4 Update Category (Admin)

**Endpoint:** `PUT /api/categories/{id}`
**Access:** Admin only
**Description:** Update existing category

#### Request Body
```json
{
  "name": "Consumer Electronics",
  "slug": "consumer-electronics",
  "description": "Updated description"
}
```

#### Response (200 OK)
```json
{
  "id": 1,
  "name": "Consumer Electronics",
  "slug": "consumer-electronics",
  "isActive": true,
  "sortOrder": 0
}
```

---

### 2.5 Toggle Category Active Status (Admin)

**Endpoint:** `PATCH /api/categories/{id}`  
**Access:** Admin only  
**Description:** Activate or deactivate category

#### Response (204 No Content)

---

### 2.6 Delete Category (Admin)

**Endpoint:** `DELETE /api/categories/{id}`  
**Access:** Admin only  
**Description:** Soft delete category

#### Response (204 No Content)

---

## 3. Articles Endpoints

### 3.1 Get All Articles (Public)

**Endpoint:** `GET /api/articles`
**Access:** Public
**Description:** Get paginated list of articles with filters

#### Query Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | integer | 1 | Page number |
| `pageSize` | integer | 20 | Items per page (max 100) |
| `search` | string | - | Search in title/description |
| `categoryId` | integer | - | Filter by category |
| `active` | boolean | - | Filter by active status |
| `featured` | boolean | - | Filter featured articles |
| `minPrice` | decimal | - | Minimum price filter |
| `maxPrice` | decimal | - | Maximum price filter |
| `sort` | string | "newest" | Sort: newest, priceAsc, priceDesc |

#### Example Request
```
GET /api/articles?page=1&pageSize=10&categoryId=1&featured=true
```

#### Response (200 OK)
```json
{
  "items": [
    {
      "id": 1,
      "title": "Smartphone Pro X",
      "slug": "smartphone-pro-x",
      "price": 799.000,
      "isActive": true,
      "isFeatured": true,
      "categoryName": "Electronics",
      "categoryId": 1,
      "createdAt": "2026-02-09T14:01:59.2150616"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1
}
```

#### TypeScript Example
```typescript
interface ArticleListItem {
  id: number;
  title: string;
  slug: string;
  price: number;
  isActive: boolean;
  isFeatured: boolean;
  categoryName: string;
  categoryId: number;
  createdAt: string;
}

interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

async function getArticles(params: {
  page?: number;
  pageSize?: number;
  categoryId?: number;
  featured?: boolean;
}): Promise<PagedResult<ArticleListItem>> {
  const queryParams = new URLSearchParams();
  if (params.page) queryParams.set('page', params.page.toString());
  if (params.pageSize) queryParams.set('pageSize', params.pageSize.toString());
  if (params.categoryId) queryParams.set('categoryId', params.categoryId.toString());
  if (params.featured !== undefined) queryParams.set('featured', params.featured.toString());
  
  const response = await fetch(`http://localhost:5273/api/articles?${queryParams}`);
  return await response.json();
}
```

---

### 3.2 Get Article by ID

**Endpoint:** `GET /api/articles/{id}`  
**Access:** Public  
**Description:** Get detailed article information

#### Response (200 OK)
```json
{
  "id": 1,
  "title": "Smartphone Pro X",
  "slug": "smartphone-pro-x",
  "description": "High-end smartphone with amazing features",
  "price": 799.000,
  "oldPrice": null,
  "isActive": true,
  "isFeatured": true,
  "isMadeToOrder": true,
  "categoryId": 1,
  "categoryName": "Electronics",
  "createdAt": "2026-02-09T14:01:59.2150616",
  "images": [],
  "variants": []
}
```

---

### 3.3 Get Article by Slug

**Endpoint:** `GET /api/articles/by-slug/{slug}`  
**Access:** Public  
**Description:** Get article by slug (same response as by ID)

---

### 3.4 Create Article (Admin)

**Endpoint:** `POST /api/articles`  
**Access:** Admin only  
**Content-Type:** `multipart/form-data`  
**Description:** Create new article with optional images

#### Request Body (Form Data)
```
Title: "Smartphone Pro X"
Slug: "smartphone-pro-x" (optional, auto-generated if null)
Description: "High-end smartphone"
Price: 799
OldPrice: 999 (optional)
CategoryId: 1
IsActive: true
IsFeatured: true
IsMadeToOrder: true
StockQuantity: 50 (optional)
Images: [File1, File2] (optional)
CoverIndex: 0 (optional, default first image)
VariantsJson: "[{\"name\":\"Red\",\"price\":799,\"isDefault\":true}]" (optional)
```

#### Response (201 Created)
```json
{
  "id": 1,
  "title": "Smartphone Pro X",
  "slug": "smartphone-pro-x",
  "description": "High-end smartphone",
  "price": 799.000,
  "isActive": true,
  "isFeatured": true,
  "isMadeToOrder": true,
  "categoryId": 1,
  "categoryName": "Electronics",
  "createdAt": "2026-02-09T14:01:59.2150616",
  "images": [],
  "variants": []
}
```

#### TypeScript Example (with File Upload)
```typescript
async function createArticle(data: {
  title: string;
  description: string;
  price: number;
  categoryId: number;
  images?: File[];
}, token: string) {
  const formData = new FormData();
  formData.append('Title', data.title);
  formData.append('Description', data.description);
  formData.append('Price', data.price.toString());
  formData.append('CategoryId', data.categoryId.toString());
  formData.append('IsActive', 'true');
  
  if (data.images) {
    data.images.forEach(file => formData.append('Images', file));
  }
  
  const response = await fetch('http://localhost:5273/api/articles', {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` },
    body: formData
  });
  
  return await response.json();
}
```

---

### 3.5 Update Article (Admin)

**Endpoint:** `PUT /api/articles/{id}`  
**Access:** Admin only  
**Content-Type:** `application/json`

#### Request Body
```json
{
  "title": "Updated Title",
  "price": 899.00,
  "isActive": true,
  "categoryId": 1
}
```

#### Response (200 OK)
```json
{
  "id": 1,
  "title": "Updated Title",
  "price": 899.000,
  "isActive": true
}
```

---

### 3.6 Toggle Article Active Status (Admin)

**Endpoint:** `PATCH /api/articles/{id}`
**Access:** Admin only

#### Response (204 No Content)

---

### 3.7 Delete Article (Admin)

**Endpoint:** `DELETE /api/articles/{id}`
**Access:** Admin only

#### Response (204 No Content)

---

### 3.8 Upload Article Images (Admin)

**Endpoint:** `POST /api/articles/{id}/images`
**Access:** Admin only
**Content-Type:** `multipart/form-data`

#### Request Body
```
files: [File1, File2, File3]
coverIndex: 0
```

#### Response (200 OK)
```json
{
  "uploadedImages": [
    {
      "id": 1,
      "url": "/uploads/articles/image1.jpg",
      "isCover": true,
      "sortOrder": 0
    }
  ]
}
```

---

### 3.9 Delete Article Image (Admin)

**Endpoint:** `DELETE /api/articles/{articleId}/images/{imageId}`  
**Access:** Admin only

#### Response (204 No Content)

---

## 4. Cart Endpoints

### 4.1 Get User Cart

**Endpoint:** `GET /api/cart`  
**Access:** Client or Admin  
**Description:** Get current user's shopping cart

#### Request Headers
```http
Authorization: Bearer YOUR_JWT_TOKEN
```

#### Response (200 OK)
```json
{
  "id": 1,
  "currency": "TND",
  "subtotal": 1598.000,
  "discountTotal": 0.000,
  "deliveryFee": 0.000,
  "total": 1598.000,
  "items": [
    {
      "id": 1,
      "articleId": 1,
      "articleTitle": "Smartphone Pro X",
      "articlePrice": 799.000,
      "quantity": 2,
      "subtotal": 1598.000,
      "articleSlug": "smartphone-pro-x",
      "articleImage": "/uploads/articles/phone.jpg"
    }
  ],
  "updatedAt": "2026-02-09T14:30:00Z"
}
```

#### TypeScript Example
```typescript
interface CartItem {
  id: number;
  articleId: number;
  articleTitle: string;
  articlePrice: number;
  quantity: number;
  subtotal: number;
  articleSlug: string;
  articleImage?: string;
}

interface Cart {
  id: number;
  currency: string;
  subtotal: number;
  discountTotal: number;
  deliveryFee: number;
  total: number;
  items: CartItem[];
  updatedAt: string;
}

async function getCart(token: string): Promise<Cart> {
  const response = await fetch('http://localhost:5273/api/cart', {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  return await response.json();
}
```

---

### 4.2 Add Item to Cart

**Endpoint:** `POST /api/cart/items`  
**Access:** Client or Admin  
**Description:** Add article to cart

#### Request Body
```json
{
  "articleId": 1,
  "variantId": null,
  "quantity": 2
}
```

#### Response (200 OK)
```json
{
  "id": 1,
  "currency": "TND",
  "subtotal": 1598.000,
  "total": 1598.000,
  "items": [
    {
      "id": 1,
      "articleId": 1,
      "quantity": 2,
      "subtotal": 1598.000
    }
  ]
}
```

#### Error Response (400 Bad Request)
```json
{
  "title": "Requête invalide",
  "status": 400,
  "detail": "Article introuvable.",
  "instance": "/api/cart/items"
}
```

#### TypeScript Example
```typescript
async function addToCart(articleId: number, quantity: number, token: string) {
  const response = await fetch('http://localhost:5273/api/cart/items', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ articleId, quantity })
  });
  
  if (!response.ok) throw new Error('Failed to add to cart');
  return await response.json();
}
```

---

### 4.3 Update Cart Item Quantity

**Endpoint:** `PUT /api/cart/items/{itemId}`  
**Access:** Client or Admin  
**Description:** Update quantity of existing cart item

#### Request Body
```json
{
  "quantity": 3
}
```

#### Response (200 OK)
```json
{
  "id": 1,
  "currency": "TND",
  "subtotal": 2397.000,
  "total": 2397.000,
  "items": [...]
}
```

---

### 4.4 Remove Item from Cart

**Endpoint:** `DELETE /api/cart/items/{itemId}`  
**Access:** Client or Admin  
**Description:** Remove item from cart

#### Response (200 OK)
```json
{
  "id": 1,
  "currency": "TND",
  "subtotal": 0.000,
  "total": 0.000,
  "items": []
}
```

---

### 4.5 Clear Cart

**Endpoint:** `POST /api/cart/clear`  
**Access:** Client or Admin  
**Description:** Remove all items from cart

#### Response (200 OK)
```json
{
  "id": 1,
  "currency": "TND",
  "subtotal": 0.000,
  "total": 0.000,
  "items": [],
  "updatedAt": "2026-02-09T14:35:00Z"
}
```

---

## 5. Addresses Endpoints

### 5.1 Get User Addresses

**Endpoint:** `GET /api/addresses`  
**Access:** Client or Admin  
**Description:** Get all addresses for current user

#### Response (200 OK)
```json
[
  {
    "id": 1,
    "label": "Home",
    "fullName": "John Doe",
    "phone": "12345678",
    "addressLine1": "123 Main Street",
    "addressLine2": "Apt 4B",
    "city": "Tunis",
    "state": "Tunis",
    "postalCode": "1000",
    "country": "Tunisia",
    "isDefault": true,
    "createdAt": "2026-02-09T10:00:00Z"
  }
]
```

#### TypeScript Example
```typescript
interface Address {
  id: number;
  label: string;
  fullName: string;
  phone: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state?: string;
  postalCode: string;
  country: string;
  isDefault: boolean;
  createdAt: string;
}

async function getAddresses(token: string): Promise<Address[]> {
  const response = await fetch('http://localhost:5273/api/addresses', {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  return await response.json();
}
```

---

### 5.2 Get Address by ID

**Endpoint:** `GET /api/addresses/{id}`  
**Access:** Client or Admin  
**Description:** Get single address details

#### Response (200 OK)
```json
{
  "id": 1,
  "label": "Home",
  "fullName": "John Doe",
  "phone": "12345678",
  "addressLine1": "123 Main Street",
  "city": "Tunis",
  "postalCode": "1000",
  "country": "Tunisia",
  "isDefault": true
}
```

---

### 5.3 Create Address

**Endpoint:** `POST /api/addresses`  
**Access:** Client or Admin  
**Description:** Create new address for current user

#### Request Body
```json
{
  "label": "Home",
  "fullName": "John Doe",
  "phone": "12345678",
  "addressLine1": "123 Main Street",
  "addressLine2": "Apt 4B",
  "city": "Tunis",
  "state": "Tunis",
  "postalCode": "1000",
  "country": "Tunisia"
}
```

#### Response (201 Created)
```json
{
  "id": 1,
  "label": "Home",
  "fullName": "John Doe",
  "phone": "12345678",
  "addressLine1": "123 Main Street",
  "addressLine2": "Apt 4B",
  "city": "Tunis",
  "state": "Tunis",
  "postalCode": "1000",
  "country": "Tunisia",
  "isDefault": false,
  "createdAt": "2026-02-09T14:40:00Z"
}
```

#### TypeScript Example
```typescript
interface CreateAddressRequest {
  label: string;
  fullName: string;
  phone: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state?: string;
  postalCode: string;
  country: string;
}

async function createAddress(data: CreateAddressRequest, token: string) {
  const response = await fetch('http://localhost:5273/api/addresses', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(data)
  });
  
  return await response.json();
}
```

---

### 5.4 Update Address

**Endpoint:** `PUT /api/addresses/{id}`  
**Access:** Client or Admin  
**Description:** Update existing address

#### Request Body
```json
{
  "label": "Work",
  "fullName": "John Doe",
  "phone": "87654321",
  "addressLine1": "456 Business Ave",
  "city": "Sfax",
  "postalCode": "3000",
  "country": "Tunisia"
}
```

#### Response (204 No Content)

---

### 5.5 Set Default Address

**Endpoint:** `PATCH /api/addresses/{id}/default`  
**Access:** Client or Admin  
**Description:** Set address as default (unsets previous default)

#### Response (204 No Content)

---

### 5.6 Delete Address

**Endpoint:** `DELETE /api/addresses/{id}`  
**Access:** Client or Admin  
**Description:** Soft delete address

#### Response (204 No Content)

---

## 6. Orders Endpoints

### 6.1 Create Order

**Endpoint:** `POST /api/orders`  
**Access:** Client or Admin  
**Description:** Create order from current cart

#### Request Body
```json
{
  "addressId": 1,
  "paymentMethod": "cash",
  "notes": "Please deliver in the morning"
}
```

#### Response (201 Created)
```json
{
  "id": 1,
  "orderNumber": "ORD-20260209-0001",
  "status": "Pending",
  "paymentMethod": "cash",
  "subtotal": 1598.000,
  "deliveryFee": 0.000,
  "total": 1598.000,
  "currency": "TND",
  "notes": "Please deliver in the morning",
  "createdAt": "2026-02-09T15:00:00Z",
  "address": {
    "fullName": "John Doe",
    "phone": "12345678",
    "addressLine1": "123 Main Street",
    "city": "Tunis",
    "country": "Tunisia"
  },
  "items": [
    {
      "articleId": 1,
      "articleTitle": "Smartphone Pro X",
      "quantity": 2,
      "unitPrice": 799.000,
      "subtotal": 1598.000
    }
  ]
}
```

#### TypeScript Example
```typescript
interface CreateOrderRequest {
  addressId: number;
  paymentMethod: 'cash' | 'card' | 'online';
  notes?: string;
}

interface Order {
  id: number;
  orderNumber: string;
  status: string;
  paymentMethod: string;
  subtotal: number;
  deliveryFee: number;
  total: number;
  currency: string;
  notes?: string;
  createdAt: string;
}

async function createOrder(data: CreateOrderRequest, token: string): Promise<Order> {
  const response = await fetch('http://localhost:5273/api/orders', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(data)
  });
  
  if (!response.ok) throw new Error('Failed to create order');
  return await response.json();
}
```

---

### 6.2 Get User Orders

**Endpoint:** `GET /api/orders/me`  
**Access:** Client or Admin  
**Description:** Get all orders for current user

#### Query Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | integer | Page number (default: 1) |
| `pageSize` | integer | Items per page (default: 20) |

#### Response (200 OK)
```json
{
  "items": [
    {
      "id": 1,
      "orderNumber": "ORD-20260209-0001",
      "status": "Pending",
      "total": 1598.000,
      "currency": "TND",
      "createdAt": "2026-02-09T15:00:00Z",
      "itemCount": 1
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}
```

---

### 6.3 Get Order Details

**Endpoint:** `GET /api/orders/me/{id}`  
**Access:** Client or Admin  
**Description:** Get detailed order information

#### Response (200 OK)
```json
{
  "id": 1,
  "orderNumber": "ORD-20260209-0001",
  "status": "Pending",
  "paymentMethod": "cash",
  "paymentStatus": "Pending",
  "subtotal": 1598.000,
  "deliveryFee": 0.000,
  "total": 1598.000,
  "currency": "TND",
  "notes": "Please deliver in the morning",
  "createdAt": "2026-02-09T15:00:00Z",
  "address": {
    "fullName": "John Doe",
    "phone": "12345678",
    "addressLine1": "123 Main Street",
    "city": "Tunis",
    "postalCode": "1000",
    "country": "Tunisia"
  },
  "items": [
    {
      "id": 1,
      "articleId": 1,
      "articleTitle": "Smartphone Pro X",
      "articleSlug": "smartphone-pro-x",
      "quantity": 2,
      "unitPrice": 799.000,
      "subtotal": 1598.000
    }
  ],
  "statusHistory": [
    {
      "status": "Pending",
      "timestamp": "2026-02-09T15:00:00Z",
      "notes": "Order created"
    }
  ]
}
```

---

### 6.4 Cancel Order

**Endpoint:** `PATCH /api/orders/me/{id}/cancel`  
**Access:** Client or Admin  
**Description:** Cancel order (only if status allows)

#### Response (200 OK)
```json
{
  "id": 1,
  "status": "Cancelled",
  "cancelledAt": "2026-02-09T15:30:00Z"
}
```

---

### 6.5 Get All Orders (Admin)

**Endpoint:** `GET /api/orders`  
**Access:** Admin only  
**Description:** Get all orders (admin view)

#### Query Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | integer | Page number |
| `pageSize` | integer | Items per page |
| `status` | string | Filter by status |
| `search` | string | Search by order number/user |

---

### 6.6 Get Order by ID (Admin)

**Endpoint:** `GET /api/orders/{id}`  
**Access:** Admin only

---

### 6.7 Update Order Status (Admin)

**Endpoint:** `PUT /api/orders/{id}/status`  
**Access:** Admin only  
**Description:** Update order status

#### Request Body
```json
{
  "status": "Processing",
  "notes": "Order is being prepared"
}
```

#### Response (200 OK)
```json
{
  "id": 1,
  "status": "Processing",
  "updatedAt": "2026-02-09T16:00:00Z"
}
```

---

### 6.8 Update Delivery Fees (Admin)

**Endpoint:** `PUT /api/orders/{id}/fees`  
**Access:** Admin only

#### Request Body
```json
{
  "deliveryFee": 7.50
}
```

#### Response (200 OK)
```json
{
  "id": 1,
  "deliveryFee": 7.50,
  "total": 1605.50
}
```

---

## 7. Payments Endpoints

### 7.1 Initialize Payment

**Endpoint:** `POST /api/payments/init/{orderId}`  
**Access:** Client or Admin  
**Description:** Initialize payment for order

#### URL Parameters
- `orderId` (integer): Order ID to pay

#### Request Body
```json
{
  "provider": "stripe",
  "returnUrl": "http://localhost:4200/payment-success",
  "cancelUrl": "http://localhost:4200/payment-cancel"
}
```

#### Response (200 OK)
```json
{
  "transactionId": "txn_12345",
  "provider": "stripe",
  "amount": 1598.000,
  "currency": "TND",
  "status": "Pending",
  "redirectUrl": "https://checkout.stripe.com/pay/..."
}
```

---

### 7.2 Payment Callback

**Endpoint:** `GET /api/payments/callback/{provider}`  
**Access:** Public  
**Description:** Handle payment provider callback

#### Query Parameters
- Payment provider specific parameters

---

### 7.3 Payment Webhook

**Endpoint:** `POST /api/payments/webhook/{provider}`  
**Access:** Public (verified by signature)  
**Description:** Handle payment provider webhook

---

## 8. Reclamations Endpoints

### 8.1 Create Reclamation (Public)

**Endpoint:** `POST /api/reclamations`  
**Access:** Public  
**Description:** Create new reclamation/complaint

#### Request Body
```json
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "phone": "12345678",
  "subject": "Product Quality Issue",
  "message": "The product arrived damaged and not working properly."
}
```

#### Response (201 Created)
```json
{
  "id": 1,
  "status": "New",
  "createdAt": "2026-02-09T16:30:00Z"
}
```

#### TypeScript Example
```typescript
interface CreateReclamationRequest {
  fullName: string;
  email: string;
  phone: string;
  subject: string;
  message: string;
}

async function createReclamation(data: CreateReclamationRequest) {
  const response = await fetch('http://localhost:5273/api/reclamations', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data)
  });
  
  return await response.json();
}
```

---

### 8.2 Get User Reclamations

**Endpoint:** `GET /api/reclamations/me`  
**Access:** Client or Admin  
**Description:** Get all reclamations for current user

#### Response (200 OK)
```json
[
  {
    "id": 1,
    "subject": "Product Quality Issue",
    "status": "New",
    "createdAt": "2026-02-09T16:30:00Z",
    "updatedAt": "2026-02-09T16:30:00Z"
  }
]
```

---

### 8.3 Get Reclamation Details (User)

**Endpoint:** `GET /api/reclamations/me/{id}`  
**Access:** Client or Admin  
**Description:** Get reclamation details for current user

#### Response (200 OK)
```json
{
  "id": 1,
  "fullName": "John Doe",
  "email": "john@example.com",
  "phone": "12345678",
  "subject": "Product Quality Issue",
  "message": "The product arrived damaged and not working properly.",
  "status": "New",
  "response": null,
  "createdAt": "2026-02-09T16:30:00Z",
  "updatedAt": "2026-02-09T16:30:00Z"
}
```

---

### 8.4 Get All Reclamations (Admin)

**Endpoint:** `GET /api/reclamations`  
**Access:** Admin only  
**Description:** Get all reclamations

#### Query Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | integer | Page number |
| `pageSize` | integer | Items per page |
| `status` | string | Filter by status |

---

### 8.5 Get Reclamation by ID (Admin)

**Endpoint:** `GET /api/reclamations/{id}`  
**Access:** Admin only

---

### 8.6 Update Reclamation (Admin)

**Endpoint:** `PUT /api/reclamations/{id}`  
**Access:** Admin only  
**Description:** Update reclamation status and response

#### Request Body
```json
{
  "status": "Resolved",
  "response": "We have processed your refund and sent a replacement product."
}
```

#### Response (200 OK)
```json
{
  "id": 1,
  "status": "Resolved",
  "response": "We have processed your refund and sent a replacement product.",
  "updatedAt": "2026-02-09T17:00:00Z"
}
```

---

### 8.7 Delete Reclamation (Admin)

**Endpoint:** `DELETE /api/reclamations/{id}`  
**Access:** Admin only

#### Response (204 No Content)

---

## 9. Stock Endpoints

### 9.1 Adjust Stock

**Endpoint:** `POST /api/stock/adjust`  
**Access:** Admin only  
**Description:** Increase or decrease stock quantity

#### Request Body
```json
{
  "articleId": 1,
  "variantId": null,
  "delta": 50,
  "note": "Initial stock"
}
```

**Notes:**
- `delta`: Positive = increase, Negative = decrease
- `delta` cannot be 0

#### Response (204 No Content)

#### TypeScript Example
```typescript
async function adjustStock(
  articleId: number, 
  delta: number, 
  note: string, 
  token: string
) {
  const response = await fetch('http://localhost:5273/api/stock/adjust', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ articleId, delta, note })
  });
  
  if (!response.ok) throw new Error('Failed to adjust stock');
}
```

---

### 9.2 Get Stock Items

**Endpoint:** `GET /api/stock/items`  
**Access:** Admin only  
**Description:** Get stock levels for all articles

#### Response (200 OK)
```json
[
  {
    "articleId": 1,
    "variantId": null,
    "onHand": 50.000,
    "reserved": 0.000,
    "available": 50.000
  },
  {
    "articleId": 2,
    "variantId": null,
    "onHand": 30.000,
    "reserved": 5.000,
    "available": 25.000
  }
]
```

#### TypeScript Example
```typescript
interface StockItem {
  articleId: number;
  variantId?: number;
  onHand: number;
  reserved: number;
  available: number;
}

async function getStockItems(token: string): Promise<StockItem[]> {
  const response = await fetch('http://localhost:5273/api/stock/items', {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  return await response.json();
}
```

---

### 9.3 Get Stock Movements

**Endpoint:** `GET /api/stock/movements`  
**Access:** Admin only  
**Description:** Get stock movement history

#### Query Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| `articleId` | integer | Filter by article |
| `fromDate` | datetime | Start date |
| `toDate` | datetime | End date |

#### Response (200 OK)
```json
[
  {
    "id": 1,
    "articleId": 1,
    "type": "Adjustment",
    "quantityChange": 50.000,
    "note": "Initial stock",
    "createdAt": "2026-02-09T14:05:00Z"
  }
]
```

---

### 9.4 Get Stock Reservations

**Endpoint:** `GET /api/stock/reservations`  
**Access:** Admin only  
**Description:** Get active stock reservations

#### Response (200 OK)
```json
[
  {
    "id": 1,
    "articleId": 1,
    "orderId": 1,
    "quantity": 2.000,
    "expiresAt": "2026-02-09T18:00:00Z",
    "createdAt": "2026-02-09T15:00:00Z"
  }
]
```

---

### 9.5 Expire Reservations

**Endpoint:** `POST /api/stock/expire-reservations`  
**Access:** Admin only  
**Description:** Manually expire old reservations

#### Response (200 OK)
```json
{
  "expiredCount": 5
}
```

---

## 10. Error Handling

### Standard Error Response Format

All API errors follow RFC 7807 Problem Details format:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Error Title",
  "status": 400,
  "detail": "Detailed error message",
  "instance": "/api/endpoint",
  "traceId": "0HNJ7S4674263:00000001",
  "errors": {
    "fieldName": ["Field-specific error message"]
  }
}
```

### HTTP Status Codes

| Code | Meaning | Description |
|------|---------|-------------|
| 200 | OK | Request succeeded |
| 201 | Created | Resource created successfully |
| 204 | No Content | Success with no response body |
| 400 | Bad Request | Invalid request data |
| 401 | Unauthorized | Missing or invalid token |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource not found |
| 500 | Internal Server Error | Server error |

### Common Error Scenarios

#### 1. Authentication Error (401)
```json
{
  "title": "Non autorisé",
  "status": 401,
  "detail": "Token invalide ou expiré.",
  "instance": "/api/cart"
}
```

**Solution:** Re-authenticate via `/api/auth/login`

---

#### 2. Authorization Error (403)
```json
{
  "title": "Accès refusé",
  "status": 403,
  "detail": "Vous n'avez pas les permissions nécessaires.",
  "instance": "/api/categories"
}
```

**Solution:** Use admin account or contact administrator

---

#### 3. Validation Error (400)
```json
{
  "title": "Requête invalide",
  "status": 400,
  "errors": {
    "Email": ["The Email field is required."],
    "Password": ["The Password field must be at least 6 characters."]
  }
}
```

**Solution:** Fix validation errors in request body

---

#### 4. Not Found Error (404)
```json
{
  "title": "Introuvable",
  "status": 404,
  "detail": "Article introuvable.",
  "instance": "/api/articles/999"
}
```

---

## 11. TypeScript SDK Example

Here's a complete TypeScript SDK example for consuming the API:

```typescript
// api-client.ts
class HlouwaApiClient {
  private baseUrl: string;
  private token: string | null = null;

  constructor(baseUrl: string = 'http://localhost:5273/api') {
    this.baseUrl = baseUrl;
    this.token = localStorage.getItem('token');
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      ...options.headers,
    };

    if (this.token && !options.headers?.['Authorization']) {
      headers['Authorization'] = `Bearer ${this.token}`;
    }

    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      ...options,
      headers,
    });

    if (response.status === 204) {
      return undefined as T;
    }

    const data = await response.json();

    if (!response.ok) {
      throw new ApiError(data, response.status);
    }

    return data;
  }

  // Auth
  async login(email: string, password: string) {
    const data = await this.request<AuthResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
      headers: { 'Authorization': '' }, // Don't send token for login
    });
    
    this.token = data.token;
    localStorage.setItem('token', data.token);
    return data;
  }

  async register(userData: RegisterRequest) {
    const data = await this.request<AuthResponse>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(userData),
      headers: { 'Authorization': '' },
    });
    
    this.token = data.token;
    localStorage.setItem('token', data.token);
    return data;
  }

  logout() {
    this.token = null;
    localStorage.removeItem('token');
  }

  // Categories
  async getCategories() {
    return this.request<Category[]>('/categories');
  }

  async getCategoryBySlug(slug: string) {
    return this.request<Category>(`/categories/by-slug/${slug}`);
  }

  // Articles
  async getArticles(params: ArticleQueryParams = {}) {
    const queryString = new URLSearchParams(
      Object.entries(params)
        .filter(([_, v]) => v !== undefined)
        .map(([k, v]) => [k, String(v)])
    ).toString();
    
    return this.request<PagedResult<ArticleListItem>>(
      `/articles${queryString ? '?' + queryString : ''}`
    );
  }

  async getArticle(id: number) {
    return this.request<Article>(`/articles/${id}`);
  }

  async getArticleBySlug(slug: string) {
    return this.request<Article>(`/articles/by-slug/${slug}`);
  }

  // Cart
  async getCart() {
    return this.request<Cart>('/cart');
  }

  async addToCart(articleId: number, quantity: number) {
    return this.request<Cart>('/cart/items', {
      method: 'POST',
      body: JSON.stringify({ articleId, quantity }),
    });
  }

  async updateCartItem(itemId: number, quantity: number) {
    return this.request<Cart>(`/cart/items/${itemId}`, {
      method: 'PUT',
      body: JSON.stringify({ quantity }),
    });
  }

  async removeFromCart(itemId: number) {
    return this.request<Cart>(`/cart/items/${itemId}`, {
      method: 'DELETE',
    });
  }

  async clearCart() {
    return this.request<Cart>('/cart/clear', {
      method: 'POST',
    });
  }

  // Addresses
  async getAddresses() {
    return this.request<Address[]>('/addresses');
  }

  async createAddress(address: CreateAddressRequest) {
    return this.request<Address>('/addresses', {
      method: 'POST',
      body: JSON.stringify(address),
    });
  }

  async updateAddress(id: number, address: CreateAddressRequest) {
    return this.request<void>(`/addresses/${id}`, {
      method: 'PUT',
      body: JSON.stringify(address),
    });
  }

  async setDefaultAddress(id: number) {
    return this.request<void>(`/addresses/${id}/default`, {
      method: 'PATCH',
    });
  }

  async deleteAddress(id: number) {
    return this.request<void>(`/addresses/${id}`, {
      method: 'DELETE',
    });
  }

  // Orders
  async createOrder(orderData: CreateOrderRequest) {
    return this.request<Order>('/orders', {
      method: 'POST',
      body: JSON.stringify(orderData),
    });
  }

  async getMyOrders(page = 1, pageSize = 20) {
    return this.request<PagedResult<OrderListItem>>(
      `/orders/me?page=${page}&pageSize=${pageSize}`
    );
  }

  async getOrder(id: number) {
    return this.request<OrderDetail>(`/orders/me/${id}`);
  }

  async cancelOrder(id: number) {
    return this.request<Order>(`/orders/me/${id}/cancel`, {
      method: 'PATCH',
    });
  }

  // Reclamations
  async createReclamation(data: CreateReclamationRequest) {
    return this.request<{ id: number; status: string }>('/reclamations', {
      method: 'POST',
      body: JSON.stringify(data),
      headers: { 'Authorization': '' }, // Public endpoint
    });
  }

  async getMyReclamations() {
    return this.request<Reclamation[]>('/reclamations/me');
  }
}

// Error handling
class ApiError extends Error {
  constructor(
    public problemDetails: any,
    public statusCode: number
  ) {
    super(problemDetails.detail || 'An error occurred');
    this.name = 'ApiError';
  }
}

// Types
interface AuthResponse {
  token: string;
  expiresAt: string;
  userId: string;
  email: string;
  fullName: string;
  roles: string[];
}

interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  phoneNumber: string;
}

interface ArticleQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: number;
  featured?: boolean;
  minPrice?: number;
  maxPrice?: number;
  sort?: 'newest' | 'priceAsc' | 'priceDesc';
}

// Export
export default HlouwaApiClient;
```

### Usage Example

```typescript
// In your Angular/React/Vue component
import HlouwaApiClient from './api-client';

const api = new HlouwaApiClient();

// Login
async function handleLogin() {
  try {
    const result = await api.login('user@example.com', 'password123');
    console.log('Logged in:', result.fullName);
  } catch (error) {
    if (error instanceof ApiError) {
      console.error('Login failed:', error.problemDetails.detail);
    }
  }
}

// Get products
async function loadProducts() {
  const products = await api.getArticles({
    page: 1,
    pageSize: 12,
    categoryId: 1,
    featured: true
  });
  
  console.log(`Found ${products.totalCount} products`);
  return products.items;
}

// Add to cart
async function addProductToCart(articleId: number) {
  try {
    const cart = await api.addToCart(articleId, 1);
    console.log('Cart updated:', cart.total);
  } catch (error) {
    console.error('Failed to add to cart:', error);
  }
}

// Create order
async function checkout(addressId: number) {
  const order = await api.createOrder({
    addressId,
    paymentMethod: 'cash',
    notes: 'Ring the doorbell'
  });
  
  console.log('Order created:', order.orderNumber);
  return order;
}
```

---

## 12. Postman Collection

Import this Postman collection to test all endpoints:

### Collection Structure
```
Hlouwa API
├── Auth
│   ├── Register
│   ├── Login
│   └── Get Profile
├── Categories
│   ├── Get All
│   ├── Get by Slug
│   ├── Create (Admin)
│   └── Update (Admin)
├── Articles
│   ├── Get All
│   ├── Get by ID
│   ├── Search
│   └── Create (Admin)
├── Cart
│   ├── Get Cart
│   ├── Add Item
│   ├── Update Item
│   └── Clear Cart
├── Addresses
│   └── Full CRUD
├── Orders
│   └── Full workflow
└── Admin Operations
    ├── Stock Management
    ├── Order Management
    └── Reclamations
```

---

## 13. Best Practices

### 1. Token Management
```typescript
// Store token securely
function storeToken(token: string) {
  localStorage.setItem('token', token);
  // Or use secure httpOnly cookie for production
}

// Auto-refresh on 401
async function fetchWithAuth(url: string, options: RequestInit = {}) {
  let response = await fetch(url, {
    ...options,
    headers: {
      ...options.headers,
      'Authorization': `Bearer ${getToken()}`
    }
  });
  
  if (response.status === 401) {
    // Token expired, re-login
    await reLogin();
    response = await fetch(url, options);
  }
  
  return response;
}
```

### 2. Error Handling
```typescript
try {
  const data = await api.getArticles();
} catch (error) {
  if (error instanceof ApiError) {
    switch (error.statusCode) {
      case 401:
        // Redirect to login
        break;
      case 403:
        // Show permission error
        break;
      case 404:
        // Show not found
        break;
      default:
        // Show generic error
        break;
    }
  }
}
```

### 3. Pagination
```typescript
function usePagination<T>(
  fetchFn: (page: number, pageSize: number) => Promise<PagedResult<T>>
) {
  const [page, setPage] = useState(1);
  const [data, setData] = useState<T[]>([]);
  const [totalPages, setTotalPages] = useState(0);
  
  useEffect(() => {
    fetchFn(page, 20).then(result => {
      setData(result.items);
      setTotalPages(Math.ceil(result.totalCount / result.pageSize));
    });
  }, [page]);
  
  return { data, page, totalPages, setPage };
}
```

### 4. Caching
```typescript
// Simple cache for categories (rarely change)
let categoriesCache: Category[] | null = null;

async function getCategoriesWithCache() {
  if (categoriesCache) return categoriesCache;
  
  categoriesCache = await api.getCategories();
  return categoriesCache;
}

// Invalidate cache when needed
function invalidateCache() {
  categoriesCache = null;
}
```

---

## 14. Testing Credentials

### Admin Account
```
Email: admin@hlouwa.tn
Password: Admin123!
Role: Admin
```

### Client Account
```
Email: client@hlouwa.tn
Password: Client123!
Role: Client
```

---

## 15. Support & Contact

**API Maintainer:** Development Team  
**Last Updated:** February 9, 2026  
**API Version:** 1.0  
**Documentation Version:** 1.0

For questions or issues, please contact the backend team.

---

**End of Documentation**
