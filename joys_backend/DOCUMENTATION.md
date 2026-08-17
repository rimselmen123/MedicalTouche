# 📘 Documentation Complète - Hlouwa E-Commerce

## 🎯 But du Projet

**Hlouwa** est une plateforme e-commerce tunisienne spécialisée dans la vente de **pâtisseries artisanales**. Le système offre une solution complète de gestion de boutique en ligne avec :

- **Gestion de catalogue produits** (articles, variantes, images, catégories)
- **Système de commandes** avec paiement en ligne (Online) ou à la livraison (CashOnDelivery)
- **Intégration de paiements tunisiens** (Konnect, Paymee, Flouci, ClicToPaySMT)
- **Gestion avancée de stock** avec réservation automatique pour paiements en ligne
- **Système de réclamations** pour le service après-vente
- **Panier d'achat** persistant par utilisateur
- **Gestion d'adresses de livraison** multiples
- **Authentification JWT** avec rôles (Admin/Client)

**Devise**: TND (Tunisian Dinar) avec précision de 3 décimales  
**Architecture**: ASP.NET Core 8.0 + Entity Framework Core + SQL Server  
**Frontend**: Angular (fichiers statiques servis depuis wwwroot/)

---

## 👥 Types d'Utilisateurs

### 1. **Admin** (`Role: Admin`)
- Gestion complète du catalogue
- Gestion des commandes et statuts
- Gestion du stock (ajustements, historique, réservations)
- Consultation des paiements et transactions
- Gestion des réclamations

### 2. **Client** (`Role: User/Client`)
- Navigation catalogue
- Gestion de panier
- Passage de commande
- Suivi de commandes
- Gestion d'adresses
- Création de réclamations

---

## 🔐 Authentification

### **POST** `/api/auth/register`
**Rôle**: Public  
**Description**: Inscription nouveau client

**Input**:
```json
{
  "email": "client@example.com",
  "password": "Password123!",
  "confirmPassword": "Password123!",
  "firstName": "Ahmed",
  "lastName": "Ben Salem",
  "phoneNumber": "+21612345678"
}
```

**Output** (200 OK):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiration": "2026-02-09T03:00:00Z",
  "user": {
    "id": "uuid-string",
    "email": "client@example.com",
    "firstName": "Ahmed",
    "lastName": "Ben Salem",
    "roles": ["User"]
  }
}
```

**Erreurs**:
- 400: Validation échouée (email déjà utilisé, mot de passe faible)

---

### **POST** `/api/auth/login`
**Rôle**: Public  
**Description**: Connexion utilisateur

**Input**:
```json
{
  "email": "client@example.com",
  "password": "Password123!"
}
```

**Output** (200 OK):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiration": "2026-02-09T03:00:00Z",
  "user": {
    "id": "uuid-string",
    "email": "client@example.com",
    "firstName": "Ahmed",
    "lastName": "Ben Salem",
    "roles": ["User"]
  }
}
```

**Erreurs**:
- 401: Email ou mot de passe incorrect

---

## 📦 Catalogue Produits

### **GET** `/api/articles`
**Rôle**: Public  
**Description**: Liste tous les articles actifs avec pagination

**Query Parameters**:
- `categoryId` (int, optional): Filtrer par catégorie
- `search` (string, optional): Recherche dans titre/description
- `pageNumber` (int, default=1): Numéro de page
- `pageSize` (int, default=20): Taille de page

