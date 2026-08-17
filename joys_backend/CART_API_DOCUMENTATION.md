# 🛒 Documentation API - Gestion du Panier (Cart)

> Documentation complète des endpoints API pour la gestion du panier d'achat et guide d'intégration frontend.

**Base URL:** `/api/cart`

**Date:** 11 février 2026

---

## 🔐 Authentification

**Tous les endpoints nécessitent une authentification utilisateur.**

Le panier est personnel et lié à chaque utilisateur connecté. Aucun accès anonyme n'est autorisé.

**Header requis:**
```
Authorization: Bearer <token>
```

---

## 📋 Table des matières

1. [Vue d'ensemble](#vue-densemble)
2. [Objectif du Cart côté Frontend](#objectif-du-cart-côté-frontend)
3. [Obtenir le Panier](#1-obtenir-le-panier)
4. [Ajouter un Article](#2-ajouter-un-article)
5. [Modifier la Quantité](#3-modifier-la-quantité)
6. [Supprimer un Article](#4-supprimer-un-article)
7. [Vider le Panier](#5-vider-le-panier)
8. [Modèles de Données](#modèles-de-données)
9. [Gestion des Erreurs](#gestion-des-erreurs)
10. [Flux et Scénarios](#flux-et-scénarios)
11. [Implémentation Frontend](#implémentation-frontend)
12. [Bonnes Pratiques](#bonnes-pratiques)

---

## Vue d'ensemble

### Caractéristiques du panier

- ✅ **Persistance serveur** : Le panier est stocké en base de données
- ✅ **Recalcul automatique** : Les prix sont toujours synchronisés avec les articles
- ✅ **Fusion intelligente** : Ajouter le même article/variante incrémente la quantité
- ✅ **Validation automatique** : Les articles inactifs ou supprimés sont retirés automatiquement
- ✅ **Synchronisation temps réel** : Disponible sur tous les appareils de l'utilisateur
- ✅ **Préparation commande** : Structure optimisée pour créer une commande

### Processus typique

```
1. Utilisateur navigue → Ajoute articles au panier
2. Panier persisté → Disponible partout (mobile, desktop, etc.)
3. Modifications → Quantités, suppressions
4. Validation → Passage en caisse
5. Création commande → Panier vidé automatiquement
```

---

## Objectif du Cart côté Frontend

### 🎯 Pourquoi utiliser le panier serveur ?

#### 1. **Expérience Multi-Appareils**
```
Client ajoute article sur mobile → Se connecte sur desktop → Panier synchronisé ✅
```
Le panier suit l'utilisateur partout, pas de perte de données entre sessions.

#### 2. **Prix Toujours à Jour**
```
Article était 50 TND → Prix change à 45 TND → Panier mis à jour automatiquement ✅
```
Évite les surprises à la caisse : le prix affiché est toujours le prix actuel.

#### 3. **Gestion Automatique des Stocks**
```
Article devient indisponible → Retiré automatiquement du panier ✅
Variante supprimée → Item retiré du panier ✅
```
Le système nettoie automatiquement les articles qui ne sont plus disponibles.

#### 4. **Base pour la Commande**
```
Panier validé → Conversion directe en commande
Items du panier → OrderItems
Totaux calculés → Prêts pour facturation
```

#### 5. **Évite la Fraude**
```
❌ Prix manipulé côté client → Rejeté
✅ Prix calculé serveur → Sécurisé
```
Les calculs côté serveur empêchent toute manipulation de prix.

### 📊 Architecture recommandée Frontend

```
┌─────────────────────────────────────────────┐
│           Frontend Application              │
│                                             │
│  ┌──────────────┐      ┌─────────────────┐ │
│  │  Local State │◄────►│  Cart Context   │ │
│  │  (React/Vue) │      │  /Store/Service │ │
│  └──────────────┘      └─────────────────┘ │
│         ▲                       ▲           │
│         │                       │           │
│         │    Auto-sync every    │           │
│         │    action (add/update)│           │
│         │                       │           │
│         ▼                       ▼           │
│  ┌─────────────────────────────────────┐   │
│  │      Cart API (Backend)             │   │
│  │  GET /api/cart                      │   │
│  │  POST /api/cart/items               │   │
│  │  PUT /api/cart/items/{id}           │   │
│  │  DELETE /api/cart/items/{id}        │   │
│  │  POST /api/cart/clear               │   │
│  └─────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
```

**Pattern recommandé :**
- Chaque action utilisateur déclenche immédiatement un appel API
- Le state local est mis à jour avec la réponse du serveur
- Pas de synchronisation complexe nécessaire
- Le serveur est la source de vérité unique

---

## 1. Obtenir le Panier

### `GET /api/cart`

Récupère le panier complet de l'utilisateur connecté avec tous les articles et totaux calculés.

#### 🔒 Autorisation
- Rôle requis: **User** ou **Admin** (authentifié)

#### 📥 Paramètres

Aucun paramètre requis.

#### 📤 Réponse

**Succès:** `200 OK`

```json
{
  "id": 42,
  "currency": "TND",
  "subtotal": 115.500,
  "discountTotal": 0.000,
  "deliveryFee": 0.000,
  "total": 115.500,
  "items": [
    {
      "id": 156,
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
      "id": 157,
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
  ],
  "updatedAt": "2026-02-11T14:30:52Z"
}
```

**Structure de la réponse:** (voir [CartDto](#cartdto))

**Panier vide:**
```json
{
  "id": 42,
  "currency": "TND",
  "subtotal": 0.000,
  "discountTotal": 0.000,
  "deliveryFee": 0.000,
  "total": 0.000,
  "items": [],
  "updatedAt": "2026-02-11T14:30:52Z"
}
```

#### 💡 Exemple

```bash
curl -X GET https://api.example.com/api/cart \
  -H "Authorization: Bearer <token>"
```

#### 📌 Comportements importants

1. **Création automatique** : Si l'utilisateur n'a pas encore de panier, il est créé automatiquement (vide).

2. **Recalcul automatique** : À chaque appel GET, le panier est recalculé :
   - Prix unitaires mis à jour depuis les articles
   - Articles inactifs supprimés
   - Variantes supprimées retirées
   - Totaux recalculés

3. **Ordre des items** : Les articles sont triés par date d'ajout (plus récent en premier).

4. **Utilisation recommandée** :
   - Au chargement de l'application (si utilisateur connecté)
   - Après authentification réussie
   - Avant d'afficher la page panier
   - Avant de passer à la caisse

---

## 2. Ajouter un Article

### `POST /api/cart/items`

Ajoute un article (avec ou sans variante) au panier. Si l'article existe déjà, les quantités sont fusionnées.

#### 🔒 Autorisation
- Rôle requis: **User** ou **Admin** (authentifié)

#### 📥 Request Body

```json
{
  "articleId": 12,
  "variantId": 5,
  "quantity": 2
}
```

**Paramètres:**

| Champ | Type | Requis | Description |
|-------|------|--------|-------------|
| `articleId` | `integer` | ✅ | ID de l'article à ajouter |
| `variantId` | `integer` | ❌ | ID de la variante (null si article sans variante) |
| `quantity` | `integer` | ✅ | Quantité à ajouter (min: 1, max: 999, défaut: 1) |

#### 📤 Réponse

**Succès:** `200 OK`

Retourne le panier complet mis à jour (même structure que [GET /api/cart](#1-obtenir-le-panier)).

**Erreurs:**

```json
// Article introuvable
{
  "status": 400,
  "title": "Requête invalide",
  "detail": "Article introuvable.",
  "instance": "/api/cart/items",
  "traceId": "00-abc123..."
}

// Article inactif
{
  "status": 400,
  "title": "Requête invalide",
  "detail": "Produit indisponible.",
  "instance": "/api/cart/items",
  "traceId": "00-abc123..."
}

// Variante introuvable
{
  "status": 400,
  "title": "Requête invalide",
  "detail": "Variant introuvable.",
  "instance": "/api/cart/items",
  "traceId": "00-abc123..."
}

// Validation échouée
{
  "status": 400,
  "title": "Validation Failed",
  "detail": "One or more validation errors occurred",
  "errors": {
    "quantity": ["The field Quantity must be between 1 and 999."]
  }
}
```

#### 💡 Exemples

**Ajouter un article simple (sans variante):**
```bash
curl -X POST https://api.example.com/api/cart/items \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "articleId": 15,
    "variantId": null,
    "quantity": 1
  }'
```

**Ajouter un article avec variante:**
```bash
curl -X POST https://api.example.com/api/cart/items \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "articleId": 12,
    "variantId": 5,
    "quantity": 3
  }'
```

**Exemple JavaScript:**
```javascript
async function addToCart(articleId, variantId, quantity = 1) {
  const response = await fetch('/api/cart/items', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      articleId,
      variantId,
      quantity
    })
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.detail || 'Erreur lors de l\'ajout au panier');
  }
  
  return await response.json();
}
```

#### 📌 Comportements importants

1. **Fusion automatique** : 
   ```
   Panier contient : T-shirt Rouge M (qté: 2)
   Ajout : T-shirt Rouge M (qté: 3)
   Résultat : T-shirt Rouge M (qté: 5) ✅
   ```

2. **Différenciation par variante** :
   ```
   Panier contient : T-shirt Rouge M (qté: 2)
   Ajout : T-shirt Rouge L (qté: 1)
   Résultat : 2 items séparés ✅
   ```

3. **Limite de quantité** : La quantité totale est automatiquement limitée à 999 maximum.

4. **Snapshot des données** :
   - Le nom de l'article est copié dans le panier
   - L'image de couverture est stockée
   - Le prix actuel est enregistré
   - Le SKU est copié

---

## 3. Modifier la Quantité

### `PUT /api/cart/items/{id}`

Modifie la quantité d'un article déjà présent dans le panier.

#### 🔒 Autorisation
- Rôle requis: **User** ou **Admin** (authentifié)

#### 📥 Paramètres de route

| Paramètre | Type | Description |
|-----------|------|-------------|
| `id` | `integer` | ID de l'item dans le panier (CartItem.Id) |

#### 📥 Request Body

```json
{
  "quantity": 5
}
```

**Paramètres:**

| Champ | Type | Requis | Description |
|-------|------|--------|-------------|
| `quantity` | `integer` | ✅ | Nouvelle quantité (min: 1, max: 999) |

#### 📤 Réponse

**Succès:** `200 OK`

Retourne le panier complet mis à jour.

**Erreurs:**

```json
// Item introuvable
{
  "status": 404,
  "title": "Introuvable",
  "detail": "Item introuvable.",
  "instance": "/api/cart/items/156",
  "traceId": "00-abc123..."
}

// Validation échouée
{
  "status": 400,
  "title": "Validation Failed",
  "detail": "One or more validation errors occurred",
  "errors": {
    "quantity": ["The field Quantity must be between 1 and 999."]
  }
}
```

#### 💡 Exemples

**Modifier la quantité:**
```bash
curl -X PUT https://api.example.com/api/cart/items/156 \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "quantity": 5
  }'
```

**Exemple JavaScript (incrémenter):**
```javascript
async function incrementQuantity(itemId, currentQuantity) {
  const response = await fetch(`/api/cart/items/${itemId}`, {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      quantity: currentQuantity + 1
    })
  });
  
  if (!response.ok) {
    throw new Error('Erreur lors de la modification');
  }
  
  return await response.json();
}
```

**Exemple JavaScript (décrémenter):**
```javascript
async function decrementQuantity(itemId, currentQuantity) {
  // Si quantité = 1, supprimer l'item au lieu de décrémenter
  if (currentQuantity <= 1) {
    return await deleteCartItem(itemId);
  }
  
  const response = await fetch(`/api/cart/items/${itemId}`, {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      quantity: currentQuantity - 1
    })
  });
  
  if (!response.ok) {
    throw new Error('Erreur lors de la modification');
  }
  
  return await response.json();
}
```

#### 📌 Comportements importants

1. **Recalcul automatique** : Les totaux sont recalculés après chaque modification.

2. **Limite automatique** : La quantité est automatiquement limitée entre 1 et 999.

3. **Quantité 0 non autorisée** : Pour retirer un article, utilisez DELETE au lieu de mettre la quantité à 0.

---

## 4. Supprimer un Article

### `DELETE /api/cart/items/{id}`

Retire complètement un article du panier.

#### 🔒 Autorisation
- Rôle requis: **User** ou **Admin** (authentifié)

#### 📥 Paramètres de route

| Paramètre | Type | Description |
|-----------|------|-------------|
| `id` | `integer` | ID de l'item dans le panier (CartItem.Id) |

#### 📥 Request Body

Aucun body requis.

#### 📤 Réponse

**Succès:** `200 OK`

Retourne le panier complet mis à jour (sans l'item supprimé).

**Erreurs:**

```json
// Item introuvable
{
  "status": 404,
  "title": "Introuvable",
  "detail": "Item introuvable.",
  "instance": "/api/cart/items/156",
  "traceId": "00-abc123..."
}
```

#### 💡 Exemples

**Supprimer un article:**
```bash
curl -X DELETE https://api.example.com/api/cart/items/156 \
  -H "Authorization: Bearer <token>"
```

**Exemple JavaScript:**
```javascript
async function deleteCartItem(itemId) {
  const response = await fetch(`/api/cart/items/${itemId}`, {
    method: 'DELETE',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  if (!response.ok) {
    throw new Error('Erreur lors de la suppression');
  }
  
  return await response.json();
}
```

#### 📌 Comportements importants

1. **Recalcul automatique** : Les totaux sont recalculés après suppression.

2. **Retour complet** : Le panier complet est retourné, permettant de mettre à jour l'interface immédiatement.

3. **Confirmation recommandée** : Pour une meilleure UX, demandez confirmation avant suppression (sauf pour les boutons "-" qui décrémentent).

---

## 5. Vider le Panier

### `POST /api/cart/clear`

Vide complètement le panier, supprimant tous les articles.

#### 🔒 Autorisation
- Rôle requis: **User** ou **Admin** (authentifié)

#### 📥 Request Body

Aucun body requis.

#### 📤 Réponse

**Succès:** `200 OK`

Retourne le panier vide.

```json
{
  "id": 42,
  "currency": "TND",
  "subtotal": 0.000,
  "discountTotal": 0.000,
  "deliveryFee": 0.000,
  "total": 0.000,
  "items": [],
  "updatedAt": "2026-02-11T15:30:00Z"
}
```

#### 💡 Exemples

**Vider le panier:**
```bash
curl -X POST https://api.example.com/api/cart/clear \
  -H "Authorization: Bearer <token>"
```

**Exemple JavaScript:**
```javascript
async function clearCart() {
  // Demander confirmation
  if (!confirm('Êtes-vous sûr de vouloir vider votre panier ?')) {
    return;
  }
  
  const response = await fetch('/api/cart/clear', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  if (!response.ok) {
    throw new Error('Erreur lors du vidage du panier');
  }
  
  return await response.json();
}
```

#### 📌 Cas d'utilisation

1. **Bouton "Vider le panier"** : Action manuelle de l'utilisateur
2. **Après commande réussie** : Automatique après création de commande
3. **Changement de contexte** : Par exemple, changement de devise ou de région
4. **Nettoyage administratif** : Opérations de maintenance

---

## Modèles de Données

### CartDto

Structure complète du panier.

```typescript
interface CartDto {
  id: number;                    // ID du panier
  currency: string;              // "TND"
  subtotal: number;              // Somme des lineTotals
  discountTotal: number;         // Total des réductions (actuellement 0)
  deliveryFee: number;           // Frais de livraison (actuellement 0)
  total: number;                 // Subtotal - Discount + Delivery
  items: CartItemDto[];          // Liste des articles
  updatedAt: string;             // ISO 8601 UTC - dernière modification
}
```

### CartItemDto

Article dans le panier.

```typescript
interface CartItemDto {
  id: number;                    // ID de l'item dans le panier (pour update/delete)
  articleId: number;             // ID de l'article d'origine
  variantId: number | null;      // ID de la variante (null si aucune)
  name: string;                  // Nom de l'article (snapshot)
  variantName: string | null;    // Nom de la variante (ex: "Rouge - M")
  sku: string | null;            // SKU du produit
  imageUrl: string | null;       // URL de l'image
  unitPrice: number;             // Prix unitaire actuel (TND)
  quantity: number;              // Quantité (1-999)
  lineTotal: number;             // unitPrice × quantity
}
```

### AddCartItemDto

Payload pour ajouter un article.

```typescript
interface AddCartItemDto {
  articleId: number;             // ID de l'article (requis)
  variantId?: number | null;     // ID de la variante (optionnel)
  quantity: number;              // Quantité (1-999, défaut: 1)
}
```

### UpdateCartItemDto

Payload pour modifier la quantité.

```typescript
interface UpdateCartItemDto {
  quantity: number;              // Nouvelle quantité (1-999)
}
```

---

## Gestion des Erreurs

### Format de réponse d'erreur

Toutes les erreurs suivent le standard RFC 7807 (Problem Details):

```json
{
  "status": 400,
  "title": "Titre de l'erreur",
  "detail": "Description détaillée de l'erreur",
  "instance": "/api/cart/items",
  "traceId": "00-abc123def456...",
  "errors": {
    "field": ["Message d'erreur de validation"]
  }
}
```

### Codes HTTP

| Code | Signification | Description |
|------|---------------|-------------|
| `200` | OK | Requête réussie |
| `400` | Bad Request | Données invalides ou article indisponible |
| `401` | Unauthorized | Token manquant ou invalide |
| `404` | Not Found | Item introuvable dans le panier |
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

**Article indisponible:**
```json
{
  "status": 400,
  "title": "Requête invalide",
  "detail": "Produit indisponible."
}
```

**Item non trouvé:**
```json
{
  "status": 404,
  "title": "Introuvable",
  "detail": "Item introuvable."
}
```

---

## Flux et Scénarios

### Scénario 1: Première utilisation

```
1. Utilisateur se connecte
   └─ GET /api/cart
   └─ Panier créé automatiquement (vide)

2. Parcourt catalogue
   └─ Clique "Ajouter au panier"
   └─ POST /api/cart/items
   └─ Item ajouté + panier retourné

3. Continue shopping
   └─ Ajoute même article différente taille
   └─ POST /api/cart/items
   └─ Nouvel item créé (variante différente)

4. Visualise panier
   └─ Affichage des items avec totaux
```

### Scénario 2: Modification de quantités

```
1. Utilisateur dans le panier
   └─ Clique "+1" sur un article
   └─ PUT /api/cart/items/{id} (quantity: currentQty + 1)

2. Clique "-1" sur un article
   └─ Si qty > 1 : PUT /api/cart/items/{id}
   └─ Si qty = 1 : DELETE /api/cart/items/{id}

3. Saisie manuelle de quantité
   └─ PUT /api/cart/items/{id} (quantity: inputValue)
```

### Scénario 3: Gestion des changements de prix

```
1. Article dans panier : 50 TND
2. Admin change prix → 45 TND
3. Utilisateur refresh panier
   └─ GET /api/cart
   └─ Recalcul automatique ✅
   └─ Prix mis à jour : 45 TND
   └─ Total recalculé
```

### Scénario 4: Article devient indisponible

```
1. Utilisateur a article A dans panier
2. Admin désactive article A
3. Utilisateur refresh panier
   └─ GET /api/cart
   └─ Recalcul automatique ✅
   └─ Article A retiré du panier
   └─ Notification à l'utilisateur recommandée
```

### Scénario 5: Passage en commande

```
1. Utilisateur valide panier
   └─ Redirection vers checkout

2. Remplit informations livraison
   └─ Utilise données du panier

3. Crée commande
   └─ POST /api/orders
   └─ Items du panier → OrderItems

4. Commande créée
   └─ POST /api/cart/clear (automatique)
   └─ Panier vidé ✅
```

### Scénario 6: Multi-appareils

```
1. Mobile : Ajoute 3 articles
   └─ POST /api/cart/items (×3)

2. Desktop : Se connecte
   └─ GET /api/cart
   └─ Affiche les 3 articles ✅

3. Desktop : Modifie quantité
   └─ PUT /api/cart/items/{id}

4. Mobile : Refresh
   └─ GET /api/cart
   └─ Quantité à jour ✅
```

---

## Implémentation Frontend

### 1. Context/Store React avec Cart API

```javascript
// CartContext.jsx
import React, { createContext, useContext, useState, useEffect } from 'react';

const CartContext = createContext();

export function CartProvider({ children, token }) {
  const [cart, setCart] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Charger le panier au montage
  useEffect(() => {
    if (token) {
      fetchCart();
    }
  }, [token]);

  // Récupérer le panier
  const fetchCart = async () => {
    try {
      setLoading(true);
      const response = await fetch('/api/cart', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (!response.ok) throw new Error('Erreur chargement panier');
      
      const data = await response.json();
      setCart(data);
      setError(null);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  // Ajouter un article
  const addItem = async (articleId, variantId, quantity = 1) => {
    try {
      const response = await fetch('/api/cart/items', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ articleId, variantId, quantity })
      });
      
      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.detail || 'Erreur ajout article');
      }
      
      const data = await response.json();
      setCart(data);
      
      // Notification succès
      showNotification('Article ajouté au panier', 'success');
      
      return data;
    } catch (err) {
      setError(err.message);
      showNotification(err.message, 'error');
      throw err;
    }
  };

  // Mettre à jour la quantité
  const updateQuantity = async (itemId, quantity) => {
    try {
      const response = await fetch(`/api/cart/items/${itemId}`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ quantity })
      });
      
      if (!response.ok) throw new Error('Erreur mise à jour');
      
      const data = await response.json();
      setCart(data);
      
      return data;
    } catch (err) {
      setError(err.message);
      throw err;
    }
  };

  // Supprimer un article
  const removeItem = async (itemId) => {
    try {
      const response = await fetch(`/api/cart/items/${itemId}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (!response.ok) throw new Error('Erreur suppression');
      
      const data = await response.json();
      setCart(data);
      
      showNotification('Article retiré du panier', 'info');
      
      return data;
    } catch (err) {
      setError(err.message);
      throw err;
    }
  };

  // Vider le panier
  const clearCart = async () => {
    try {
      const response = await fetch('/api/cart/clear', {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (!response.ok) throw new Error('Erreur vidage panier');
      
      const data = await response.json();
      setCart(data);
      
      showNotification('Panier vidé', 'info');
      
      return data;
    } catch (err) {
      setError(err.message);
      throw err;
    }
  };

  // Helpers
  const itemCount = cart?.items.length || 0;
  const totalAmount = cart?.total || 0;

  return (
    <CartContext.Provider value={{
      cart,
      loading,
      error,
      itemCount,
      totalAmount,
      fetchCart,
      addItem,
      updateQuantity,
      removeItem,
      clearCart
    }}>
      {children}
    </CartContext.Provider>
  );
}

export function useCart() {
  const context = useContext(CartContext);
  if (!context) {
    throw new Error('useCart must be used within CartProvider');
  }
  return context;
}

// Helper notification (à adapter selon votre système)
function showNotification(message, type) {
  // toast, snackbar, etc.
  console.log(`[${type}] ${message}`);
}
```

### 2. Composant Bouton "Ajouter au Panier"

```javascript
// AddToCartButton.jsx
import React, { useState } from 'react';
import { useCart } from './CartContext';

export function AddToCartButton({ article, selectedVariant }) {
  const { addItem } = useCart();
  const [isAdding, setIsAdding] = useState(false);

  const handleAddToCart = async () => {
    try {
      setIsAdding(true);
      
      await addItem(
        article.id,
        selectedVariant?.id || null,
        1
      );
      
      // Animation succès
      // Optionnel : ouvrir mini-panier pendant 3 secondes
    } catch (error) {
      console.error('Erreur ajout panier:', error);
    } finally {
      setIsAdding(false);
    }
  };

  return (
    <button
      onClick={handleAddToCart}
      disabled={isAdding || !article.isActive}
      className="add-to-cart-btn"
    >
      {isAdding ? (
        <>
          <span className="spinner" />
          Ajout en cours...
        </>
      ) : (
        <>
          <span className="cart-icon">🛒</span>
          Ajouter au panier
        </>
      )}
    </button>
  );
}
```

### 3. Composant Page Panier

```javascript
// CartPage.jsx
import React from 'react';
import { useCart } from './CartContext';
import { CartItem } from './CartItem';
import { Link } from 'react-router-dom';

export function CartPage() {
  const { cart, loading, itemCount, clearCart } = useCart();

  if (loading) {
    return <div className="loading">Chargement du panier...</div>;
  }

  if (!cart || itemCount === 0) {
    return (
      <div className="empty-cart">
        <h2>Votre panier est vide</h2>
        <p>Découvrez nos produits et commencez vos achats !</p>
        <Link to="/produits" className="btn-primary">
          Voir les produits
        </Link>
      </div>
    );
  }

  const handleClearCart = async () => {
    if (window.confirm('Êtes-vous sûr de vouloir vider votre panier ?')) {
      await clearCart();
    }
  };

  return (
    <div className="cart-page">
      <div className="cart-header">
        <h1>Mon Panier ({itemCount} article{itemCount > 1 ? 's' : ''})</h1>
        <button onClick={handleClearCart} className="btn-text">
          Vider le panier
        </button>
      </div>

      <div className="cart-content">
        <div className="cart-items">
          {cart.items.map(item => (
            <CartItem key={item.id} item={item} />
          ))}
        </div>

        <div className="cart-summary">
          <h3>Récapitulatif</h3>
          
          <div className="summary-line">
            <span>Sous-total</span>
            <span>{cart.subtotal.toFixed(3)} {cart.currency}</span>
          </div>

          {cart.discountTotal > 0 && (
            <div className="summary-line discount">
              <span>Réduction</span>
              <span>-{cart.discountTotal.toFixed(3)} {cart.currency}</span>
            </div>
          )}

          {cart.deliveryFee > 0 && (
            <div className="summary-line">
              <span>Livraison</span>
              <span>{cart.deliveryFee.toFixed(3)} {cart.currency}</span>
            </div>
          )}

          <div className="summary-line total">
            <span>Total</span>
            <span className="amount">{cart.total.toFixed(3)} {cart.currency}</span>
          </div>

          <Link to="/checkout" className="btn-primary btn-block">
            Passer la commande
          </Link>

          <Link to="/produits" className="btn-secondary btn-block">
            Continuer mes achats
          </Link>
        </div>
      </div>
    </div>
  );
}
```

### 4. Composant Item du Panier

```javascript
// CartItem.jsx
import React, { useState } from 'react';
import { useCart } from './CartContext';

export function CartItem({ item }) {
  const { updateQuantity, removeItem } = useCart();
  const [isUpdating, setIsUpdating] = useState(false);

  const handleQuantityChange = async (newQuantity) => {
    if (newQuantity < 1 || newQuantity > 999) return;
    
    try {
      setIsUpdating(true);
      await updateQuantity(item.id, newQuantity);
    } finally {
      setIsUpdating(false);
    }
  };

  const handleIncrement = () => {
    handleQuantityChange(item.quantity + 1);
  };

  const handleDecrement = () => {
    if (item.quantity === 1) {
      handleRemove();
    } else {
      handleQuantityChange(item.quantity - 1);
    }
  };

  const handleRemove = async () => {
    if (window.confirm('Retirer cet article du panier ?')) {
      await removeItem(item.id);
    }
  };

  return (
    <div className="cart-item">
      <div className="item-image">
        <img src={item.imageUrl || '/placeholder.jpg'} alt={item.name} />
      </div>

      <div className="item-details">
        <h4>{item.name}</h4>
        {item.variantName && (
          <p className="variant">{item.variantName}</p>
        )}
        {item.sku && (
          <p className="sku">SKU: {item.sku}</p>
        )}
      </div>

      <div className="item-price">
        <span className="unit-price">{item.unitPrice.toFixed(3)} TND</span>
      </div>

      <div className="item-quantity">
        <button
          onClick={handleDecrement}
          disabled={isUpdating}
          className="qty-btn"
          aria-label="Diminuer"
        >
          {item.quantity === 1 ? '🗑️' : '-'}
        </button>
        
        <input
          type="number"
          value={item.quantity}
          onChange={(e) => handleQuantityChange(parseInt(e.target.value) || 1)}
          min="1"
          max="999"
          disabled={isUpdating}
          className="qty-input"
        />
        
        <button
          onClick={handleIncrement}
          disabled={isUpdating || item.quantity >= 999}
          className="qty-btn"
          aria-label="Augmenter"
        >
          +
        </button>
      </div>

      <div className="item-total">
        <span className="line-total">{item.lineTotal.toFixed(3)} TND</span>
      </div>

      <button
        onClick={handleRemove}
        className="btn-remove"
        aria-label="Supprimer"
        title="Supprimer l'article"
      >
        ✕
      </button>
    </div>
  );
}
```

### 5. Badge Compteur Panier (Header)

```javascript
// CartBadge.jsx
import React from 'react';
import { Link } from 'react-router-dom';
import { useCart } from './CartContext';

export function CartBadge() {
  const { itemCount, totalAmount } = useCart();

  return (
    <Link to="/panier" className="cart-badge">
      <span className="cart-icon">🛒</span>
      {itemCount > 0 && (
        <span className="badge">{itemCount}</span>
      )}
      <span className="cart-total">
        {totalAmount.toFixed(3)} TND
      </span>
    </Link>
  );
}
```

### 6. Mini-Panier (Dropdown)

```javascript
// MiniCart.jsx
import React from 'react';
import { Link } from 'react-router-dom';
import { useCart } from './CartContext';

export function MiniCart({ isOpen, onClose }) {
  const { cart, itemCount } = useCart();

  if (!isOpen) return null;

  if (!cart || itemCount === 0) {
    return (
      <div className="mini-cart">
        <div className="mini-cart-header">
          <h3>Panier</h3>
          <button onClick={onClose}>✕</button>
        </div>
        <div className="mini-cart-empty">
          <p>Votre panier est vide</p>
        </div>
      </div>
    );
  }

  return (
    <div className="mini-cart">
      <div className="mini-cart-header">
        <h3>Panier ({itemCount})</h3>
        <button onClick={onClose}>✕</button>
      </div>

      <div className="mini-cart-items">
        {cart.items.slice(0, 3).map(item => (
          <div key={item.id} className="mini-cart-item">
            <img src={item.imageUrl || '/placeholder.jpg'} alt={item.name} />
            <div className="item-info">
              <h4>{item.name}</h4>
              {item.variantName && <p>{item.variantName}</p>}
              <p>{item.quantity} × {item.unitPrice.toFixed(3)} TND</p>
            </div>
          </div>
        ))}

        {itemCount > 3 && (
          <p className="more-items">+ {itemCount - 3} autre(s) article(s)</p>
        )}
      </div>

      <div className="mini-cart-total">
        <span>Total</span>
        <span>{cart.total.toFixed(3)} {cart.currency}</span>
      </div>

      <div className="mini-cart-actions">
        <Link to="/panier" className="btn-secondary" onClick={onClose}>
          Voir le panier
        </Link>
        <Link to="/checkout" className="btn-primary" onClick={onClose}>
          Commander
        </Link>
      </div>
    </div>
  );
}
```

---

## Bonnes Pratiques

### 1. **Synchronisation immédiate**

```javascript
// ✅ BON : Synchroniser immédiatement après chaque action
const handleAddToCart = async () => {
  const updatedCart = await addItem(articleId, variantId, 1);
  // Le state est déjà mis à jour par le context
  // L'UI reflète immédiatement les changements
};

// ❌ MAUVAIS : Gérer le panier localement puis sync plus tard
const handleAddToCart = () => {
  // NE PAS FAIRE : gérer le panier côté client uniquement
  localCart.push(item);
  // Risque de désynchronisation
};
```

### 2. **Gestion des erreurs gracieuse**

```javascript
const addItem = async (articleId, variantId, quantity) => {
  try {
    const response = await fetch('/api/cart/items', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ articleId, variantId, quantity })
    });
    
    if (!response.ok) {
      const error = await response.json();
      
      // Gérer les cas spécifiques
      if (error.detail.includes('indisponible')) {
        showNotification('Ce produit n\'est plus disponible', 'warning');
        // Rediriger vers la page produit pour voir les alternatives
      } else {
        showNotification(error.detail, 'error');
      }
      
      throw new Error(error.detail);
    }
    
    return await response.json();
  } catch (err) {
    // Log pour debug
    console.error('Erreur ajout panier:', err);
    throw err;
  }
};
```

### 3. **Debounce pour modifications quantité**

```javascript
import { debounce } from 'lodash';

// Éviter trop d'appels API lors de la saisie manuelle
const debouncedUpdateQuantity = debounce(async (itemId, quantity) => {
  await updateQuantity(itemId, quantity);
}, 500);

const handleQuantityInput = (e) => {
  const newQty = parseInt(e.target.value) || 1;
  debouncedUpdateQuantity(item.id, newQty);
};
```

### 4. **Feedback visuel pendant chargement**

```javascript
// Optimistic UI : montrer le changement immédiatement
const handleIncrement = async () => {
  // Mettre à jour l'UI immédiatement
  setOptimisticQuantity(quantity + 1);
  
  try {
    // Appeler l'API
    await updateQuantity(item.id, quantity + 1);
    // Success : le cart context met à jour l'état
  } catch (error) {
    // Erreur : revenir à la valeur précédente
    setOptimisticQuantity(quantity);
    showNotification('Erreur mise à jour', 'error');
  }
};
```

### 5. **Persistance entre sessions**

```javascript
// Le panier est automatiquement persisté côté serveur
// Pas besoin de localStorage !

useEffect(() => {
  // Charger le panier à chaque connexion
  if (isAuthenticated && token) {
    fetchCart();
  }
}, [isAuthenticated, token]);
```

### 6. **Notifications contextuelles**

```javascript
const addItem = async (articleId, variantId, quantity) => {
  const updatedCart = await addItemToCart(articleId, variantId, quantity);
  
  // Notification avec action
  showNotification(
    'Article ajouté au panier',
    'success',
    {
      action: {
        label: 'Voir le panier',
        onClick: () => navigate('/panier')
      },
      duration: 3000
    }
  );
  
  return updatedCart;
};
```

### 7. **Vérification avant checkout**

```javascript
const handleCheckout = async () => {
  // Rafraîchir le panier pour avoir les prix à jour
  await fetchCart();
  
  // Vérifier que le panier n'est pas vide
  if (itemCount === 0) {
    showNotification('Votre panier est vide', 'warning');
    return;
  }
  
  // Vérifier disponibilité des articles
  const unavailableItems = cart.items.filter(item => !item.isActive);
  if (unavailableItems.length > 0) {
    showNotification(
      'Certains articles ne sont plus disponibles. Veuillez les retirer.',
      'warning'
    );
    return;
  }
  
  // OK : rediriger vers checkout
  navigate('/checkout');
};
```

### 8. **Analytics et tracking**

```javascript
const addItem = async (articleId, variantId, quantity) => {
  const updatedCart = await addItemToCart(articleId, variantId, quantity);
  
  // Track pour analytics
  trackEvent('cart_add_item', {
    article_id: articleId,
    variant_id: variantId,
    quantity: quantity,
    cart_total: updatedCart.total,
    cart_item_count: updatedCart.items.length
  });
  
  return updatedCart;
};
```

---

## 📞 Support

Pour toute question ou problème:

1. Vérifiez le `traceId` dans la réponse d'erreur
2. Consultez les logs serveur avec le `traceId`
3. Vérifiez que l'article existe et est actif
4. Contactez l'équipe backend avec les détails complets de la requête

---

## 📊 Résumé des Endpoints

| Méthode | Endpoint | Description | Retour |
|---------|----------|-------------|--------|
| `GET` | `/api/cart` | Obtenir le panier | CartDto |
| `POST` | `/api/cart/items` | Ajouter un article | CartDto |
| `PUT` | `/api/cart/items/{id}` | Modifier la quantité | CartDto |
| `DELETE` | `/api/cart/items/{id}` | Supprimer un article | CartDto |
| `POST` | `/api/cart/clear` | Vider le panier | CartDto |

**Tous les endpoints retournent le panier complet mis à jour !**

---

## ✅ Checklist d'intégration Frontend

- [ ] Créer un Context/Store pour gérer le state du panier
- [ ] Implémenter les 5 méthodes d'API (get, add, update, delete, clear)
- [ ] Ajouter un badge compteur dans le header
- [ ] Créer la page panier complète
- [ ] Ajouter des boutons "Ajouter au panier" sur les pages produits
- [ ] Gérer les erreurs avec notifications appropriées
- [ ] Implémenter un mini-panier (dropdown) optionnel
- [ ] Ajouter des animations de feedback
- [ ] Synchroniser le panier après connexion
- [ ] Vider le panier après commande réussie
- [ ] Ajouter du tracking analytics
- [ ] Tester sur plusieurs appareils (synchronisation)
- [ ] Gérer les cas limites (produits indisponibles, etc.)

---

**Dernière mise à jour:** 11 février 2026  
**Version API:** 1.0  
**Maintenu par:** Équipe Backend Hlouwa
