# 🛒 Documentation API - Gestion des Commandes

> Documentation complète des endpoints API pour la gestion des commandes, du client et de l'administration.

**Base URL:** `/api/orders`

**Date:** 11 février 2026

---

## 🔐 Authentification

Les endpoints sont divisés en deux catégories :
- **Endpoints Client** : Nécessitent une authentification utilisateur (rôle: User ou Admin)
- **Endpoints Admin** : Nécessitent une authentification avec le rôle **Admin**

**Header requis:**
```
Authorization: Bearer <token>
```

---

## 📋 Table des matières

### Endpoints Client
1. [Créer une Commande](#1-créer-une-commande)
2. [Mes Commandes](#2-mes-commandes)
3. [Détails d'une Commande](#3-détails-dune-commande)
4. [Annuler une Commande](#4-annuler-une-commande)

### Endpoints Admin
5. [Liste des Commandes (Admin)](#5-liste-des-commandes-admin)
6. [Détails d'une Commande (Admin)](#6-détails-dune-commande-admin)
7. [Mettre à jour le Statut](#7-mettre-à-jour-le-statut)
8. [Mettre à jour les Frais](#8-mettre-à-jour-les-frais)

### Référence
9. [Types et Énumérations](#9-types-et-énumérations)
10. [Modèles de Données](#10-modèles-de-données)
11. [Gestion des Erreurs](#11-gestion-des-erreurs)
12. [Flux de Commande](#12-flux-de-commande)

---

## 🛍️ ENDPOINTS CLIENT

## 1. Créer une Commande

### `POST /api/orders`

Permet à un client authentifié de créer une nouvelle commande.

#### 🔒 Autorisation
- Rôle requis: **User** ou **Admin** (authentifié)

#### 📥 Request Body

```json
{
  "paymentMethod": 0,
  "provider": null,
  "customerNote": "Merci de livrer avant 18h",
  "shippingAddress": {
    "fullName": "Mohamed Ben Ali",
    "phone": "+216 20 123 456",
    "line1": "15 Avenue Habib Bourguiba",
    "line2": "Appartement 3, 2ème étage",
    "city": "Tunis",
    "postalCode": "1000",
    "governorate": "Tunis"
  },
  "requestedDeliveryDate": "2026-02-15T00:00:00Z",
  "deliveryTimeSlot": "14h00-18h00",
  "items": [
    {
      "articleId": 12,
      "variantId": 5,
      "quantity": 2
    },
    {
      "articleId": 15,
      "variantId": null,
      "quantity": 1
    }
  ]
}
```

**Paramètres:**

| Champ | Type | Requis | Description |
|-------|------|--------|-------------|
| `paymentMethod` | `integer` | ✅ | 0 = Paiement à la livraison, 1 = Paiement en ligne (voir [PaymentMethod](#paymentmethod)) |
| `provider` | `integer` | ⚠️ | Fournisseur de paiement (requis si `paymentMethod=1`) - voir [PaymentProvider](#paymentprovider) |
| `customerNote` | `string` | ❌ | Note du client (max: 500 caractères) |
| `shippingAddress` | `object` | ✅ | Adresse de livraison complète |
| `requestedDeliveryDate` | `datetime` | ❌ | Date de livraison souhaitée (ISO 8601 UTC) |
| `deliveryTimeSlot` | `string` | ❌ | Créneau horaire souhaité (max: 40 caractères) |
| `items` | `array` | ✅ | Liste des articles (minimum 1) |

**Structure de `shippingAddress`:**

| Champ | Type | Requis | Longueur max | Description |
|-------|------|--------|--------------|-------------|
| `fullName` | `string` | ✅ | 160 | Nom complet du destinataire |
| `phone` | `string` | ✅ | 30 | Numéro de téléphone |
| `line1` | `string` | ✅ | 180 | Adresse ligne 1 |
| `line2` | `string` | ❌ | 180 | Adresse ligne 2 (complément) |
| `city` | `string` | ✅ | 80 | Ville |
| `postalCode` | `string` | ❌ | 20 | Code postal |
| `governorate` | `string` | ❌ | 80 | Gouvernorat |

**Structure des `items`:**

| Champ | Type | Requis | Description |
|-------|------|--------|-------------|
| `articleId` | `integer` | ✅ | ID de l'article |
| `variantId` | `integer` | ❌ | ID de la variante (null si article sans variante) |
| `quantity` | `integer` | ✅ | Quantité (min: 1, max: 999) |

#### 📤 Réponse

**Succès:** `200 OK`

```json
{
  "id": 156,
  "orderNumber": "HLW-20260211-143052-A3F2",
  "status": 0,
  "paymentMethod": 0,
  "paymentStatus": 0,
  "total": 125.500,
  "currency": "TND",
  "createdAt": "2026-02-11T14:30:52Z"
}
```

**Structure de la réponse:**

| Champ | Type | Description |
|-------|------|-------------|
| `id` | `integer` | ID de la commande créée |
| `orderNumber` | `string` | Numéro unique de commande (format: HLW-YYYYMMDD-HHMMSS-XXXX) |
| `status` | `integer` | Statut de la commande (voir [OrderStatus](#orderstatus)) |
| `paymentMethod` | `integer` | Méthode de paiement choisie |
| `paymentStatus` | `integer` | Statut du paiement (voir [PaymentStatus](#paymentstatus)) |
| `total` | `decimal` | Montant total de la commande (TND) |
| `currency` | `string` | Devise (TND) |
| `createdAt` | `datetime` | Date de création (ISO 8601 UTC) |

**Erreurs:**

```json
// Commande vide
{
  "status": 400,
  "title": "Requête invalide",
  "detail": "Commande vide.",
  "instance": "/api/orders",
  "traceId": "00-abc123..."
}

// Article indisponible
{
  "status": 400,
  "title": "Requête invalide",
  "detail": "Un ou plusieurs produits sont indisponibles.",
  "instance": "/api/orders",
  "traceId": "00-abc123..."
}

// Stock insuffisant (paiement en ligne)
{
  "status": 409,
  "title": "Stock insuffisant",
  "detail": "La commande a été créée mais le stock est insuffisant. Annulation en cours.",
  "instance": "/api/orders",
  "traceId": "00-abc123..."
}

// Provider manquant pour paiement en ligne
{
  "status": 400,
  "title": "Requête invalide",
  "detail": "PaymentProvider requis pour paiement en ligne.",
  "instance": "/api/orders",
  "traceId": "00-abc123..."
}
```

#### 💡 Exemples

**Commande avec paiement à la livraison:**
```bash
curl -X POST https://api.example.com/api/orders \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "paymentMethod": 0,
    "provider": null,
    "shippingAddress": {
      "fullName": "Ahmed Trabelsi",
      "phone": "+216 98 765 432",
      "line1": "32 Rue de la République",
      "line2": null,
      "city": "Sfax",
      "postalCode": "3000",
      "governorate": "Sfax"
    },
    "items": [
      {
        "articleId": 10,
        "variantId": null,
        "quantity": 3
      }
    ]
  }'
```

**Commande avec paiement en ligne:**
```bash
curl -X POST https://api.example.com/api/orders \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "paymentMethod": 1,
    "provider": 10,
    "customerNote": "Urgent - cadeau danniversaire",
    "shippingAddress": {
      "fullName": "Salma Kacem",
      "phone": "+216 22 555 777",
      "line1": "12 Avenue Habib Bourguiba",
      "city": "Sousse",
      "postalCode": "4000",
      "governorate": "Sousse"
    },
    "requestedDeliveryDate": "2026-02-14T00:00:00Z",
    "deliveryTimeSlot": "10h00-12h00",
    "items": [
      {
        "articleId": 25,
        "variantId": 8,
        "quantity": 1
      }
    ]
  }'
```

#### 📌 Notes importantes

1. **Réservation de stock automatique** : Pour les commandes avec paiement en ligne (`paymentMethod=1`), le système réserve automatiquement le stock pendant 15 minutes.
2. **Validation des articles** : Le système vérifie que tous les articles existent et sont actifs avant de créer la commande.
3. **Calcul automatique** : Les prix unitaires, le sous-total et le total sont calculés automatiquement depuis la base de données.
4. **Numéro de commande unique** : Généré automatiquement au format `HLW-YYYYMMDD-HHMMSS-XXXX`.

---

## 2. Mes Commandes

### `GET /api/orders/me`

Récupère la liste de toutes les commandes du client authentifié.

#### 🔒 Autorisation
- Rôle requis: **User** ou **Admin** (authentifié)

#### 📥 Paramètres

Aucun paramètre requis.

#### 📤 Réponse

**Succès:** `200 OK`

```json
[
  {
    "id": 156,
    "orderNumber": "HLW-20260211-143052-A3F2",
    "status": 0,
    "paymentMethod": 0,
    "paymentStatus": 0,
    "total": 125.500,
    "currency": "TND",
    "createdAt": "2026-02-11T14:30:52Z"
  },
  {
    "id": 142,
    "orderNumber": "HLW-20260205-091520-B7D9",
    "status": 6,
    "paymentMethod": 0,
    "paymentStatus": 2,
    "total": 89.900,
    "currency": "TND",
    "createdAt": "2026-02-05T09:15:20Z"
  }
]
```

**Structure de la réponse:** (même structure que [OrderSummaryDto](#ordersummarydto))

Les commandes sont triées par date de création décroissante (plus récente en premier).

#### 💡 Exemple

```bash
curl -X GET https://api.example.com/api/orders/me \
  -H "Authorization: Bearer <token>"
```

---

## 3. Détails d'une Commande

### `GET /api/orders/me/{id}`

Récupère les détails complets d'une commande spécifique du client authentifié.

#### 🔒 Autorisation
- Rôle requis: **User** ou **Admin** (authentifié)
- Le client ne peut voir que **ses propres commandes**

#### 📥 Paramètres de route

| Paramètre | Type | Description |
|-----------|------|-------------|
| `id` | `integer` | ID de la commande |

#### 📤 Réponse

**Succès:** `200 OK`

```json
{
  "id": 156,
  "orderNumber": "HLW-20260211-143052-A3F2",
  "status": 0,
  "paymentMethod": 0,
  "paymentStatus": 0,
  "provider": 0,
  "subtotal": 115.500,
  "deliveryFee": 10.000,
  "discountTotal": 0.000,
  "total": 125.500,
  "currency": "TND",
  "shippingAddress": {
    "fullName": "Ahmed Trabelsi",
    "phone": "+216 98 765 432",
    "line1": "32 Rue de la République",
    "line2": null,
    "city": "Sfax",
    "postalCode": "3000",
    "governorate": "Sfax",
    "countryCode": "TN"
  },
  "customerNote": "Merci de livrer avant 18h",
  "adminNote": null,
  "createdAt": "2026-02-11T14:30:52Z",
  "items": [
    {
      "id": 523,
      "articleId": 12,
      "variantId": 5,
      "name": "T-shirt Premium",
      "variantName": "Rouge - M",
      "sku": "TSH-RED-M",
      "imageUrl": "/uploads/articles/a12_abc123.jpg",
      "unitPrice": 45.000,
      "quantity": 2,
      "lineTotal": 90.000
    },
    {
      "id": 524,
      "articleId": 15,
      "variantId": null,
      "name": "Casquette Classic",
      "variantName": null,
      "sku": "CAP-CLS",
      "imageUrl": "/uploads/articles/a15_def456.jpg",
      "unitPrice": 25.500,
      "quantity": 1,
      "lineTotal": 25.500
    }
  ]
}
```

**Structure de la réponse:** (voir [OrderDetailsDto](#orderdetailsdto))

**Erreurs:**

```json
// Commande introuvable ou n'appartient pas au client
{
  "status": 404,
  "title": "Introuvable",
  "detail": "Commande introuvable.",
  "instance": "/api/orders/me/156",
  "traceId": "00-abc123..."
}
```

#### 💡 Exemple

```bash
curl -X GET https://api.example.com/api/orders/me/156 \
  -H "Authorization: Bearer <token>"
```

---

## 4. Annuler une Commande

### `PATCH /api/orders/me/{id}/cancel`

Permet au client d'annuler sa propre commande (sous certaines conditions).

#### 🔒 Autorisation
- Rôle requis: **User** ou **Admin** (authentifié)
- Le client ne peut annuler que **ses propres commandes**

#### 📥 Paramètres de route

| Paramètre | Type | Description |
|-----------|------|-------------|
| `id` | `integer` | ID de la commande à annuler |

#### 📥 Request Body

Aucun body requis.

#### 📤 Réponse

**Succès:** `204 No Content`

**Erreurs:**

```json
// Commande déjà en préparation
{
  "status": 400,
  "title": "Annulation impossible",
  "detail": "Commande déjà en traitement, annulation impossible.",
  "instance": "/api/orders/me/156/cancel",
  "traceId": "00-abc123..."
}

// Commande déjà payée
{
  "status": 400,
  "title": "Annulation impossible",
  "detail": "Commande payée. Utilise le process de remboursement.",
  "instance": "/api/orders/me/156/cancel",
  "traceId": "00-abc123..."
}

// Commande introuvable
{
  "status": 404,
  "title": "Introuvable",
  "detail": "Commande introuvable.",
  "instance": "/api/orders/me/156/cancel",
  "traceId": "00-abc123..."
}
```

#### 💡 Exemple

```bash
curl -X PATCH https://api.example.com/api/orders/me/156/cancel \
  -H "Authorization: Bearer <token>"
```

#### 📌 Conditions d'annulation

Une commande **peut être annulée** si :
- ✅ Statut = `Pending` (0) ou `AwaitingPayment` (1)
- ✅ PaymentStatus ≠ `Paid` (2)

Une commande **ne peut PAS être annulée** si :
- ❌ Statut = `Preparing` (3), `OutForDelivery` (5) ou `Delivered` (6)
- ❌ PaymentStatus = `Paid` (2) → nécessite un remboursement

---

## 👨‍💼 ENDPOINTS ADMIN

## 5. Liste des Commandes (Admin)

### `GET /api/orders`

Récupère une liste paginée de toutes les commandes avec filtres avancés (Admin uniquement).

#### 🔒 Autorisation
- Rôle requis: **Admin**

#### 📥 Paramètres de requête

| Paramètre | Type | Requis | Défaut | Description |
|-----------|------|--------|--------|-------------|
| `page` | `integer` | ❌ | `1` | Numéro de page |
| `pageSize` | `integer` | ❌ | `20` | Nombre d'éléments par page (min: 1, max: 100) |
| `status` | `integer` | ❌ | - | Filtrer par statut de commande |
| `paymentStatus` | `integer` | ❌ | - | Filtrer par statut de paiement |
| `paymentMethod` | `integer` | ❌ | - | Filtrer par méthode de paiement |
| `search` | `string` | ❌ | - | Recherche dans orderNumber, email, nom, téléphone client |
| `from` | `datetime` | ❌ | - | Date de début (ISO 8601 UTC) |
| `to` | `datetime` | ❌ | - | Date de fin (ISO 8601 UTC) |

#### 📤 Réponse

**Succès:** `200 OK`

```json
{
  "items": [
    {
      "id": 156,
      "orderNumber": "HLW-20260211-143052-A3F2",
      "status": 0,
      "paymentMethod": 0,
      "paymentStatus": 0,
      "total": 125.500,
      "currency": "TND",
      "createdAt": "2026-02-11T14:30:52Z",
      "itemsCount": 2,
      "customerEmail": "ahmed@example.com",
      "customerFullName": "Ahmed Trabelsi",
      "customerPhone": "+216 98 765 432"
    },
    {
      "id": 155,
      "orderNumber": "HLW-20260210-165432-C8B3",
      "status": 3,
      "paymentMethod": 1,
      "paymentStatus": 2,
      "total": 245.000,
      "currency": "TND",
      "createdAt": "2026-02-10T16:54:32Z",
      "itemsCount": 4,
      "customerEmail": "salma@example.com",
      "customerFullName": "Salma Kacem",
      "customerPhone": "+216 22 555 777"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 347
}
```

**Structure de la réponse:**

| Champ | Type | Description |
|-------|------|-------------|
| `items` | `array` | Liste des commandes (voir [OrderAdminListDto](#orderadminlistdto)) |
| `page` | `integer` | Numéro de page actuelle |
| `pageSize` | `integer` | Nombre d'éléments par page |
| `totalCount` | `integer` | Nombre total de commandes correspondant aux filtres |

#### 💡 Exemples

**Toutes les commandes (page 1):**
```bash
curl -X GET "https://api.example.com/api/orders?page=1&pageSize=20" \
  -H "Authorization: Bearer <admin-token>"
```

**Commandes en attente de paiement:**
```bash
curl -X GET "https://api.example.com/api/orders?status=1&paymentStatus=0" \
  -H "Authorization: Bearer <admin-token>"
```

**Recherche par numéro de commande:**
```bash
curl -X GET "https://api.example.com/api/orders?search=HLW-20260211" \
  -H "Authorization: Bearer <admin-token>"
```

**Commandes d'une période:**
```bash
curl -X GET "https://api.example.com/api/orders?from=2026-02-01T00:00:00Z&to=2026-02-15T23:59:59Z" \
  -H "Authorization: Bearer <admin-token>"
```

**Commandes en ligne payées:**
```bash
curl -X GET "https://api.example.com/api/orders?paymentMethod=1&paymentStatus=2" \
  -H "Authorization: Bearer <admin-token>"
```

---

## 6. Détails d'une Commande (Admin)

### `GET /api/orders/{id}`

Récupère les détails complets d'une commande spécifique (Admin uniquement).

#### 🔒 Autorisation
- Rôle requis: **Admin**

#### 📥 Paramètres de route

| Paramètre | Type | Description |
|-----------|------|-------------|
| `id` | `integer` | ID de la commande |

#### 📤 Réponse

**Succès:** `200 OK`

La structure de réponse est identique à [GET /api/orders/me/{id}](#3-détails-dune-commande), incluant tous les détails de la commande.

**Différence avec l'endpoint client:**
- L'admin peut voir **toutes** les commandes (pas de restriction par userId)
- Inclut potentiellement le champ `adminNote`

**Erreurs:**

```json
// Commande introuvable
{
  "status": 404,
  "title": "Introuvable",
  "detail": "Commande introuvable.",
  "instance": "/api/orders/156",
  "traceId": "00-abc123..."
}
```

#### 💡 Exemple

```bash
curl -X GET https://api.example.com/api/orders/156 \
  -H "Authorization: Bearer <admin-token>"
```

---

## 7. Mettre à jour le Statut

### `PUT /api/orders/{id}/status`

Permet à l'admin de modifier le statut d'une commande avec traçabilité.

#### 🔒 Autorisation
- Rôle requis: **Admin**

#### 📥 Paramètres de route

| Paramètre | Type | Description |
|-----------|------|-------------|
| `id` | `integer` | ID de la commande |

#### 📥 Request Body

```json
{
  "status": 3,
  "note": "Commande préparée et prête pour expédition"
}
```

**Paramètres:**

| Champ | Type | Requis | Description |
|-------|------|--------|-------------|
| `status` | `integer` | ✅ | Nouveau statut (voir [OrderStatus](#orderstatus)) |
| `note` | `string` | ❌ | Note explicative du changement (max: 600 caractères) |

#### 📤 Réponse

**Succès:** `204 No Content`

**Erreurs:**

```json
// Commande introuvable
{
  "status": 404,
  "title": "Introuvable",
  "detail": "Commande introuvable.",
  "instance": "/api/orders/156/status",
  "traceId": "00-abc123..."
}

// Validation échouée
{
  "status": 400,
  "title": "Validation Failed",
  "detail": "One or more validation errors occurred",
  "errors": {
    "status": ["The status field is required."]
  }
}
```

#### 💡 Exemples

**Passer en préparation:**
```bash
curl -X PUT https://api.example.com/api/orders/156/status \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "status": 3,
    "note": "Commande assignée à léquipe de préparation"
  }'
```

**Marquer comme livrée:**
```bash
curl -X PUT https://api.example.com/api/orders/156/status \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "status": 6,
    "note": "Commande livrée et signée par le client"
  }'
```

#### 📌 Notes importantes

1. **Historique automatique** : Chaque changement de statut est enregistré dans `StatusHistory` avec :
   - Statut précédent (`FromStatus`)
   - Nouveau statut (`ToStatus`)
   - Snapshot du statut de paiement
   - Note de l'admin
   - ID de l'admin qui a effectué le changement
   - Date et heure du changement

2. **Traçabilité complète** : L'historique permet de retracer tous les changements d'état de la commande.

---

## 8. Mettre à jour les Frais

### `PUT /api/orders/{id}/fees`

Permet à l'admin de modifier les frais de livraison et les réductions d'une commande.

#### 🔒 Autorisation
- Rôle requis: **Admin**

#### 📥 Paramètres de route

| Paramètre | Type | Description |
|-----------|------|-------------|
| `id` | `integer` | ID de la commande |

#### 📥 Request Body

```json
{
  "deliveryFee": 15.000,
  "discountTotal": 10.000
}
```

**Paramètres:**

| Champ | Type | Requis | Description |
|-------|------|--------|-------------|
| `deliveryFee` | `decimal` | ✅ | Frais de livraison (min: 0, max: 999999) |
| `discountTotal` | `decimal` | ✅ | Montant total des réductions (min: 0, max: 999999) |

#### 📤 Réponse

**Succès:** `204 No Content`

**Effet:** Le total de la commande est recalculé automatiquement selon la formule :
```
Total = Subtotal - DiscountTotal + DeliveryFee
```

**Erreurs:**

```json
// Commande introuvable
{
  "status": 404,
  "title": "Introuvable",
  "detail": "Commande introuvable.",
  "instance": "/api/orders/156/fees",
  "traceId": "00-abc123..."
}

// Validation échouée
{
  "status": 400,
  "title": "Validation Failed",
  "detail": "One or more validation errors occurred",
  "errors": {
    "deliveryFee": ["The field deliveryFee must be between 0 and 999999."]
  }
}
```

#### 💡 Exemples

**Ajouter frais de livraison:**
```bash
curl -X PUT https://api.example.com/api/orders/156/fees \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "deliveryFee": 12.000,
    "discountTotal": 0.000
  }'
```

**Appliquer une réduction:**
```bash
curl -X PUT https://api.example.com/api/orders/156/fees \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "deliveryFee": 10.000,
    "discountTotal": 15.000
  }'
```

#### 📌 Notes importantes

1. **Recalcul automatique** : Le montant total est automatiquement recalculé après modification.
2. **Subtotal intact** : Le sous-total (somme des items) reste inchangé.
3. **Pas de notification** : Actuellement, aucune notification n'est envoyée au client lors de la modification des frais.

---

## 9. Types et Énumérations

### OrderStatus

Statuts possibles d'une commande :

| Valeur | Nom | Description | Peut annuler? |
|--------|-----|-------------|---------------|
| `0` | `Pending` | En attente de confirmation | ✅ Oui |
| `1` | `AwaitingPayment` | En attente de paiement (online) | ✅ Oui |
| `2` | `Paid` | Payée | ❌ Non |
| `3` | `Preparing` | En préparation | ❌ Non |
| `4` | `Ready` | Prête pour livraison | ❌ Non |
| `5` | `OutForDelivery` | En cours de livraison | ❌ Non |
| `6` | `Delivered` | Livrée | ❌ Non |
| `7` | `Canceled` | Annulée | - |
| `8` | `Refunded` | Remboursée | - |

### PaymentMethod

Méthodes de paiement disponibles :

| Valeur | Nom | Description |
|--------|-----|-------------|
| `0` | `CashOnDelivery` | Paiement à la livraison |
| `1` | `Online` | Paiement en ligne |

### PaymentStatus

Statuts de paiement :

| Valeur | Nom | Description |
|--------|-----|-------------|
| `0` | `Pending` | En attente |
| `1` | `Authorized` | Autorisé (pré-autorisation) |
| `2` | `Paid` | Payé avec succès |
| `3` | `Failed` | Échec du paiement |
| `4` | `Canceled` | Annulé |
| `5` | `Refunded` | Remboursé |

### PaymentProvider

Fournisseurs de paiement en ligne :

| Valeur | Nom | Description |
|--------|-----|-------------|
| `0` | `None` | Aucun (paiement à la livraison) |
| `10` | `Paymee` | Paymee (Tunisie) |
| `20` | `Konnect` | Konnect (Tunisie) |
| `30` | `Flouci` | Flouci (Tunisie) |
| `40` | `ClicToPaySMT` | Clic to Pay SMT (Tunisie) |

---

## 10. Modèles de Données

### OrderSummaryDto

Résumé d'une commande (liste).

```typescript
interface OrderSummaryDto {
  id: number;
  orderNumber: string;
  status: OrderStatus;
  paymentMethod: PaymentMethod;
  paymentStatus: PaymentStatus;
  total: number;
  currency: string;              // "TND"
  createdAt: string;             // ISO 8601 UTC
}
```

### OrderDetailsDto

Détails complets d'une commande.

```typescript
interface OrderDetailsDto {
  id: number;
  orderNumber: string;
  status: OrderStatus;
  paymentMethod: PaymentMethod;
  paymentStatus: PaymentStatus;
  provider: PaymentProvider;
  subtotal: number;              // Somme des items
  deliveryFee: number;
  discountTotal: number;
  total: number;                 // Subtotal - Discount + Delivery
  currency: string;              // "TND"
  shippingAddress: OrderShippingAddressDto;
  customerNote: string | null;
  adminNote: string | null;
  createdAt: string;             // ISO 8601 UTC
  items: OrderItemDto[];
}
```

### OrderItemDto

Item d'une commande.

```typescript
interface OrderItemDto {
  id: number;
  articleId: number;
  variantId: number | null;
  name: string;
  variantName: string | null;
  sku: string | null;
  imageUrl: string | null;
  unitPrice: number;
  quantity: number;
  lineTotal: number;             // unitPrice * quantity
}
```

### OrderShippingAddressDto

Adresse de livraison d'une commande.

```typescript
interface OrderShippingAddressDto {
  fullName: string | null;
  phone: string | null;
  line1: string | null;
  line2: string | null;
  city: string | null;
  postalCode: string | null;
  governorate: string | null;
  countryCode: string | null;    // "TN"
}
```

### OrderAdminListDto

Commande dans la liste admin (inclut infos client).

```typescript
interface OrderAdminListDto {
  id: number;
  orderNumber: string;
  status: OrderStatus;
  paymentMethod: PaymentMethod;
  paymentStatus: PaymentStatus;
  total: number;
  currency: string;
  createdAt: string;
  itemsCount: number;            // Nombre d'items
  customerEmail: string | null;
  customerFullName: string | null;
  customerPhone: string | null;
}
```

### PagedResult<T>

Résultat paginé générique.

```typescript
interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
```

---

## 11. Gestion des Erreurs

### Format de réponse d'erreur

Toutes les erreurs suivent le standard RFC 7807 (Problem Details):

```json
{
  "status": 400,
  "title": "Titre de l'erreur",
  "detail": "Description détaillée de l'erreur",
  "instance": "/api/orders",
  "traceId": "00-abc123def456...",
  "errors": {
    "field": ["Message d'erreur de validation"]
  }
}
```

### Codes HTTP

| Code | Signification | Cas d'usage |
|------|---------------|-------------|
| `200` | OK | Requête réussie avec données |
| `204` | No Content | Requête réussie sans données (update, delete) |
| `400` | Bad Request | Données invalides, règles métier non respectées |
| `401` | Unauthorized | Token manquant ou invalide |
| `403` | Forbidden | Permissions insuffisantes |
| `404` | Not Found | Ressource introuvable |
| `409` | Conflict | Conflit (ex: stock insuffisant) |
| `500` | Internal Server Error | Erreur serveur |

### Erreurs fréquentes

**Non authentifié:**
```json
{
  "status": 401,
  "title": "Non authentifié",
  "detail": "Utilisateur non authentifié."
}
```

**Commande vide:**
```json
{
  "status": 400,
  "title": "Requête invalide",
  "detail": "Commande vide."
}
```

**Article indisponible:**
```json
{
  "status": 400,
  "title": "Requête invalide",
  "detail": "Un ou plusieurs produits sont indisponibles."
}
```

**Stock insuffisant:**
```json
{
  "status": 409,
  "title": "Stock insuffisant",
  "detail": "La commande a été créée mais le stock est insuffisant. Annulation en cours."
}
```

---

## 12. Flux de Commande

### Scénario 1: Commande avec paiement à la livraison (COD)

```
1. Client → POST /api/orders (paymentMethod=0)
   └─ Commande créée avec status = Pending (0)
   └─ PaymentStatus = Pending (0)
   └─ PAS de réservation de stock

2. Admin → PUT /api/orders/{id}/status (status=3)
   └─ Status = Preparing (3)
   └─ Stock déduit manuellement si nécessaire

3. Admin → PUT /api/orders/{id}/status (status=5)
   └─ Status = OutForDelivery (5)

4. Livreur livre + client paie
   └─ Admin → PUT /api/orders/{id}/status (status=6)
   └─ Status = Delivered (6)
   └─ PaymentStatus = Paid (2)
```

### Scénario 2: Commande avec paiement en ligne

```
1. Client → POST /api/orders (paymentMethod=1, provider=10)
   └─ Commande créée avec status = AwaitingPayment (1)
   └─ PaymentStatus = Pending (0)
   └─ Réservation de stock créée (15 minutes) ✅

2a. Paiement réussi (via PaymentsController)
    └─ Status = Paid (2)
    └─ PaymentStatus = Paid (2)
    └─ Réservation → Committed
    └─ Stock déduit définitivement

2b. Paiement échoué ou timeout
    └─ Status = Canceled (7)
    └─ PaymentStatus = Failed (3) ou Canceled (4)
    └─ Réservation → Released/Expired
    └─ Stock libéré

3. Admin → PUT /api/orders/{id}/status (status=3)
   └─ Status = Preparing (3)

4. Admin → PUT /api/orders/{id}/status (status=5)
   └─ Status = OutForDelivery (5)

5. Admin → PUT /api/orders/{id}/status (status=6)
   └─ Status = Delivered (6)
```

### Scénario 3: Annulation par le client

```
Conditions:
- Status = Pending (0) ou AwaitingPayment (1)
- PaymentStatus ≠ Paid (2)

Client → PATCH /api/orders/me/{id}/cancel
└─ Status = Canceled (7)
└─ PaymentStatus = Canceled (4) si Pending
└─ Si réservation active → Released
```

### Scénario 4: Modification des frais par l'admin

```
Admin → PUT /api/orders/{id}/fees
{
  "deliveryFee": 12.000,
  "discountTotal": 5.000
}

Calcul automatique:
Total = Subtotal (115.500) - Discount (5.000) + Delivery (12.000)
Total = 122.500 TND
```

---

## 🔧 Recommandations Frontend

### 1. **Affichage visuel des statuts**

Utilisez des couleurs et des icônes pour une meilleure UX :

```javascript
const getStatusBadge = (status) => {
  const statusConfig = {
    0: { label: 'En attente', color: 'gray', icon: '⏳' },
    1: { label: 'Attente paiement', color: 'orange', icon: '💳' },
    2: { label: 'Payée', color: 'green', icon: '✅' },
    3: { label: 'En préparation', color: 'blue', icon: '📦' },
    4: { label: 'Prête', color: 'teal', icon: '✔️' },
    5: { label: 'En livraison', color: 'purple', icon: '🚚' },
    6: { label: 'Livrée', color: 'green', icon: '🏠' },
    7: { label: 'Annulée', color: 'red', icon: '❌' },
    8: { label: 'Remboursée', color: 'pink', icon: '💸' }
  };
  
  return statusConfig[status] || statusConfig[0];
};
```

### 2. **Validation des adresses côté client**

Validez les champs avant soumission :

```javascript
const validateShippingAddress = (address) => {
  const errors = {};
  
  if (!address.fullName?.trim()) {
    errors.fullName = 'Le nom complet est requis';
  }
  
  if (!address.phone?.trim()) {
    errors.phone = 'Le numéro de téléphone est requis';
  } else if (!/^\+216\s?\d{2}\s?\d{3}\s?\d{3}$/.test(address.phone)) {
    errors.phone = 'Format: +216 XX XXX XXX';
  }
  
  if (!address.line1?.trim()) {
    errors.line1 = 'L\'adresse est requise';
  }
  
  if (!address.city?.trim()) {
    errors.city = 'La ville est requise';
  }
  
  return errors;
};
```

### 3. **Récapitulatif avant commande**

Affichez toujours un récapitulatif complet avant la soumission :

```javascript
const OrderSummary = ({ items, shippingAddress, paymentMethod }) => {
  const subtotal = items.reduce((sum, item) => 
    sum + (item.unitPrice * item.quantity), 0
  );
  
  return (
    <div className="order-summary">
      <h3>Récapitulatif de la commande</h3>
      
      {/* Items */}
      <div className="items">
        {items.map(item => (
          <div key={item.articleId}>
            {item.name} {item.variantName && `(${item.variantName})`}
            - {item.quantity} × {item.unitPrice.toFixed(3)} TND
            = {(item.quantity * item.unitPrice).toFixed(3)} TND
          </div>
        ))}
      </div>
      
      {/* Totaux */}
      <div className="totals">
        <div>Sous-total: {subtotal.toFixed(3)} TND</div>
        <div>Livraison: À calculer</div>
        <div className="total">Total (estimé): {subtotal.toFixed(3)} TND</div>
      </div>
      
      {/* Adresse */}
      <div className="address">
        <strong>Livraison à:</strong>
        <p>{shippingAddress.fullName}</p>
        <p>{shippingAddress.line1}</p>
        {shippingAddress.line2 && <p>{shippingAddress.line2}</p>}
        <p>{shippingAddress.postalCode} {shippingAddress.city}</p>
        <p>{shippingAddress.phone}</p>
      </div>
      
      {/* Paiement */}
      <div className="payment">
        <strong>Paiement:</strong>
        <p>{paymentMethod === 0 ? 'À la livraison' : 'En ligne'}</p>
      </div>
    </div>
  );
};
```

### 4. **Gestion du panier → commande**

Transformez le panier en format de commande :

```javascript
const createOrderFromCart = async (cart, shippingAddress, paymentDetails) => {
  const orderDto = {
    paymentMethod: paymentDetails.method,
    provider: paymentDetails.method === 1 ? paymentDetails.provider : null,
    customerNote: paymentDetails.note || null,
    shippingAddress: {
      fullName: shippingAddress.fullName,
      phone: shippingAddress.phone,
      line1: shippingAddress.line1,
      line2: shippingAddress.line2 || null,
      city: shippingAddress.city,
      postalCode: shippingAddress.postalCode || null,
      governorate: shippingAddress.governorate || null
    },
    requestedDeliveryDate: paymentDetails.deliveryDate || null,
    deliveryTimeSlot: paymentDetails.timeSlot || null,
    items: cart.items.map(item => ({
      articleId: item.articleId,
      variantId: item.variantId || null,
      quantity: item.quantity
    }))
  };
  
  try {
    const response = await fetch('/api/orders', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(orderDto)
    });
    
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.detail || 'Erreur lors de la création de la commande');
    }
    
    const order = await response.json();
    
    // Vider le panier après création réussie
    await clearCart();
    
    // Rediriger selon le mode de paiement
    if (paymentDetails.method === 1) {
      // Rediriger vers la page de paiement
      window.location.href = `/payment/${order.id}`;
    } else {
      // Afficher la confirmation
      window.location.href = `/order-confirmation/${order.id}`;
    }
    
    return order;
  } catch (error) {
    console.error('Erreur création commande:', error);
    throw error;
  }
};
```

### 5. **Polling pour suivi de commande**

Mettez à jour automatiquement le statut :

```javascript
// Hook React pour le suivi de commande
const useOrderTracking = (orderId) => {
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  
  useEffect(() => {
    const fetchOrder = async () => {
      try {
        const response = await fetch(`/api/orders/me/${orderId}`, {
          headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (response.ok) {
          const data = await response.json();
          setOrder(data);
        }
      } catch (error) {
        console.error('Erreur fetch order:', error);
      } finally {
        setLoading(false);
      }
    };
    
    // Fetch initial
    fetchOrder();
    
    // Polling si commande active (pas livrée/annulée)
    const interval = setInterval(() => {
      if (order && ![6, 7, 8].includes(order.status)) {
        fetchOrder();
      }
    }, 30000); // 30 secondes
    
    return () => clearInterval(interval);
  }, [orderId, token]);
  
  return { order, loading };
};
```

### 6. **Interface admin - Dashboard**

Statistiques utiles pour l'admin :

```javascript
const OrdersDashboard = () => {
  const [stats, setStats] = useState({
    pending: 0,
    awaitingPayment: 0,
    preparing: 0,
    outForDelivery: 0,
    todayTotal: 0,
    todayCount: 0
  });
  
  useEffect(() => {
    const fetchStats = async () => {
      // Requests parallèles
      const [pending, awaiting, preparing, delivery, today] = await Promise.all([
        fetch('/api/orders?status=0&pageSize=1').then(r => r.json()),
        fetch('/api/orders?status=1&pageSize=1').then(r => r.json()),
        fetch('/api/orders?status=3&pageSize=1').then(r => r.json()),
        fetch('/api/orders?status=5&pageSize=1').then(r => r.json()),
        fetch(`/api/orders?from=${getTodayStart()}&pageSize=100`).then(r => r.json())
      ]);
      
      setStats({
        pending: pending.totalCount,
        awaitingPayment: awaiting.totalCount,
        preparing: preparing.totalCount,
        outForDelivery: delivery.totalCount,
        todayCount: today.totalCount,
        todayTotal: today.items.reduce((sum, o) => sum + o.total, 0)
      });
    };
    
    fetchStats();
    const interval = setInterval(fetchStats, 60000); // 1 minute
    
    return () => clearInterval(interval);
  }, []);
  
  return (
    <div className="dashboard">
      <StatCard title="En attente" value={stats.pending} color="gray" />
      <StatCard title="Attente paiement" value={stats.awaitingPayment} color="orange" />
      <StatCard title="En préparation" value={stats.preparing} color="blue" />
      <StatCard title="En livraison" value={stats.outForDelivery} color="purple" />
      <StatCard 
        title="Aujourd'hui" 
        value={`${stats.todayCount} commandes`}
        subtitle={`${stats.todayTotal.toFixed(3)} TND`}
        color="green" 
      />
    </div>
  );
};
```

### 7. **Filtres avancés admin**

Interface de filtrage complète :

```javascript
const OrderFilters = ({ onFilterChange }) => {
  const [filters, setFilters] = useState({
    status: null,
    paymentStatus: null,
    paymentMethod: null,
    search: '',
    from: null,
    to: null
  });
  
  const handleChange = (field, value) => {
    const newFilters = { ...filters, [field]: value };
    setFilters(newFilters);
    onFilterChange(newFilters);
  };
  
  return (
    <div className="filters">
      <input
        type="text"
        placeholder="Rechercher (numéro, email, nom...)"
        value={filters.search}
        onChange={(e) => handleChange('search', e.target.value)}
      />
      
      <select onChange={(e) => handleChange('status', e.target.value || null)}>
        <option value="">Tous les statuts</option>
        <option value="0">En attente</option>
        <option value="1">Attente paiement</option>
        <option value="2">Payée</option>
        <option value="3">En préparation</option>
        <option value="5">En livraison</option>
        <option value="6">Livrée</option>
        <option value="7">Annulée</option>
      </select>
      
      <select onChange={(e) => handleChange('paymentMethod', e.target.value || null)}>
        <option value="">Toutes les méthodes</option>
        <option value="0">Paiement à la livraison</option>
        <option value="1">Paiement en ligne</option>
      </select>
      
      <input
        type="date"
        placeholder="De"
        onChange={(e) => handleChange('from', e.target.value ? new Date(e.target.value).toISOString() : null)}
      />
      
      <input
        type="date"
        placeholder="À"
        onChange={(e) => handleChange('to', e.target.value ? new Date(e.target.value).toISOString() : null)}
      />
    </div>
  );
};
```

---

## 📞 Support

Pour toute question ou problème:

1. Vérifiez le `traceId` dans la réponse d'erreur
2. Consultez les logs serveur avec le `traceId`
3. Vérifiez les [énumérations](#9-types-et-énumérations) pour les valeurs valides
4. Contactez l'équipe backend avec les détails complets de la requête

---

## 📊 Résumé des Endpoints

| Méthode | Endpoint | Rôle | Description |
|---------|----------|------|-------------|
| `POST` | `/api/orders` | User | Créer une commande |
| `GET` | `/api/orders/me` | User | Mes commandes |
| `GET` | `/api/orders/me/{id}` | User | Détails de ma commande |
| `PATCH` | `/api/orders/me/{id}/cancel` | User | Annuler ma commande |
| `GET` | `/api/orders` | Admin | Liste paginée + filtres |
| `GET` | `/api/orders/{id}` | Admin | Détails d'une commande |
| `PUT` | `/api/orders/{id}/status` | Admin | Changer le statut |
| `PUT` | `/api/orders/{id}/fees` | Admin | Modifier les frais |

---

**Dernière mise à jour:** 11 février 2026  
**Version API:** 1.0  
**Maintenu par:** Équipe Backend Hlouwa