**Output** (200 OK):
```json
{
  "items": [
    {
      "id": 1,
      "title": "Baklawa Premium",
      "slug": "baklawa-premium",
      "description": "Baklawa aux pistaches et miel",
      "price": 25.500,
      "oldPrice": 30.000,
      "currency": "TND",
      "sku": "BAK-001",
      "categoryId": 2,
      "categoryName": "Pâtisseries Orientales",
      "coverImage": "/uploads/articles/baklawa.jpg",
      "images": [
        {
          "url": "/uploads/articles/baklawa.jpg",
          "altText": "Baklawa",
          "isCover": true
        }
      ],
      "variants": [
        {
          "id": 5,
          "name": "500g",
          "sku": "BAK-001-500G",
          "price": 25.500
        },
        {
          "id": 6,
          "name": "1kg",
          "sku": "BAK-001-1KG",
          "price": 48.000
        }
      ],
      "isActive": true,
      "createdAt": "2026-01-15T10:00:00Z"
    }
  ],
  "totalCount": 45,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

---

### **GET** `/api/articles/{id}`
**Rôle**: Public  
**Description**: Détails d'un article

**Output** (200 OK):
```json
{
  "id": 1,
  "title": "Baklawa Premium",
  "slug": "baklawa-premium",
  "description": "Baklawa aux pistaches et miel...",
  "price": 25.500,
  "oldPrice": 30.000,
  "currency": "TND",
  "sku": "BAK-001",
  "categoryId": 2,
  "categoryName": "Pâtisseries Orientales",
  "images": [...],
  "variants": [...],
  "isActive": true,
  "createdAt": "2026-01-15T10:00:00Z"
}
```

**Erreurs**:
- 404: Article non trouvé

---

### **POST** `/api/articles` 🔒 Admin
**Rôle**: Admin  
**Description**: Créer un nouvel article

**Input**:
```json
{
  "title": "Makroud aux Dattes",
  "description": "Makroud traditionnel fourré aux dattes",
  "price": 18.500,
  "oldPrice": null,
  "sku": "MAK-001",
  "categoryId": 2,
  "isActive": true
}
```

**Output** (201 Created):
```json
{
  "id": 15,
  "title": "Makroud aux Dattes",
  "slug": "makroud-aux-dattes",
  "price": 18.500,
  "createdAt": "2026-02-08T14:30:00Z"
}
```

---

### **PUT** `/api/articles/{id}` 🔒 Admin
**Rôle**: Admin  
**Description**: Modifier un article

**Input**: (même structure que POST)

**Output** (204 No Content)

**Erreurs**:
- 404: Article non trouvé
- 409: Slug déjà utilisé

---

### **DELETE** `/api/articles/{id}` 🔒 Admin
**Rôle**: Admin  
**Description**: Supprimer (soft delete) un article

**Output** (204 No Content)

---

## 🗂️ Catégories

### **GET** `/api/categories`
**Rôle**: Public  
**Description**: Liste toutes les catégories

**Output** (200 OK):
```json
[
  {
    "id": 1,
    "name": "Gâteaux",
    "slug": "gateaux",
    "description": "Gâteaux de fête et anniversaire",
    "articlesCount": 12
  },
  {
    "id": 2,
    "name": "Pâtisseries Orientales",
    "slug": "patisseries-orientales",
    "description": "Baklawa, Makroud, Zlabia...",
    "articlesCount": 25
  }
]
```

---

### **POST** `/api/categories` 🔒 Admin
**Rôle**: Admin  
**Description**: Créer une catégorie

**Input**:
```json
{
  "name": "Pâtisseries Françaises",
  "description": "Croissants, Éclairs, Macarons..."
}
```

**Output** (201 Created):
```json
{
  "id": 5,
  "name": "Pâtisseries Françaises",
  "slug": "patisseries-francaises"
}
```

---

## 🛒 Panier (Cart)

### **GET** `/api/cart` 🔒 Client
**Rôle**: Authentifié  
**Description**: Récupérer le panier de l'utilisateur

**Output** (200 OK):
```json
{
  "id": 10,
  "userId": "uuid-string",
  "items": [
    {
      "id": 25,
      "articleId": 1,
      "articleVariantId": 5,
      "name": "Baklawa Premium",
      "variantName": "500g",
      "imageUrl": "/uploads/articles/baklawa.jpg",
      "unitPrice": 25.500,
      "quantity": 2
    }
  ],
  "subtotal": 51.000,
  "discountTotal": 0.000,
  "deliveryFee": 0.000,
  "total": 51.000,
  "currency": "TND",
  "createdAt": "2026-02-07T10:00:00Z",
  "updatedAt": "2026-02-08T11:30:00Z"
}
```

---

### **POST** `/api/cart/items` 🔒 Client
**Rôle**: Authentifié  
**Description**: Ajouter un article au panier

**Input**:
```json
{
  "articleId": 1,
  "articleVariantId": 5,
  "quantity": 2
}
```

**Output** (204 No Content)

**Erreurs**:
- 400: Article non trouvé ou inactif
- 400: Quantité invalide

---

### **PUT** `/api/cart/items/{itemId}` 🔒 Client
**Rôle**: Authentifié  
**Description**: Modifier quantité d'un item

**Input**:
```json
{
  "quantity": 3
}
```

**Output** (204 No Content)

---

### **DELETE** `/api/cart/items/{itemId}` 🔒 Client
**Rôle**: Authentifié  
**Description**: Retirer un article du panier

**Output** (204 No Content)

---

### **DELETE** `/api/cart/clear` 🔒 Client
**Rôle**: Authentifié  
**Description**: Vider complètement le panier

**Output** (204 No Content)

---

## 📍 Adresses de Livraison

### **GET** `/api/addresses` 🔒 Client
**Rôle**: Authentifié  
**Description**: Liste des adresses de l'utilisateur

**Output** (200 OK):
```json
[
  {
    "id": 3,
    "label": "Domicile",
    "fullName": "Ahmed Ben Salem",
    "phone": "+21612345678",
    "line1": "Appartement 5, Résidence Jasmin",
    "line2": "Rue de la République",
    "city": "Tunis",
    "postalCode": "1000",
    "governorate": "Tunis",
    "countryCode": "TN",
    "isDefault": true
  }
]
```

---

### **POST** `/api/addresses` 🔒 Client
**Rôle**: Authentifié  
**Description**: Créer une adresse

**Input**:
```json
{
  "label": "Bureau",
  "fullName": "Ahmed Ben Salem",
  "phone": "+21612345678",
  "line1": "Immeuble City Center, 5ème étage",
  "line2": "Avenue Habib Bourguiba",
  "city": "Tunis",
  "postalCode": "1000",
  "governorate": "Tunis",
  "isDefault": false
}
```

**Output** (201 Created):
```json
{
  "id": 8,
  "label": "Bureau",
  "isDefault": false
}
```

---

## 🛍️ Commandes (Orders)

### **POST** `/api/orders` 🔒 Client
**Rôle**: Authentifié  
**Description**: Créer une nouvelle commande

**Input**:
```json
{
  "items": [
    {
      "articleId": 1,
      "variantId": 5,
      "quantity": 2
    },
    {
      "articleId": 3,
      "variantId": null,
      "quantity": 1
    }
  ],
  "paymentMethod": "Online",
  "provider": "Konnect",
  "shippingAddress": {
    "fullName": "Ahmed Ben Salem",
    "phone": "+21612345678",
    "line1": "Appartement 5, Résidence Jasmin",
    "line2": "Rue de la République",
    "city": "Tunis",
    "postalCode": "1000",
    "governorate": "Tunis"
  },
  "customerNote": "Livraison après 18h SVP",
  "requestedDeliveryDate": "2026-02-10",
  "deliveryTimeSlot": "18:00-20:00"
}
```

**PaymentMethod**: `Online` | `CashOnDelivery`  
**Provider** (si Online): `Konnect` | `Paymee` | `Flouci` | `ClicToPaySMT`

**Output** (200 OK):
```json
{
  "id": 125,
  "orderNumber": "HLW-20260208-143025-A5F3",
  "status": "AwaitingPayment",
  "paymentMethod": "Online",
  "paymentStatus": "Pending",
  "total": 76.500,
  "currency": "TND",
  "createdAt": "2026-02-08T14:30:25Z"
}
```

**Erreurs**:
- 400: Panier vide, adresse invalide, provider manquant
- 409: Stock insuffisant (pour commandes Online)

**Notes**:
- Pour `PaymentMethod.Online`: Stock est **automatiquement réservé** pendant 15 minutes
- Pour `CashOnDelivery`: Pas de réservation stock (traitement admin manuel)

---

### **GET** `/api/orders/me` 🔒 Client
**Rôle**: Authentifié  
**Description**: Liste des commandes du client

**Output** (200 OK):
```json
[
  {
    "id": 125,
    "orderNumber": "HLW-20260208-143025-A5F3",
    "status": "Paid",
    "paymentMethod": "Online",
    "paymentStatus": "Paid",
    "total": 76.500,
    "currency": "TND",
    "createdAt": "2026-02-08T14:30:25Z"
  }
]
```

**Status possibles**:
- `Pending`: En attente (cash on delivery)
- `AwaitingPayment`: En attente de paiement (online)
- `Paid`: Payée
- `Processing`: En traitement
- `Shipped`: Expédiée
- `Delivered`: Livrée
- `Canceled`: Annulée
- `Refunded`: Remboursée

---

### **GET** `/api/orders/me/{orderNumber}` 🔒 Client
**Rôle**: Authentifié  
**Description**: Détails d'une commande

**Output** (200 OK):
```json
{
  "id": 125,
  "orderNumber": "HLW-20260208-143025-A5F3",
  "status": "Paid",
  "paymentMethod": "Online",
  "paymentStatus": "Paid",
  "paymentProvider": "Konnect",
  "subtotal": 76.500,
  "deliveryFee": 0.000,
  "discountTotal": 0.000,
  "total": 76.500,
  "currency": "TND",
  "shippingAddress": {
    "fullName": "Ahmed Ben Salem",
    "phone": "+21612345678",
    "line1": "Appartement 5, Résidence Jasmin",
    "line2": "Rue de la République",
    "city": "Tunis",
    "postalCode": "1000",
    "governorate": "Tunis",
    "countryCode": "TN"
  },
  "customerNote": "Livraison après 18h SVP",
  "adminNote": null,
  "createdAt": "2026-02-08T14:30:25Z",
  "items": [
    {
      "id": 320,
      "articleId": 1,
      "articleVariantId": 5,
      "name": "Baklawa Premium",
      "variantName": "500g",
      "sku": "BAK-001-500G",
      "imageUrl": "/uploads/articles/baklawa.jpg",
      "unitPrice": 25.500,
      "quantity": 2
    }
  ]
}
```

---

### **GET** `/api/orders` 🔒 Admin
**Rôle**: Admin  
**Description**: Liste TOUTES les commandes avec filtres

**Query Parameters**:
- `status` (OrderStatus, optional): Filtrer par statut
- `paymentStatus` (PaymentStatus, optional): Filtrer par statut paiement
- `pageNumber` (int, default=1)
- `pageSize` (int, default=50)

**Output** (200 OK):
```json
{
  "items": [
    {
      "id": 125,
      "orderNumber": "HLW-20260208-143025-A5F3",
      "userId": "uuid-string",
      "userEmail": "client@example.com",
      "status": "Paid",
      "paymentMethod": "Online",
      "paymentStatus": "Paid",
      "total": 76.500,
      "currency": "TND",
      "createdAt": "2026-02-08T14:30:25Z"
    }
  ],
  "totalCount": 253,
  "pageNumber": 1,
  "pageSize": 50,
  "totalPages": 6
}
```

---

### **GET** `/api/orders/{id}` 🔒 Admin
**Rôle**: Admin  
**Description**: Détails complets d'une commande (même structure que client mais avec plus d'infos)

**Output** (200 OK): (même structure que GET `/api/orders/me/{orderNumber}`)

---

### **PUT** `/api/orders/{id}/status` 🔒 Admin
**Rôle**: Admin  
**Description**: Changer le statut d'une commande

**Input**:
```json
{
  "newStatus": "Processing",
  "adminNote": "Commande en préparation"
}
```

**Output** (204 No Content)

**Erreurs**:
- 400: Transition de statut invalide

---

## 💳 Paiements (Payments)

### **POST** `/api/payments/{orderId}/init` 🔒 Client
**Rôle**: Authentifié  
**Description**: Initialiser un paiement en ligne

**Input** (optionnel):
```json
{
  "provider": "Konnect"
}
```

**Output** (200 OK):
```json
{
  "orderId": 125,
  "orderNumber": "HLW-20260208-143025-A5F3",
  "amount": 76.500,
  "currency": "TND",
  "provider": "Konnect",
  "checkoutUrl": "https://konnect.network/gateway/payment?token=xxx"
}
```

**Notes**:
- Rediriger le client vers `checkoutUrl`
- Webhook sera appelé automatiquement par le provider
- Callback URL: `/api/payments/{orderId}/callback`

**Erreurs**:
- 404: Commande non trouvée
- 400: Commande déjà payée ou méthode non-online

---

### **GET** `/api/payments/{orderId}/callback` 🔒 Client
**Rôle**: Authentifié  
**Description**: Callback après paiement (redirection provider)

**Query Parameters**: (dépend du provider)

**Output**: Redirection vers Angular frontend avec statut

---

### **POST** `/api/payments/webhook/{provider}`
**Rôle**: Public (webhook provider)  
**Description**: Webhook appelé par le provider pour confirmer paiement

**Notes**:
- Signature vérifiée côté serveur
- Gestion automatique des statuts:
  - **Paid** → Commande passée à `Paid`, stock **committed** (déduction finale)
  - **Failed** → Commande reste `AwaitingPayment`, stock **released** (libéré)
  - **Canceled** → Commande reste `AwaitingPayment`, stock **released**

---

### **GET** `/api/payments/{orderId}/transactions` 🔒 Admin
**Rôle**: Admin  
**Description**: Historique des tentatives de paiement

**Output** (200 OK):
```json
[
  {
    "id": 45,
    "orderId": 125,
    "provider": "Konnect",
    "status": "Succeeded",
    "amount": 76.500,
    "currency": "TND",
    "providerPaymentId": "pay_xxx123",
    "providerTransactionId": "txn_yyy456",
    "checkoutUrl": "https://konnect.network/...",
    "paidAt": "2026-02-08T14:35:00Z",
    "createdAt": "2026-02-08T14:30:25Z"
  }
]
```

**Status possibles**:
- `Pending`: En attente
- `Succeeded`: Réussi
- `Failed`: Échoué
- `Canceled`: Annulé
- `Expired`: Expiré

---

## 📦 Gestion de Stock (Inventory Pro)

### **GET** `/api/stock/items` 🔒 Admin
**Rôle**: Admin  
**Description**: Liste de tous les stocks

**Output** (200 OK):
```json
[
  {
    "articleId": 1,
    "articleVariantId": 5,
    "onHand": 150.000,
    "reserved": 10.000,
    "available": 140.000
  },
  {
    "articleId": 1,
    "articleVariantId": 6,
    "onHand": 200.000,
    "reserved": 0.000,
    "available": 200.000
  }
]
```

**Formule**: `Available = OnHand - Reserved`

---

### **POST** `/api/stock/adjust` 🔒 Admin
**Rôle**: Admin  
**Description**: Ajuster le stock manuellement (entrée/sortie)

**Input**:
```json
{
  "articleId": 1,
  "variantId": 5,
  "delta": 50.000,
  "note": "Réception fournisseur - Lot #2024-02-08"
}
```

**Delta**:
- Positif (+50): Entrée de stock
- Négatif (-20): Sortie de stock

**Output** (204 No Content)

**Erreurs**:
- 400: Delta = 0
- 400: OnHand deviendrait négatif

**Notes**:
- Crée automatiquement un `StockMovement` type `Adjust`
- Si article/variant n'existe pas dans stock, création automatique avec OnHand = delta

---

### **GET** `/api/stock/movements` 🔒 Admin
**Rôle**: Admin  
**Description**: Historique des mouvements de stock

**Query Parameters**:
- `articleId` (int, optional): Filtrer par article
- `variantId` (int, optional): Filtrer par variante
- `limit` (int, default=100, max=1000): Nombre de résultats

**Output** (200 OK):
```json
[
  {
    "id": 152,
    "articleId": 1,
    "articleVariantId": 5,
    "type": "Adjust",
    "quantity": 50.000,
    "refType": "Admin",
    "refId": "admin-user-id",
    "note": "Réception fournisseur - Lot #2024-02-08",
    "actorUserId": "admin-user-id",
    "createdAt": "2026-02-08T14:00:00Z"
  },
  {
    "id": 153,
    "articleId": 1,
    "articleVariantId": 5,
    "type": "Reserve",
    "quantity": 2.000,
    "refType": "Order",
    "refId": "HLW-20260208-143025-A5F3",
    "note": "Reserve until 2026-02-08T14:45:25Z",
    "actorUserId": "client-user-id",
    "createdAt": "2026-02-08T14:30:25Z"
  },
  {
    "id": 154,
    "articleId": 1,
    "articleVariantId": 5,
    "type": "Out",
    "quantity": 2.000,
    "refType": "Order",
    "refId": "HLW-20260208-143025-A5F3",
    "note": "Commit after payment success",
    "actorUserId": "SYSTEM",
    "createdAt": "2026-02-08T14:35:00Z"
  }
]
```

**Types de mouvements**:
- `In`: Entrée de stock
- `Out`: Sortie de stock (vente confirmée)
- `Reserve`: Réservation (paiement en ligne en attente)
- `Release`: Libération de réservation (paiement échoué/annulé)
- `Adjust`: Ajustement manuel admin (IN ou OUT)

---

### **GET** `/api/stock/reservations` 🔒 Admin
**Rôle**: Admin  
**Description**: Liste des réservations de stock

**Query Parameters**:
- `status` (StockReservationStatus, optional): `Active` | `Committed` | `Released` | `Expired`

**Output** (200 OK):
```json
[
  {
    "id": 25,
    "orderId": 125,
    "orderNumber": "HLW-20260208-143025-A5F3",
    "status": "Committed",
    "expiresAt": "2026-02-08T14:45:25Z",
    "reason": "Online checkout reserve",
    "createdAt": "2026-02-08T14:30:25Z",
    "items": [
      {
        "articleId": 1,
        "articleVariantId": 5,
        "name": "Baklawa Premium",
        "variantName": "500g",
        "quantity": 2.000
      }
    ]
  }
]
```

**Status lifecycle**:
1. `Active`: Réservation active (stock bloqué)
2. `Committed`: Confirmée (stock déduit définitivement)
3. `Released`: Libérée (paiement échoué/annulé, stock redevient disponible)
4. `Expired`: Expirée (TTL dépassé, auto-libérée par système)

---

### **POST** `/api/stock/expire-reservations` 🔒 Admin
**Rôle**: Admin  
**Description**: Expirer manuellement les réservations périmées

**Output** (200 OK):
```json
{
  "expired": 5
}
```

**Notes**:
- Traite toutes les réservations avec `status=Active` et `expiresAt <= now`
- Libère automatiquement le stock réservé
- Crée des `StockMovement` type `Release` avec actorUserId = "SYSTEM"

---

## 🔧 Flux Automatique de Stock

### **1. Commande Online créée**
```
Client → POST /api/orders (PaymentMethod.Online)
  ↓
