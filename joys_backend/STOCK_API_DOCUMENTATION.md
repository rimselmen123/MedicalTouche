# 📦 Documentation API - Gestion du Stock

> Documentation complète des endpoints API pour la gestion du stock, des mouvements et des réservations.

**Base URL:** `/api/stock`

**Date:** 11 février 2026

---

## 🔐 Authentification

Tous les endpoints nécessitent une authentification avec le rôle **Admin**, sauf indication contraire.

**Header requis:**
```
Authorization: Bearer <token>
```

---

## 📋 Table des matières

1. [Ajustement du Stock](#1-ajustement-du-stock)
2. [Consulter les Items de Stock](#2-consulter-les-items-de-stock)
3. [Historique des Mouvements](#3-historique-des-mouvements)
4. [Consulter les Réservations](#4-consulter-les-réservations)
5. [Expirer les Réservations](#5-expirer-les-réservations)
6. [Types et Énumérations](#6-types-et-énumérations)
7. [Modèles de Données](#7-modèles-de-données)
8. [Gestion des Erreurs](#8-gestion-des-erreurs)

---

## 1. Ajustement du Stock

### `POST /api/stock/adjust`

Permet d'ajuster manuellement le stock d'un article (augmentation ou diminution).

#### 🔒 Autorisation
- Rôle requis: **Admin**

#### 📥 Request Body

```json
{
  "articleId": 12,
  "variantId": 5,
  "delta": 10.5,
  "note": "Réception fournisseur"
}
```

**Paramètres:**

| Champ | Type | Requis | Description |
|-------|------|--------|-------------|
| `articleId` | `integer` | ✅ | ID de l'article |
| `variantId` | `integer` | ❌ | ID de la variante (null si article sans variante) |
| `delta` | `decimal` | ✅ | Quantité à ajouter (positif) ou retirer (négatif). Ne peut pas être 0 |
| `note` | `string` | ❌ | Note explicative de l'ajustement |

#### 📤 Réponse

**Succès:** `204 No Content`

**Erreurs:**

```json
// Si delta = 0
{
  "status": 400,
  "title": "Requête invalide",
  "detail": "Delta ne peut pas être 0.",
  "instance": "/api/stock/adjust",
  "traceId": "00-abc123..."
}

// Si OnHand deviendrait négatif
{
  "status": 400,
  "title": "Requête invalide",
  "detail": "OnHand ne peut pas devenir négatif.",
  "instance": "/api/stock/adjust",
  "traceId": "00-abc123..."
}
```

#### 💡 Exemples

**Ajouter du stock:**
```bash
curl -X POST https://api.example.com/api/stock/adjust \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "articleId": 15,
    "variantId": null,
    "delta": 50,
    "note": "Réception commande fournisseur #1234"
  }'
```

**Retirer du stock:**
```bash
curl -X POST https://api.example.com/api/stock/adjust \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "articleId": 15,
    "variantId": 2,
    "delta": -5,
    "note": "Produit endommagé"
  }'
```

---

## 2. Consulter les Items de Stock

### `GET /api/stock/items`

Récupère la liste complète des items de stock avec leurs quantités disponibles.

#### 🔒 Autorisation
- Rôle requis: **Admin**

#### 📥 Paramètres

Aucun paramètre requis.

#### 📤 Réponse

**Succès:** `200 OK`

```json
[
  {
    "articleId": 12,
    "articleVariantId": 5,
    "onHand": 100.0,
    "reserved": 15.0,
    "available": 85.0
  },
  {
    "articleId": 13,
    "articleVariantId": null,
    "onHand": 50.0,
    "reserved": 0.0,
    "available": 50.0
  }
]
```

**Structure de la réponse:**

| Champ | Type | Description |
|-------|------|-------------|
| `articleId` | `integer` | ID de l'article |
| `articleVariantId` | `integer\|null` | ID de la variante (null si aucune variante) |
| `onHand` | `decimal` | Quantité physiquement disponible en stock |
| `reserved` | `decimal` | Quantité réservée (commandes en attente de paiement) |
| `available` | `decimal` | Quantité disponible à la vente (OnHand - Reserved) |

#### 💡 Exemple

```bash
curl -X GET https://api.example.com/api/stock/items \
  -H "Authorization: Bearer <token>"
```

---

## 3. Historique des Mouvements

### `GET /api/stock/movements`

Récupère l'historique des mouvements de stock avec filtres optionnels.

#### 🔒 Autorisation
- Rôle requis: **Admin**

#### 📥 Paramètres de requête

| Paramètre | Type | Requis | Défaut | Description |
|-----------|------|--------|--------|-------------|
| `articleId` | `integer` | ❌ | - | Filtrer par ID d'article |
| `variantId` | `integer` | ❌ | - | Filtrer par ID de variante |
| `limit` | `integer` | ❌ | `100` | Nombre maximum de résultats (max: 1000) |

#### 📤 Réponse

**Succès:** `200 OK`

```json
[
  {
    "id": 523,
    "articleId": 12,
    "articleVariantId": 5,
    "type": 0,
    "quantity": 50.0,
    "refType": "Admin",
    "refId": "user-abc-123",
    "note": "Réception fournisseur",
    "actorUserId": "user-abc-123",
    "createdAt": "2026-02-10T14:30:00Z"
  },
  {
    "id": 522,
    "articleId": 12,
    "articleVariantId": 5,
    "type": 2,
    "quantity": 2.0,
    "refType": "Order",
    "refId": "ORD-20260210-001",
    "note": "Reserve until 2026-02-10T15:00:00Z",
    "actorUserId": "user-xyz-456",
    "createdAt": "2026-02-10T14:15:00Z"
  }
]
```

**Structure de la réponse:**

| Champ | Type | Description |
|-------|------|-------------|
| `id` | `integer` | ID du mouvement |
| `articleId` | `integer` | ID de l'article |
| `articleVariantId` | `integer\|null` | ID de la variante |
| `type` | `integer` | Type de mouvement (voir énumération [StockMovementType](#stockmovementtype)) |
| `quantity` | `decimal` | Quantité du mouvement |
| `refType` | `string` | Type de référence ("Order", "Admin", "Import") |
| `refId` | `string` | Identifiant de référence (numéro de commande, ID utilisateur, etc.) |
| `note` | `string\|null` | Note explicative |
| `actorUserId` | `string\|null` | ID de l'utilisateur ayant effectué l'action |
| `createdAt` | `datetime` | Date de création (UTC) |

#### 💡 Exemples

**Tous les mouvements:**
```bash
curl -X GET "https://api.example.com/api/stock/movements?limit=50" \
  -H "Authorization: Bearer <token>"
```

**Mouvements d'un article spécifique:**
```bash
curl -X GET "https://api.example.com/api/stock/movements?articleId=12&limit=100" \
  -H "Authorization: Bearer <token>"
```

**Mouvements d'une variante spécifique:**
```bash
curl -X GET "https://api.example.com/api/stock/movements?articleId=12&variantId=5&limit=200" \
  -H "Authorization: Bearer <token>"
```

---

## 4. Consulter les Réservations

### `GET /api/stock/reservations`

Récupère les réservations de stock avec leurs détails et items associés.

#### 🔒 Autorisation
- Rôle requis: **Admin**

#### 📥 Paramètres de requête

| Paramètre | Type | Requis | Description |
|-----------|------|--------|-------------|
| `status` | `integer` | ❌ | Filtrer par statut (voir [StockReservationStatus](#stockreservationstatus)) |

#### 📤 Réponse

**Succès:** `200 OK`

```json
[
  {
    "id": 45,
    "orderId": 123,
    "orderNumber": "ORD-20260210-001",
    "status": 0,
    "expiresAt": "2026-02-10T15:30:00Z",
    "reason": "Online checkout reserve",
    "createdAt": "2026-02-10T14:30:00Z",
    "items": [
      {
        "articleId": 12,
        "articleVariantId": 5,
        "name": "T-shirt Premium",
        "variantName": "Rouge - M",
        "quantity": 2.0
      },
      {
        "articleId": 15,
        "articleVariantId": null,
        "name": "Casquette Classic",
        "variantName": null,
        "quantity": 1.0
      }
    ]
  }
]
```

**Structure de la réponse:**

| Champ | Type | Description |
|-------|------|-------------|
| `id` | `integer` | ID de la réservation |
| `orderId` | `integer` | ID de la commande |
| `orderNumber` | `string` | Numéro de commande |
| `status` | `integer` | Statut de la réservation (voir [StockReservationStatus](#stockreservationstatus)) |
| `expiresAt` | `datetime` | Date d'expiration de la réservation (UTC) |
| `reason` | `string\|null` | Raison de la réservation |
| `createdAt` | `datetime` | Date de création (UTC) |
| `items` | `array` | Liste des items réservés |

**Structure des items:**

| Champ | Type | Description |
|-------|------|-------------|
| `articleId` | `integer` | ID de l'article |
| `articleVariantId` | `integer\|null` | ID de la variante |
| `name` | `string` | Nom de l'article |
| `variantName` | `string\|null` | Nom de la variante |
| `quantity` | `decimal` | Quantité réservée |

#### 💡 Exemples

**Toutes les réservations:**
```bash
curl -X GET "https://api.example.com/api/stock/reservations" \
  -H "Authorization: Bearer <token>"
```

**Réservations actives uniquement:**
```bash
curl -X GET "https://api.example.com/api/stock/reservations?status=0" \
  -H "Authorization: Bearer <token>"
```

**Réservations expirées:**
```bash
curl -X GET "https://api.example.com/api/stock/reservations?status=3" \
  -H "Authorization: Bearer <token>"
```

---

## 5. Expirer les Réservations

### `POST /api/stock/expire-reservations`

Expire toutes les réservations actives dont la date d'expiration est dépassée et libère le stock réservé.

#### 🔒 Autorisation
- Rôle requis: **Admin**

#### 📥 Request Body

Aucun body requis.

#### 📤 Réponse

**Succès:** `200 OK`

```json
{
  "expired": 3
}
```

| Champ | Type | Description |
|-------|------|-------------|
| `expired` | `integer` | Nombre de réservations expirées |

#### 💡 Exemple

```bash
curl -X POST https://api.example.com/api/stock/expire-reservations \
  -H "Authorization: Bearer <token>"
```

#### 📌 Note importante

Cette action devrait être exécutée régulièrement (par exemple via un job CRON) pour libérer automatiquement le stock des commandes en attente de paiement expirées.

---

## 6. Types et Énumérations

### StockMovementType

Types de mouvements de stock:

| Valeur | Nom | Description |
|--------|-----|-------------|
| `0` | `In` | Entrée de stock (achat, réception fournisseur) |
| `1` | `Out` | Sortie de stock (vente confirmée, perte) |
| `2` | `Reserve` | Réservation temporaire (commande en attente de paiement) |
| `3` | `Release` | Libération d'une réservation (paiement échoué/timeout) |
| `4` | `Adjust` | Ajustement manuel (correction, inventaire) |

### StockReservationStatus

Statuts des réservations:

| Valeur | Nom | Description |
|--------|-----|-------------|
| `0` | `Active` | Réservation active, en attente de paiement |
| `1` | `Committed` | Réservation confirmée, stock déduit (paiement réussi) |
| `2` | `Released` | Réservation libérée manuellement |
| `3` | `Expired` | Réservation expirée automatiquement |

---

## 7. Modèles de Données

### StockItem

Représente le stock disponible pour un article ou une variante.

```typescript
interface StockItem {
  articleId: number;
  articleVariantId: number | null;
  onHand: number;      // Quantité physiquement en stock
  reserved: number;    // Quantité réservée (bloquée)
  // Calculé: available = onHand - reserved
}
```

### StockMovement

Trace chaque mouvement de stock.

```typescript
interface StockMovement {
  id: number;
  articleId: number;
  articleVariantId: number | null;
  type: StockMovementType;
  quantity: number;
  refType: string | null;     // "Order", "Admin", "Import"
  refId: string | null;       // Référence externe
  note: string | null;
  actorUserId: string | null;
  createdAt: string;          // ISO 8601 UTC
}
```

### StockReservation

Réservation de stock pour une commande.

```typescript
interface StockReservation {
  id: number;
  orderId: number;
  orderNumber: string;
  status: StockReservationStatus;
  expiresAt: string;          // ISO 8601 UTC
  reason: string | null;
  createdAt: string;          // ISO 8601 UTC
  items: StockReservationItem[];
}

interface StockReservationItem {
  articleId: number;
  articleVariantId: number | null;
  name: string;
  variantName: string | null;
  quantity: number;
}
```

---

## 8. Gestion des Erreurs

### Format de réponse d'erreur

Toutes les erreurs suivent le standard RFC 7807 (Problem Details):

```json
{
  "status": 400,
  "title": "Titre de l'erreur",
  "detail": "Description détaillée de l'erreur",
  "instance": "/api/stock/adjust",
  "traceId": "00-abc123def456...",
  "errors": {
    "field": ["Message d'erreur de validation"]
  }
}
```

### Codes HTTP

| Code | Signification | Description |
|------|---------------|-------------|
| `200` | OK | Requête réussie avec données |
| `204` | No Content | Requête réussie sans données à retourner |
| `400` | Bad Request | Données invalides ou logique métier non respectée |
| `401` | Unauthorized | Token manquant ou invalide |
| `403` | Forbidden | Utilisateur non autorisé (pas le rôle Admin) |
| `404` | Not Found | Ressource introuvable |
| `500` | Internal Server Error | Erreur serveur |

### Exemples d'erreurs courantes

**Authentification manquante:**
```json
{
  "status": 401,
  "title": "Unauthorized",
  "detail": "Authentication required"
}
```

**Permissions insuffisantes:**
```json
{
  "status": 403,
  "title": "Forbidden",
  "detail": "Admin role required"
}
```

**Validation échouée:**
```json
{
  "status": 400,
  "title": "Validation Failed",
  "detail": "One or more validation errors occurred",
  "errors": {
    "delta": ["Delta ne peut pas être 0."]
  }
}
```

---

## 📊 Flux de gestion du stock

### Scénario 1: Commande avec paiement en ligne

```
1. Client passe commande → Système crée une réservation (Reserve)
   └─ Status: Active, ExpiresAt: +30 minutes
   
2a. Paiement réussi → Système confirme (Commit)
    └─ Status: Committed, Stock: OnHand diminué, Reserved diminué
    └─ Mouvement: Out
    
2b. Paiement échoué/timeout → Système libère (Release/Expire)
    └─ Status: Released/Expired, Stock: Reserved diminué
    └─ Mouvement: Release
```

### Scénario 2: Commande en paiement à la livraison

```
1. Client passe commande → Pas de réservation créée
2. Admin prépare → Stock sort immédiatement
   └─ Mouvement: Out
```

### Scénario 3: Réapprovisionnement

```
1. Admin reçoit stock fournisseur
2. Admin ajuste → POST /api/stock/adjust
   └─ delta: +100
   └─ Mouvement: Adjust (ou In selon logique)
```

---

## 🔧 Recommandations Frontend

### 1. **Polling pour les réservations actives**

Rafraîchissez les réservations actives toutes les 30-60 secondes pour détecter les expirations:

```javascript
// Exemple avec React
useEffect(() => {
  const interval = setInterval(async () => {
    const response = await fetch('/api/stock/reservations?status=0', {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const data = await response.json();
    setActiveReservations(data);
  }, 30000); // 30 secondes

  return () => clearInterval(interval);
}, [token]);
```

### 2. **Affichage du stock disponible**

Toujours afficher `available` (OnHand - Reserved) comme stock disponible à la vente:

```javascript
const stockDisplay = (item) => {
  const available = item.onHand - item.reserved;
  return (
    <div>
      <span>Disponible: {available}</span>
      {item.reserved > 0 && (
        <span className="text-warning">
          ({item.reserved} réservé)
        </span>
      )}
    </div>
  );
};
```

### 3. **Gestion des erreurs**

Toujours vérifier le champ `traceId` pour le support:

```javascript
const handleError = (error) => {
  const detail = error.detail || 'Une erreur est survenue';
  const traceId = error.traceId;
  
  showNotification({
    type: 'error',
    message: detail,
    action: {
      label: 'Contacter le support',
      onClick: () => copyToClipboard(traceId)
    }
  });
};
```

### 4. **Validation côté client**

Validez avant d'envoyer pour une meilleure UX:

```javascript
const validateStockAdjustment = (delta) => {
  if (delta === 0) {
    return 'La quantité ne peut pas être 0';
  }
  if (currentStock + delta < 0) {
    return 'Le stock ne peut pas être négatif';
  }
  return null;
};
```

---

## 📞 Support

Pour toute question ou problème:

1. Vérifiez le `traceId` dans la réponse d'erreur
2. Consultez les logs serveur avec le `traceId`
3. Contactez l'équipe backend avec les détails complets de la requête

---

**Dernière mise à jour:** 11 février 2026  
**Version API:** 1.0  
**Maintenu par:** Équipe Backend Hlouwa