OrdersController.ReserveStockForOrderAsync()
  ↓
Création StockReservation (Status=Active, ExpiresAt=+15min)
  ↓
For each OrderItem:
  - Vérifier disponibilité: Available = OnHand - Reserved >= Quantity
  - Si insuffisant → Exception → 409 Conflict
  - Sinon: StockItem.Reserved += Quantity
  - Créer StockReservationItem (snapshot)
  - Créer StockMovement (Type=Reserve)
  ↓
Transaction DB commitée
  ↓
Response 200 OK (ou 409 si stock insuffisant)
```

---

### **2. Paiement réussi (Webhook)**
```
Provider → POST /api/payments/webhook/{provider}
  ↓
PaymentsController: Parse & verify signature
  ↓
Si IsPaid = true:
  - Transaction.Status = Succeeded
  - Order.Status = Paid
  - Order.PaymentStatus = Paid
  - PaymentsController.CommitStockForOrderAsync()
    ↓
    For each StockReservationItem:
      - StockItem.Reserved -= Quantity
      - StockItem.OnHand -= Quantity (déduction finale)
      - Créer StockMovement (Type=Out)
    - StockReservation.Status = Committed
  ↓
SaveChanges + Transaction commit
```

---

### **3. Paiement échoué/annulé (Webhook)**
```
Provider → POST /api/payments/webhook/{provider}
  ↓
Si IsFailed ou IsCanceled = true:
  - Transaction.Status = Failed/Canceled
  - Order.Status = AwaitingPayment (reste modifiable)
  - Order.PaymentStatus = Failed/Canceled
  - PaymentsController.ReleaseStockForOrderAsync()
    ↓
    For each StockReservationItem:
      - StockItem.Reserved -= Quantity (libération)
      - Créer StockMovement (Type=Release)
    - StockReservation.Status = Released
  ↓
SaveChanges + Transaction commit
```

---

### **4. Expiration automatique (Cron/Admin)**
```
Admin → POST /api/stock/expire-reservations
  ↓
For each StockReservation (Status=Active, ExpiresAt <= now):
  - StockController.ReleaseForOrderAsync(actorUserId="SYSTEM")
    - Libère Reserved
    - Change status → Expired
    - Crée StockMovements
  ↓
Response: { "expired": count }
```

---

## 📞 Réclamations (Reclamations)

### **POST** `/api/reclamations` 🔒 Client
**Rôle**: Authentifié  
**Description**: Créer une réclamation

**Input**:
```json
{
  "orderId": 125,
  "subject": "Produit endommagé",
  "message": "La boîte de baklawa est arrivée ouverte, certaines pièces sont cassées."
}
```

**Output** (201 Created):
```json
{
  "id": 12,
  "orderId": 125,
  "subject": "Produit endommagé",
  "status": "Open",
  "createdAt": "2026-02-08T15:00:00Z"
}
```

---

### **GET** `/api/reclamations/me` 🔒 Client
**Rôle**: Authentifié  
**Description**: Mes réclamations

**Output** (200 OK):
```json
[
  {
    "id": 12,
    "orderId": 125,
    "orderNumber": "HLW-20260208-143025-A5F3",
    "subject": "Produit endommagé",
    "message": "La boîte de baklawa...",
    "status": "Open",
    "adminResponse": null,
    "createdAt": "2026-02-08T15:00:00Z",
    "resolvedAt": null
  }
]
```

**Status possibles**:
- `Open`: Ouverte
- `InProgress`: En cours de traitement
- `Resolved`: Résolue
- `Closed`: Fermée

---

### **GET** `/api/reclamations` 🔒 Admin
**Rôle**: Admin  
**Description**: Toutes les réclamations

**Query Parameters**:
- `status` (ReclamationStatus, optional)
- `pageNumber`, `pageSize`

**Output** (200 OK): (liste avec pagination)

---

### **GET** `/api/reclamations/{id}` 🔒 Admin
**Rôle**: Authentifié  
**Description**: Détails d'une réclamation

**Output** (200 OK):
```json
{
  "id": 12,
  "userId": "uuid-string",
  "userEmail": "client@example.com",
  "orderId": 125,
  "orderNumber": "HLW-20260208-143025-A5F3",
  "subject": "Produit endommagé",
  "message": "La boîte de baklawa...",
  "status": "InProgress",
  "adminResponse": "Nous vous envoyons un remplacement",
  "createdAt": "2026-02-08T15:00:00Z",
  "resolvedAt": null
}
```

---

### **PUT** `/api/reclamations/{id}` 🔒 Admin
**Rôle**: Admin  
**Description**: Répondre/modifier une réclamation

**Input**:
```json
{
  "status": "Resolved",
  "adminResponse": "Votre commande de remplacement sera livrée demain. Référence: #REF-2026-001"
}
```

**Output** (204 No Content)

---

## 📊 Résumé des Endpoints

| Catégorie | Admin | Client | Public | Total |
|-----------|-------|--------|--------|-------|
| Auth | - | - | 2 | 2 |
| Articles | 4 | - | 2 | 6 |
| Categories | 3 | - | 1 | 4 |
| Cart | - | 5 | - | 5 |
| Addresses | - | 4 | - | 4 |
| Orders | 3 | 3 | - | 6 |
| Payments | 1 | 2 | 1 | 4 |
| Stock | 4 | - | - | 4 |
| Reclamations | 3 | 2 | - | 5 |
| **TOTAL** | **18** | **16** | **6** | **40** |

---

## 🔒 Sécurité

### **Authentification**
- JWT Bearer Token avec 6 heures d'expiration
- Algorithme HMAC-SHA256
- Claims: `uid`, `email`, `role`

### **Autorisation**
- Rôles: `Admin`, `User`
- Attributs: `[Authorize]`, `[Authorize(Roles="Admin")]`

### **Validation**
- ModelState validation automatique
- ProblemDetails RFC 7807 pour erreurs structurées

### **Audit Trail**
- AuditableEntity sur toutes les entités
- CreatedAt, UpdatedAt, IsDeleted, DeletedAt, RowVersion

### **Soft Delete**
- QueryFilters globaux `!IsDeleted`
- Données jamais physiquement supprimées

---

## 🗄️ Base de Données

**Tables principales** (17 au total):
- AspNetUsers, AspNetRoles (Identity)
- Articles, ArticleImages, ArticleVariants
- Categories
- Orders, OrderItems, OrderStatusHistory
- PaymentTransactions
- Carts, CartItems
- StockItems, StockReservations, StockReservationItems, StockMovements
- UserAddresses
- Reclamations

**Relations clés**:
- Article → Category (Many-to-One, SetNull on delete)
- Article → Images/Variants (One-to-Many, Cascade)
- Order → Items/Payments/StatusHistory (One-to-Many, Cascade)
- Order → User (Many-to-One, Restrict)
- StockReservation → Order (One-to-One via unique OrderId)
- StockReservation → ReservationItems (One-to-Many, Cascade)

---

## 🚀 Déploiement

**Configuration** (`appsettings.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=102.211.210.158;Database=HlouwaDB;..."
  },
  "JwtSettings": {
    "SecretKey": "YOUR-SECRET-KEY-32-CHARS-MINIMUM",
    "ExpirationHours": 6,
    "Issuer": "Hlouwa.API",
    "Audience": "Hlouwa.Client"
  },
  "PaymentProviders": {
    "Konnect": {
      "ApiKey": "xxx",
      "WebhookSecret": "yyy"
    }
  }
}
```

**Commandes**:
```bash
# Build
dotnet build --configuration Release

# Migrations (production)
dotnet ef database update

# Publish
dotnet publish --configuration Release --output ./publish

# Run
dotnet Hlouwa.dll
```

**URLs**:
- API: `https://localhost:7001`
- Swagger: `https://localhost:7001/swagger`
- Frontend: `https://localhost:7001/` (Angular SPA)

---

## 📝 Notes Importantes

### **Gestion de Stock**
- **Réservation automatique** uniquement pour `PaymentMethod.Online`
- **CashOnDelivery**: Pas de réservation (gestion manuelle par admin)
- **TTL**: 15 minutes par défaut (configurable)
- **Atomicité garantie**: BeginTransaction/Commit sur toutes opérations stock

### **Paiements**
- **Webhooks**: Signature vérifiée côté serveur
- **Idempotence**: Safe retry sur webhooks (status checking)
- **Providers**: Extensible via interface IPaymentProviderClient

### **Performances**
- Indexes sur: Slugs, OrderNumber, ProviderPaymentId, Stock (ArticleId+VariantId)
- AsNoTracking() sur requêtes lecture seule
- Pagination sur toutes listes

### **Frontend Angular**
- Fichiers statiques dans `wwwroot/`
- SPA fallback sur toutes routes non-API
- Build production: `ng build --configuration production`

---

## 📞 Support

**Email**: support@hlouwa.tn  
**Documentation API**: https://localhost:7001/swagger  
**Version**: 1.0.0 (Sprint 2 - Inventory Pro)  
**Date**: Février 2026
