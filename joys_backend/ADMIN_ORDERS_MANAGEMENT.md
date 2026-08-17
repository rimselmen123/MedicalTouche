# 👨‍💼 Guide Admin - Gestion des Commandes

> Guide complet pour l'interface d'administration de gestion des commandes avec exemples d'implémentation frontend.

**Audience:** Équipe Frontend - Interface Admin

**Date:** 11 février 2026

---

## 📋 Table des matières

1. [Vue d'ensemble](#vue-densemble)
2. [Architecture de l'interface Admin](#architecture-de-linterface-admin)
3. [Dashboard Principal](#dashboard-principal)
4. [Liste des Commandes](#liste-des-commandes)
5. [Détails d'une Commande](#détails-dune-commande)
6. [Gestion des Statuts](#gestion-des-statuts)
7. [Filtres et Recherche](#filtres-et-recherche)
8. [Workflows de Traitement](#workflows-de-traitement)
9. [Notifications et Alertes](#notifications-et-alertes)
10. [Rapports et Statistiques](#rapports-et-statistiques)
11. [Implémentation Frontend Complète](#implémentation-frontend-complète)
12. [Bonnes Pratiques Admin](#bonnes-pratiques-admin)

---

## Vue d'ensemble

### Objectif de l'interface Admin

L'interface admin permet de :
- ✅ **Visualiser** toutes les commandes en temps réel
- ✅ **Suivre** l'évolution des commandes (statuts, paiements)
- ✅ **Gérer** le traitement des commandes (préparation → livraison)
- ✅ **Modifier** les frais de livraison et réductions
- ✅ **Rechercher** et filtrer les commandes efficacement
- ✅ **Analyser** les statistiques de vente

### Endpoints Admin disponibles

| Endpoint | Méthode | Description |
|----------|---------|-------------|
| `/api/orders` | GET | Liste paginée avec filtres avancés |
| `/api/orders/{id}` | GET | Détails complets d'une commande |
| `/api/orders/{id}/status` | PUT | Changer le statut |
| `/api/orders/{id}/fees` | PUT | Modifier frais et réductions |

> 📖 **Référence complète :** Voir [ORDER_API_DOCUMENTATION.md](ORDER_API_DOCUMENTATION.md) pour tous les détails des endpoints.

---

## Architecture de l'interface Admin

### Structure recommandée

```
┌─────────────────────────────────────────────────────┐
│                  Admin Layout                        │
│  ┌──────────┐  ┌─────────────────────────────────┐ │
│  │          │  │                                 │ │
│  │ Sidebar  │  │     Main Content Area           │ │
│  │          │  │                                 │ │
│  │ - Home   │  │  ┌──────────────────────────┐  │ │
│  │ - Orders │  │  │   Dashboard / Stats      │  │ │
│  │ - Stock  │  │  └──────────────────────────┘  │ │
│  │ - Users  │  │                                 │ │
│  │ - Reports│  │  ┌──────────────────────────┐  │ │
│  │          │  │  │   Orders List / Filters  │  │ │
│  │          │  │  │   (Table with actions)   │  │ │
│  │          │  │  └──────────────────────────┘  │ │
│  │          │  │                                 │ │
│  │          │  │  ┌──────────────────────────┐  │ │
│  │          │  │  │   Order Details Modal    │  │ │
│  │          │  │  │   (Full info + actions)  │  │ │
│  │          │  │  └──────────────────────────┘  │ │
│  └──────────┘  └─────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

### Navigation recommandée

```
Admin Home
  └─ Orders Management
      ├─ Dashboard (stats + charts)
      ├─ All Orders (liste complète)
      ├─ Pending Orders (en attente)
      ├─ Processing Orders (en traitement)
      └─ Completed Orders (terminées)
```

---

## Dashboard Principal

### Composants du Dashboard

#### 1. **Cartes de statistiques en temps réel**

```javascript
// OrdersDashboard.jsx
import React, { useState, useEffect } from 'react';

export function OrdersDashboard() {
  const [stats, setStats] = useState({
    pending: 0,
    awaitingPayment: 0,
    preparing: 0,
    outForDelivery: 0,
    todayOrders: 0,
    todayRevenue: 0,
    weekRevenue: 0,
    monthRevenue: 0
  });

  useEffect(() => {
    fetchDashboardStats();
    
    // Refresh toutes les 30 secondes
    const interval = setInterval(fetchDashboardStats, 30000);
    return () => clearInterval(interval);
  }, []);

  const fetchDashboardStats = async () => {
    try {
      // Requêtes parallèles pour performance
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      
      const weekAgo = new Date(today);
      weekAgo.setDate(weekAgo.getDate() - 7);
      
      const monthAgo = new Date(today);
      monthAgo.setMonth(monthAgo.getMonth() - 1);

      const [pending, awaiting, preparing, delivery, todayData, weekData, monthData] = 
        await Promise.all([
          fetchOrders({ status: 0, pageSize: 1 }),
          fetchOrders({ status: 1, pageSize: 1 }),
          fetchOrders({ status: 3, pageSize: 1 }),
          fetchOrders({ status: 5, pageSize: 1 }),
          fetchOrders({ from: today.toISOString(), pageSize: 100 }),
          fetchOrders({ from: weekAgo.toISOString(), pageSize: 1000 }),
          fetchOrders({ from: monthAgo.toISOString(), pageSize: 1000 })
        ]);

      setStats({
        pending: pending.totalCount,
        awaitingPayment: awaiting.totalCount,
        preparing: preparing.totalCount,
        outForDelivery: delivery.totalCount,
        todayOrders: todayData.totalCount,
        todayRevenue: todayData.items.reduce((sum, o) => sum + o.total, 0),
        weekRevenue: weekData.items.reduce((sum, o) => sum + o.total, 0),
        monthRevenue: monthData.items.reduce((sum, o) => sum + o.total, 0)
      });
    } catch (error) {
      console.error('Erreur chargement stats:', error);
    }
  };

  const fetchOrders = async (params) => {
    const queryString = new URLSearchParams(params).toString();
    const response = await fetch(`/api/orders?${queryString}`, {
      headers: { 'Authorization': `Bearer ${adminToken}` }
    });
    return await response.json();
  };

  return (
    <div className="dashboard">
      <h1>Tableau de bord - Commandes</h1>

      {/* Cartes prioritaires */}
      <div className="stats-grid priority">
        <StatCard
          title="En attente"
          value={stats.pending}
          color="orange"
          icon="⏳"
          urgent={stats.pending > 5}
          onClick={() => navigateTo('/admin/orders?status=0')}
        />
        
        <StatCard
          title="Attente paiement"
          value={stats.awaitingPayment}
          color="yellow"
          icon="💳"
          urgent={stats.awaitingPayment > 10}
          onClick={() => navigateTo('/admin/orders?status=1')}
        />
        
        <StatCard
          title="En préparation"
          value={stats.preparing}
          color="blue"
          icon="📦"
          onClick={() => navigateTo('/admin/orders?status=3')}
        />
        
        <StatCard
          title="En livraison"
          value={stats.outForDelivery}
          color="purple"
          icon="🚚"
          onClick={() => navigateTo('/admin/orders?status=5')}
        />
      </div>

      {/* Statistiques financières */}
      <div className="stats-grid financial">
        <StatCard
          title="Aujourd'hui"
          value={`${stats.todayOrders} commandes`}
          subtitle={`${stats.todayRevenue.toFixed(3)} TND`}
          color="green"
          icon="📊"
        />
        
        <StatCard
          title="Cette semaine"
          value={`${stats.weekRevenue.toFixed(3)} TND`}
          color="teal"
          icon="📈"
        />
        
        <StatCard
          title="Ce mois"
          value={`${stats.monthRevenue.toFixed(3)} TND`}
          color="indigo"
          icon="💰"
        />
      </div>

      {/* Actions rapides */}
      <div className="quick-actions">
        <h2>Actions rapides</h2>
        <button onClick={() => navigateTo('/admin/orders?status=0')}>
          Traiter les commandes en attente
        </button>
        <button onClick={() => navigateTo('/admin/orders?status=1')}>
          Vérifier les paiements en attente
        </button>
        <button onClick={() => navigateTo('/admin/stock/reservations?status=0')}>
          Gérer les réservations de stock
        </button>
      </div>

      {/* Commandes récentes */}
      <RecentOrdersList limit={10} />
    </div>
  );
}

function StatCard({ title, value, subtitle, color, icon, urgent, onClick }) {
  return (
    <div
      className={`stat-card ${color} ${urgent ? 'urgent' : ''} ${onClick ? 'clickable' : ''}`}
      onClick={onClick}
    >
      <div className="stat-icon">{icon}</div>
      <div className="stat-content">
        <h3>{title}</h3>
        <div className="stat-value">{value}</div>
        {subtitle && <div className="stat-subtitle">{subtitle}</div>}
        {urgent && <span className="urgent-badge">⚠️ Urgent</span>}
      </div>
    </div>
  );
}
```

#### 2. **Graphique d'évolution des commandes**

```javascript
// OrdersChart.jsx
import React, { useState, useEffect } from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend } from 'recharts';

export function OrdersChart({ period = 'week' }) {
  const [data, setData] = useState([]);

  useEffect(() => {
    fetchChartData();
  }, [period]);

  const fetchChartData = async () => {
    // Récupérer les données des derniers jours
    const days = period === 'week' ? 7 : 30;
    const promises = [];

    for (let i = days - 1; i >= 0; i--) {
      const date = new Date();
      date.setDate(date.getDate() - i);
      date.setHours(0, 0, 0, 0);
      
      const nextDate = new Date(date);
      nextDate.setDate(nextDate.getDate() + 1);

      promises.push(
        fetch(`/api/orders?from=${date.toISOString()}&to=${nextDate.toISOString()}&pageSize=1000`, {
          headers: { 'Authorization': `Bearer ${adminToken}` }
        }).then(r => r.json())
      );
    }

    const results = await Promise.all(promises);
    
    const chartData = results.map((result, index) => {
      const date = new Date();
      date.setDate(date.getDate() - (days - 1 - index));
      
      return {
        date: date.toLocaleDateString('fr-TN', { day: '2-digit', month: 'short' }),
        commandes: result.totalCount,
        montant: result.items.reduce((sum, o) => sum + o.total, 0)
      };
    });

    setData(chartData);
  };

  return (
    <div className="chart-container">
      <h2>Évolution des commandes - {period === 'week' ? 'Semaine' : 'Mois'}</h2>
      <LineChart width={800} height={300} data={data}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="date" />
        <YAxis yAxisId="left" />
        <YAxis yAxisId="right" orientation="right" />
        <Tooltip />
        <Legend />
        <Line yAxisId="left" type="monotone" dataKey="commandes" stroke="#8884d8" name="Nombre de commandes" />
        <Line yAxisId="right" type="monotone" dataKey="montant" stroke="#82ca9d" name="Montant (TND)" />
      </LineChart>
    </div>
  );
}
```

---

## Liste des Commandes

### Interface complète avec filtres et actions

```javascript
// OrdersListPage.jsx
import React, { useState, useEffect } from 'react';

export function OrdersListPage() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 20,
    totalCount: 0
  });
  
  const [filters, setFilters] = useState({
    status: null,
    paymentStatus: null,
    paymentMethod: null,
    search: '',
    from: null,
    to: null
  });

  useEffect(() => {
    fetchOrders();
  }, [pagination.page, filters]);

  const fetchOrders = async () => {
    try {
      setLoading(true);
      
      const params = {
        page: pagination.page,
        pageSize: pagination.pageSize,
        ...filters
      };
      
      // Nettoyer les paramètres null
      Object.keys(params).forEach(key => 
        (params[key] === null || params[key] === '') && delete params[key]
      );

      const queryString = new URLSearchParams(params).toString();
      const response = await fetch(`/api/orders?${queryString}`, {
        headers: { 'Authorization': `Bearer ${adminToken}` }
      });

      if (!response.ok) throw new Error('Erreur chargement commandes');

      const data = await response.json();
      
      setOrders(data.items);
      setPagination(prev => ({
        ...prev,
        totalCount: data.totalCount
      }));
    } catch (error) {
      console.error('Erreur:', error);
      showNotification('Erreur chargement des commandes', 'error');
    } finally {
      setLoading(false);
    }
  };

  const handleFilterChange = (field, value) => {
    setFilters(prev => ({ ...prev, [field]: value }));
    setPagination(prev => ({ ...prev, page: 1 })); // Reset à la page 1
  };

  const handlePageChange = (newPage) => {
    setPagination(prev => ({ ...prev, page: newPage }));
  };

  return (
    <div className="orders-list-page">
      <div className="page-header">
        <h1>Gestion des Commandes</h1>
        <div className="header-actions">
          <button onClick={fetchOrders} className="btn-refresh">
            🔄 Actualiser
          </button>
          <ExportOrdersButton filters={filters} />
        </div>
      </div>

      {/* Filtres */}
      <OrdersFilters
        filters={filters}
        onFilterChange={handleFilterChange}
        onReset={() => setFilters({
          status: null,
          paymentStatus: null,
          paymentMethod: null,
          search: '',
          from: null,
          to: null
        })}
      />

      {/* Statistiques rapides */}
      <div className="quick-stats">
        <span>Total: <strong>{pagination.totalCount}</strong> commandes</span>
        <span>Page {pagination.page} sur {Math.ceil(pagination.totalCount / pagination.pageSize)}</span>
      </div>

      {/* Table des commandes */}
      {loading ? (
        <div className="loading">Chargement...</div>
      ) : orders.length === 0 ? (
        <div className="empty-state">
          <p>Aucune commande trouvée</p>
        </div>
      ) : (
        <>
          <OrdersTable
            orders={orders}
            onRefresh={fetchOrders}
          />

          {/* Pagination */}
          <Pagination
            currentPage={pagination.page}
            pageSize={pagination.pageSize}
            totalCount={pagination.totalCount}
            onPageChange={handlePageChange}
          />
        </>
      )}
    </div>
  );
}
```

### Composant Filtres

```javascript
// OrdersFilters.jsx
import React from 'react';

export function OrdersFilters({ filters, onFilterChange, onReset }) {
  return (
    <div className="filters-panel">
      <h3>Filtres</h3>

      <div className="filters-grid">
        {/* Recherche */}
        <div className="filter-group">
          <label>Rechercher</label>
          <input
            type="text"
            placeholder="Numéro, email, nom, téléphone..."
            value={filters.search}
            onChange={(e) => onFilterChange('search', e.target.value)}
            className="search-input"
          />
        </div>

        {/* Statut commande */}
        <div className="filter-group">
          <label>Statut commande</label>
          <select
            value={filters.status ?? ''}
            onChange={(e) => onFilterChange('status', e.target.value ? parseInt(e.target.value) : null)}
          >
            <option value="">Tous</option>
            <option value="0">En attente</option>
            <option value="1">Attente paiement</option>
            <option value="2">Payée</option>
            <option value="3">En préparation</option>
            <option value="4">Prête</option>
            <option value="5">En livraison</option>
            <option value="6">Livrée</option>
            <option value="7">Annulée</option>
            <option value="8">Remboursée</option>
          </select>
        </div>

        {/* Statut paiement */}
        <div className="filter-group">
          <label>Statut paiement</label>
          <select
            value={filters.paymentStatus ?? ''}
            onChange={(e) => onFilterChange('paymentStatus', e.target.value ? parseInt(e.target.value) : null)}
          >
            <option value="">Tous</option>
            <option value="0">En attente</option>
            <option value="1">Autorisé</option>
            <option value="2">Payé</option>
            <option value="3">Échoué</option>
            <option value="4">Annulé</option>
            <option value="5">Remboursé</option>
          </select>
        </div>

        {/* Méthode paiement */}
        <div className="filter-group">
          <label>Méthode de paiement</label>
          <select
            value={filters.paymentMethod ?? ''}
            onChange={(e) => onFilterChange('paymentMethod', e.target.value ? parseInt(e.target.value) : null)}
          >
            <option value="">Toutes</option>
            <option value="0">À la livraison</option>
            <option value="1">En ligne</option>
          </select>
        </div>

        {/* Date début */}
        <div className="filter-group">
          <label>Date de début</label>
          <input
            type="date"
            value={filters.from ? new Date(filters.from).toISOString().split('T')[0] : ''}
            onChange={(e) => onFilterChange('from', e.target.value ? new Date(e.target.value).toISOString() : null)}
          />
        </div>

        {/* Date fin */}
        <div className="filter-group">
          <label>Date de fin</label>
          <input
            type="date"
            value={filters.to ? new Date(filters.to).toISOString().split('T')[0] : ''}
            onChange={(e) => onFilterChange('to', e.target.value ? new Date(e.target.value).toISOString() : null)}
          />
        </div>
      </div>

      <div className="filters-actions">
        <button onClick={onReset} className="btn-secondary">
          Réinitialiser
        </button>
      </div>
    </div>
  );
}
```

### Table des Commandes

```javascript
// OrdersTable.jsx
import React, { useState } from 'react';

export function OrdersTable({ orders, onRefresh }) {
  const [selectedOrder, setSelectedOrder] = useState(null);

  const getStatusBadge = (status) => {
    const statusMap = {
      0: { label: 'En attente', color: 'orange', icon: '⏳' },
      1: { label: 'Attente paiement', color: 'yellow', icon: '💳' },
      2: { label: 'Payée', color: 'green', icon: '✅' },
      3: { label: 'En préparation', color: 'blue', icon: '📦' },
      4: { label: 'Prête', color: 'teal', icon: '✔️' },
      5: { label: 'En livraison', color: 'purple', icon: '🚚' },
      6: { label: 'Livrée', color: 'green', icon: '🏠' },
      7: { label: 'Annulée', color: 'red', icon: '❌' },
      8: { label: 'Remboursée', color: 'pink', icon: '💸' }
    };
    return statusMap[status] || statusMap[0];
  };

  const getPaymentStatusBadge = (status) => {
    const statusMap = {
      0: { label: 'En attente', color: 'gray' },
      1: { label: 'Autorisé', color: 'blue' },
      2: { label: 'Payé', color: 'green' },
      3: { label: 'Échoué', color: 'red' },
      4: { label: 'Annulé', color: 'orange' },
      5: { label: 'Remboursé', color: 'purple' }
    };
    return statusMap[status] || statusMap[0];
  };

  return (
    <>
      <div className="table-container">
        <table className="orders-table">
          <thead>
            <tr>
              <th>N° Commande</th>
              <th>Date</th>
              <th>Client</th>
              <th>Articles</th>
              <th>Montant</th>
              <th>Paiement</th>
              <th>Statut</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {orders.map(order => {
              const statusBadge = getStatusBadge(order.status);
              const paymentBadge = getPaymentStatusBadge(order.paymentStatus);

              return (
                <tr key={order.id} className={`order-row status-${order.status}`}>
                  <td>
                    <strong>{order.orderNumber}</strong>
                  </td>
                  
                  <td>
                    {new Date(order.createdAt).toLocaleDateString('fr-TN', {
                      day: '2-digit',
                      month: 'short',
                      year: 'numeric',
                      hour: '2-digit',
                      minute: '2-digit'
                    })}
                  </td>
                  
                  <td>
                    <div className="customer-info">
                      <div>{order.customerFullName || 'N/A'}</div>
                      <small>{order.customerEmail}</small>
                      <small>{order.customerPhone}</small>
                    </div>
                  </td>
                  
                  <td>
                    <span className="items-count">{order.itemsCount} article(s)</span>
                  </td>
                  
                  <td>
                    <strong className="amount">{order.total.toFixed(3)} {order.currency}</strong>
                    <br />
                    <small>
                      {order.paymentMethod === 0 ? '💵 COD' : '💳 En ligne'}
                    </small>
                  </td>
                  
                  <td>
                    <span className={`badge badge-${paymentBadge.color}`}>
                      {paymentBadge.label}
                    </span>
                  </td>
                  
                  <td>
                    <span className={`badge badge-${statusBadge.color}`}>
                      {statusBadge.icon} {statusBadge.label}
                    </span>
                  </td>
                  
                  <td>
                    <div className="actions">
                      <button
                        onClick={() => setSelectedOrder(order)}
                        className="btn-icon"
                        title="Voir détails"
                      >
                        👁️
                      </button>
                      
                      {[0, 1, 2, 3, 4].includes(order.status) && (
                        <QuickStatusButton
                          order={order}
                          onUpdate={onRefresh}
                        />
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Modal détails */}
      {selectedOrder && (
        <OrderDetailsModal
          orderId={selectedOrder.id}
          onClose={() => setSelectedOrder(null)}
          onUpdate={onRefresh}
        />
      )}
    </>
  );
}

// Bouton action rapide selon le statut
function QuickStatusButton({ order, onUpdate }) {
  const actions = {
    0: { label: 'Confirmer', nextStatus: 3, icon: '✅' }, // Pending → Preparing
    1: { label: 'En préparation', nextStatus: 3, icon: '📦' }, // AwaitingPayment → Preparing (si payé)
    2: { label: 'Préparer', nextStatus: 3, icon: '📦' }, // Paid → Preparing
    3: { label: 'Prête', nextStatus: 4, icon: '✔️' }, // Preparing → Ready
    4: { label: 'Expédier', nextStatus: 5, icon: '🚚' }, // Ready → OutForDelivery
  };

  const action = actions[order.status];
  if (!action) return null;

  const handleQuickAction = async () => {
    try {
      const response = await fetch(`/api/orders/${order.id}/status`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${adminToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          status: action.nextStatus,
          note: `Action rapide: ${action.label}`
        })
      });

      if (!response.ok) throw new Error('Erreur mise à jour');

      showNotification(`Commande ${action.label.toLowerCase()}`, 'success');
      onUpdate();
    } catch (error) {
      console.error('Erreur:', error);
      showNotification('Erreur mise à jour statut', 'error');
    }
  };

  return (
    <button
      onClick={handleQuickAction}
      className="btn-quick-action"
      title={action.label}
    >
      {action.icon}
    </button>
  );
}
```

---

## Détails d'une Commande

### Modal/Page de détails complets

```javascript
// OrderDetailsModal.jsx
import React, { useState, useEffect } from 'react';

export function OrderDetailsModal({ orderId, onClose, onUpdate }) {
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [isEditingStatus, setIsEditingStatus] = useState(false);
  const [isEditingFees, setIsEditingFees] = useState(false);

  useEffect(() => {
    fetchOrderDetails();
  }, [orderId]);

  const fetchOrderDetails = async () => {
    try {
      setLoading(true);
      const response = await fetch(`/api/orders/${orderId}`, {
        headers: { 'Authorization': `Bearer ${adminToken}` }
      });

      if (!response.ok) throw new Error('Erreur chargement');

      const data = await response.json();
      setOrder(data);
    } catch (error) {
      console.error('Erreur:', error);
      showNotification('Erreur chargement commande', 'error');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="modal-overlay">
        <div className="modal">
          <div className="loading">Chargement...</div>
        </div>
      </div>
    );
  }

  if (!order) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal-large" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Commande {order.orderNumber}</h2>
          <button onClick={onClose} className="btn-close">✕</button>
        </div>

        <div className="modal-body">
          {/* Informations principales */}
          <div className="order-info-grid">
            <InfoCard title="Statut Commande">
              <StatusBadge status={order.status} />
              <button
                onClick={() => setIsEditingStatus(true)}
                className="btn-link"
              >
                Modifier
              </button>
            </InfoCard>

            <InfoCard title="Paiement">
              <div>
                <PaymentStatusBadge status={order.paymentStatus} />
                <p>
                  Méthode: {order.paymentMethod === 0 ? '💵 À la livraison' : '💳 En ligne'}
                </p>
                {order.paymentMethod === 1 && (
                  <p>Fournisseur: {getProviderName(order.provider)}</p>
                )}
              </div>
            </InfoCard>

            <InfoCard title="Montants">
              <div className="amounts">
                <div className="amount-line">
                  <span>Sous-total:</span>
                  <span>{order.subtotal.toFixed(3)} TND</span>
                </div>
                {order.discountTotal > 0 && (
                  <div className="amount-line discount">
                    <span>Réduction:</span>
                    <span>-{order.discountTotal.toFixed(3)} TND</span>
                  </div>
                )}
                <div className="amount-line">
                  <span>Livraison:</span>
                  <span>{order.deliveryFee.toFixed(3)} TND</span>
                </div>
                <div className="amount-line total">
                  <span>Total:</span>
                  <span><strong>{order.total.toFixed(3)} TND</strong></span>
                </div>
              </div>
              <button
                onClick={() => setIsEditingFees(true)}
                className="btn-link"
              >
                Modifier frais
              </button>
            </InfoCard>

            <InfoCard title="Date">
              <p>
                {new Date(order.createdAt).toLocaleString('fr-TN', {
                  day: '2-digit',
                  month: 'long',
                  year: 'numeric',
                  hour: '2-digit',
                  minute: '2-digit'
                })}
              </p>
            </InfoCard>
          </div>

          {/* Adresse de livraison */}
          <div className="section">
            <h3>📍 Adresse de livraison</h3>
            <div className="address-card">
              <p><strong>{order.shippingAddress.fullName}</strong></p>
              <p>{order.shippingAddress.phone}</p>
              <p>{order.shippingAddress.line1}</p>
              {order.shippingAddress.line2 && <p>{order.shippingAddress.line2}</p>}
              <p>
                {order.shippingAddress.postalCode} {order.shippingAddress.city}
              </p>
              {order.shippingAddress.governorate && (
                <p>{order.shippingAddress.governorate}</p>
              )}
            </div>
          </div>

          {/* Articles */}
          <div className="section">
            <h3>📦 Articles ({order.items.length})</h3>
            <table className="items-table">
              <thead>
                <tr>
                  <th>Produit</th>
                  <th>Prix unitaire</th>
                  <th>Quantité</th>
                  <th>Total</th>
                </tr>
              </thead>
              <tbody>
                {order.items.map(item => (
                  <tr key={item.id}>
                    <td>
                      <div className="item-info">
                        {item.imageUrl && (
                          <img src={item.imageUrl} alt={item.name} className="item-image" />
                        )}
                        <div>
                          <strong>{item.name}</strong>
                          {item.variantName && (
                            <p className="variant">{item.variantName}</p>
                          )}
                          {item.sku && (
                            <small className="sku">SKU: {item.sku}</small>
                          )}
                        </div>
                      </div>
                    </td>
                    <td>{item.unitPrice.toFixed(3)} TND</td>
                    <td>{item.quantity}</td>
                    <td><strong>{item.lineTotal.toFixed(3)} TND</strong></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Notes */}
          {(order.customerNote || order.adminNote) && (
            <div className="section">
              <h3>📝 Notes</h3>
              {order.customerNote && (
                <div className="note customer-note">
                  <strong>Note client:</strong>
                  <p>{order.customerNote}</p>
                </div>
              )}
              {order.adminNote && (
                <div className="note admin-note">
                  <strong>Note admin:</strong>
                  <p>{order.adminNote}</p>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Modals d'édition */}
        {isEditingStatus && (
          <UpdateStatusModal
            order={order}
            onClose={() => setIsEditingStatus(false)}
            onUpdate={() => {
              setIsEditingStatus(false);
              fetchOrderDetails();
              onUpdate();
            }}
          />
        )}

        {isEditingFees && (
          <UpdateFeesModal
            order={order}
            onClose={() => setIsEditingFees(false)}
            onUpdate={() => {
              setIsEditingFees(false);
              fetchOrderDetails();
              onUpdate();
            }}
          />
        )}
      </div>
    </div>
  );
}

function InfoCard({ title, children }) {
  return (
    <div className="info-card">
      <h4>{title}</h4>
      {children}
    </div>
  );
}
```

---

## Gestion des Statuts

### Modal de changement de statut

```javascript
// UpdateStatusModal.jsx
import React, { useState } from 'react';

export function UpdateStatusModal({ order, onClose, onUpdate }) {
  const [newStatus, setNewStatus] = useState(order.status);
  const [note, setNote] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const statusOptions = [
    { value: 0, label: 'En attente', available: true },
    { value: 1, label: 'Attente paiement', available: order.paymentMethod === 1 },
    { value: 2, label: 'Payée', available: true },
    { value: 3, label: 'En préparation', available: true },
    { value: 4, label: 'Prête', available: true },
    { value: 5, label: 'En livraison', available: true },
    { value: 6, label: 'Livrée', available: true },
    { value: 7, label: 'Annulée', available: true },
    { value: 8, label: 'Remboursée', available: order.paymentStatus === 2 }
  ].filter(opt => opt.available);

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (newStatus === order.status) {
      showNotification('Aucun changement détecté', 'info');
      return;
    }

    try {
      setIsSubmitting(true);

      const response = await fetch(`/api/orders/${order.id}/status`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${adminToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          status: newStatus,
          note: note.trim() || null
        })
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.detail || 'Erreur mise à jour');
      }

      showNotification('Statut mis à jour avec succès', 'success');
      onUpdate();
    } catch (error) {
      console.error('Erreur:', error);
      showNotification(error.message, 'error');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal-small" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h3>Changer le statut</h3>
          <button onClick={onClose} className="btn-close">✕</button>
        </div>

        <form onSubmit={handleSubmit} className="modal-body">
          <div className="form-group">
            <label>Statut actuel</label>
            <StatusBadge status={order.status} />
          </div>

          <div className="form-group">
            <label htmlFor="newStatus">Nouveau statut *</label>
            <select
              id="newStatus"
              value={newStatus}
              onChange={(e) => setNewStatus(parseInt(e.target.value))}
              required
              className="form-control"
            >
              {statusOptions.map(opt => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="note">Note (optionnel)</label>
            <textarea
              id="note"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder="Raison du changement..."
              rows={3}
              maxLength={600}
              className="form-control"
            />
            <small>{note.length} / 600 caractères</small>
          </div>

          <div className="modal-actions">
            <button
              type="button"
              onClick={onClose}
              className="btn-secondary"
              disabled={isSubmitting}
            >
              Annuler
            </button>
            <button
              type="submit"
              className="btn-primary"
              disabled={isSubmitting || newStatus === order.status}
            >
              {isSubmitting ? 'Mise à jour...' : 'Mettre à jour'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
```

### Modal de modification des frais

```javascript
// UpdateFeesModal.jsx
import React, { useState } from 'react';

export function UpdateFeesModal({ order, onClose, onUpdate }) {
  const [deliveryFee, setDeliveryFee] = useState(order.deliveryFee);
  const [discountTotal, setDiscountTotal] = useState(order.discountTotal);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const calculatedTotal = order.subtotal - discountTotal + deliveryFee;

  const handleSubmit = async (e) => {
    e.preventDefault();

    try {
      setIsSubmitting(true);

      const response = await fetch(`/api/orders/${order.id}/fees`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${adminToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          deliveryFee: parseFloat(deliveryFee),
          discountTotal: parseFloat(discountTotal)
        })
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.detail || 'Erreur mise à jour');
      }

      showNotification('Frais mis à jour avec succès', 'success');
      onUpdate();
    } catch (error) {
      console.error('Erreur:', error);
      showNotification(error.message, 'error');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal-small" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h3>Modifier les frais</h3>
          <button onClick={onClose} className="btn-close">✕</button>
        </div>

        <form onSubmit={handleSubmit} className="modal-body">
          <div className="form-group">
            <label>Sous-total (non modifiable)</label>
            <div className="readonly-value">
              {order.subtotal.toFixed(3)} TND
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="deliveryFee">Frais de livraison (TND) *</label>
            <input
              type="number"
              id="deliveryFee"
              value={deliveryFee}
              onChange={(e) => setDeliveryFee(e.target.value)}
              min="0"
              max="999999"
              step="0.001"
              required
              className="form-control"
            />
          </div>

          <div className="form-group">
            <label htmlFor="discountTotal">Réduction totale (TND) *</label>
            <input
              type="number"
              id="discountTotal"
              value={discountTotal}
              onChange={(e) => setDiscountTotal(e.target.value)}
              min="0"
              max="999999"
              step="0.001"
              required
              className="form-control"
            />
          </div>

          <div className="total-preview">
            <div className="total-line">
              <span>Sous-total:</span>
              <span>{order.subtotal.toFixed(3)} TND</span>
            </div>
            {discountTotal > 0 && (
              <div className="total-line discount">
                <span>Réduction:</span>
                <span>-{parseFloat(discountTotal).toFixed(3)} TND</span>
              </div>
            )}
            {deliveryFee > 0 && (
              <div className="total-line">
                <span>Livraison:</span>
                <span>+{parseFloat(deliveryFee).toFixed(3)} TND</span>
              </div>
            )}
            <div className="total-line total">
              <span>Nouveau total:</span>
              <span><strong>{calculatedTotal.toFixed(3)} TND</strong></span>
            </div>
          </div>

          <div className="modal-actions">
            <button
              type="button"
              onClick={onClose}
              className="btn-secondary"
              disabled={isSubmitting}
            >
              Annuler
            </button>
            <button
              type="submit"
              className="btn-primary"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Mise à jour...' : 'Mettre à jour'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
```

---

## Workflows de Traitement

### Workflow Paiement à la Livraison (COD)

```javascript
// CODWorkflow.jsx
export function CODWorkflow({ order, onUpdate }) {
  const steps = [
    {
      status: 0,
      label: 'En attente',
      action: 'Confirmer',
      nextStatus: 3,
      icon: '⏳',
      description: 'Commande reçue, en attente de confirmation'
    },
    {
      status: 3,
      label: 'En préparation',
      action: 'Marquer prête',
      nextStatus: 4,
      icon: '📦',
      description: 'Préparer les articles commandés'
    },
    {
      status: 4,
      label: 'Prête',
      action: 'Expédier',
      nextStatus: 5,
      icon: '✔️',
      description: 'Commande prête, attente du livreur'
    },
    {
      status: 5,
      label: 'En livraison',
      action: 'Marquer livrée',
      nextStatus: 6,
      icon: '🚚',
      description: 'En cours de livraison au client'
    },
    {
      status: 6,
      label: 'Livrée',
      action: null,
      nextStatus: null,
      icon: '🏠',
      description: 'Commande livrée et payée'
    }
  ];

  const currentStepIndex = steps.findIndex(s => s.status === order.status);

  return (
    <div className="workflow-panel">
      <h3>Workflow COD</h3>
      
      <div className="workflow-steps">
        {steps.map((step, index) => (
          <div
            key={step.status}
            className={`workflow-step ${
              index < currentStepIndex ? 'completed' :
              index === currentStepIndex ? 'current' : 'pending'
            }`}
          >
            <div className="step-icon">{step.icon}</div>
            <div className="step-label">{step.label}</div>
            <div className="step-description">{step.description}</div>
            
            {index === currentStepIndex && step.action && (
              <button
                onClick={() => handleStatusChange(step.nextStatus)}
                className="btn-action"
              >
                {step.action}
              </button>
            )}
          </div>
        ))}
      </div>
    </div>
  );

  async function handleStatusChange(nextStatus) {
    const response = await fetch(`/api/orders/${order.id}/status`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${adminToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        status: nextStatus,
        note: `Workflow COD: passage à l'étape suivante`
      })
    });

    if (response.ok) {
      showNotification('Statut mis à jour', 'success');
      onUpdate();
    }
  }
}
```

### Workflow Paiement en Ligne

```javascript
// OnlinePaymentWorkflow.jsx
export function OnlinePaymentWorkflow({ order, onUpdate }) {
  const steps = [
    {
      status: 1,
      label: 'Attente paiement',
      icon: '💳',
      description: 'Stock réservé (15 min), attente paiement client'
    },
    {
      status: 2,
      label: 'Payée',
      action: 'Commencer préparation',
      nextStatus: 3,
      icon: '✅',
      description: 'Paiement confirmé, stock déduit'
    },
    {
      status: 3,
      label: 'En préparation',
      action: 'Marquer prête',
      nextStatus: 4,
      icon: '📦',
      description: 'Préparer les articles commandés'
    },
    {
      status: 4,
      label: 'Prête',
      action: 'Expédier',
      nextStatus: 5,
      icon: '✔️',
      description: 'Commande prête, attente du livreur'
    },
    {
      status: 5,
      label: 'En livraison',
      action: 'Marquer livrée',
      nextStatus: 6,
      icon: '🚚',
      description: 'En cours de livraison au client'
    },
    {
      status: 6,
      label: 'Livrée',
      action: null,
      nextStatus: null,
      icon: '🏠',
      description: 'Commande livrée avec succès'
    }
  ];

  const currentStepIndex = steps.findIndex(s => s.status === order.status);

  return (
    <div className="workflow-panel">
      <h3>Workflow Paiement en Ligne</h3>
      
      {order.paymentStatus !== 2 && order.status === 1 && (
        <div className="alert alert-warning">
          ⚠️ En attente du paiement client. Stock réservé temporairement.
        </div>
      )}
      
      <div className="workflow-steps">
        {steps.map((step, index) => (
          <div
            key={step.status}
            className={`workflow-step ${
              index < currentStepIndex ? 'completed' :
              index === currentStepIndex ? 'current' : 'pending'
            }`}
          >
            <div className="step-icon">{step.icon}</div>
            <div className="step-label">{step.label}</div>
            <div className="step-description">{step.description}</div>
            
            {index === currentStepIndex && step.action && (
              <button
                onClick={() => handleStatusChange(step.nextStatus)}
                className="btn-action"
              >
                {step.action}
              </button>
            )}
          </div>
        ))}
      </div>
    </div>
  );

  async function handleStatusChange(nextStatus) {
    const response = await fetch(`/api/orders/${order.id}/status`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${adminToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        status: nextStatus,
        note: `Workflow Online: passage à l'étape suivante`
      })
    });

    if (response.ok) {
      showNotification('Statut mis à jour', 'success');
      onUpdate();
    }
  }
}
```

---

## Notifications et Alertes

### Système d'alertes admin

```javascript
// AdminAlerts.jsx
import React, { useState, useEffect } from 'react';

export function AdminAlerts() {
  const [alerts, setAlerts] = useState([]);

  useEffect(() => {
    checkAlerts();
    
    // Vérifier toutes les 2 minutes
    const interval = setInterval(checkAlerts, 120000);
    return () => clearInterval(interval);
  }, []);

  const checkAlerts = async () => {
    const newAlerts = [];

    // Commandes en attente > 30 minutes
    const pendingOrders = await fetchOrders({ status: 0, pageSize: 100 });
    const oldPending = pendingOrders.items.filter(order => {
      const age = Date.now() - new Date(order.createdAt).getTime();
      return age > 30 * 60 * 1000; // 30 minutes
    });

    if (oldPending.length > 0) {
      newAlerts.push({
        id: 'pending-old',
        type: 'warning',
        title: 'Commandes en attente',
        message: `${oldPending.length} commande(s) en attente depuis plus de 30 minutes`,
        action: () => navigateTo('/admin/orders?status=0')
      });
    }

    // Paiements en attente proche expiration
    const awaitingPayment = await fetchOrders({ status: 1, pageSize: 100 });
    const nearExpiration = awaitingPayment.items.filter(order => {
      // Si on peut récupérer l'expiration de la réservation
      // Sinon utiliser 15 minutes par défaut
      const age = Date.now() - new Date(order.createdAt).getTime();
      const remaining = (15 * 60 * 1000) - age;
      return remaining > 0 && remaining < 5 * 60 * 1000; // < 5 minutes restantes
    });

    if (nearExpiration.length > 0) {
      newAlerts.push({
        id: 'payment-expiring',
        type: 'urgent',
        title: 'Paiements expirant bientôt',
        message: `${nearExpiration.length} paiement(s) expireront dans moins de 5 minutes`,
        action: () => navigateTo('/admin/orders?status=1')
      });
    }

    // Commandes en préparation trop longtemps
    const preparing = await fetchOrders({ status: 3, pageSize: 100 });
    const longPreparing = preparing.items.filter(order => {
      const age = Date.now() - new Date(order.createdAt).getTime();
      return age > 2 * 60 * 60 * 1000; // > 2 heures
    });

    if (longPreparing.length > 0) {
      newAlerts.push({
        id: 'preparing-long',
        type: 'info',
        title: 'Préparations longues',
        message: `${longPreparing.length} commande(s) en préparation depuis plus de 2 heures`,
        action: () => navigateTo('/admin/orders?status=3')
      });
    }

    setAlerts(newAlerts);
  };

  if (alerts.length === 0) return null;

  return (
    <div className="admin-alerts">
      {alerts.map(alert => (
        <div key={alert.id} className={`alert alert-${alert.type}`}>
          <span className="alert-icon">
            {alert.type === 'urgent' ? '🚨' : alert.type === 'warning' ? '⚠️' : 'ℹ️'}
          </span>
          <div className="alert-content">
            <strong>{alert.title}</strong>
            <p>{alert.message}</p>
          </div>
          {alert.action && (
            <button onClick={alert.action} className="alert-action">
              Voir
            </button>
          )}
        </div>
      ))}
    </div>
  );
}
```

---

## Rapports et Statistiques

### Générateur de rapports

```javascript
// ReportsPage.jsx
import React, { useState } from 'react';

export function ReportsPage() {
  const [reportType, setReportType] = useState('daily');
  const [dateRange, setDateRange] = useState({
    from: new Date().toISOString().split('T')[0],
    to: new Date().toISOString().split('T')[0]
  });
  const [report, setReport] = useState(null);
  const [loading, setLoading] = useState(false);

  const generateReport = async () => {
    try {
      setLoading(true);

      const from = new Date(dateRange.from);
      from.setHours(0, 0, 0, 0);

      const to = new Date(dateRange.to);
      to.setHours(23, 59, 59, 999);

      const response = await fetch(
        `/api/orders?from=${from.toISOString()}&to=${to.toISOString()}&pageSize=1000`,
        { headers: { 'Authorization': `Bearer ${adminToken}` } }
      );

      if (!response.ok) throw new Error('Erreur génération rapport');

      const data = await response.json();

      // Calculer les statistiques
      const stats = {
        totalOrders: data.totalCount,
        completedOrders: data.items.filter(o => o.status === 6).length,
        canceledOrders: data.items.filter(o => o.status === 7).length,
        totalRevenue: data.items
          .filter(o => o.status === 6)
          .reduce((sum, o) => sum + o.total, 0),
        averageOrderValue: 0,
        paymentMethods: {
          cod: data.items.filter(o => o.paymentMethod === 0).length,
          online: data.items.filter(o => o.paymentMethod === 1).length
        },
        statusBreakdown: {}
      };

      stats.averageOrderValue = stats.completedOrders > 0
        ? stats.totalRevenue / stats.completedOrders
        : 0;

      // Répartition par statut
      [0, 1, 2, 3, 4, 5, 6, 7, 8].forEach(status => {
        const count = data.items.filter(o => o.status === status).length;
        if (count > 0) {
          stats.statusBreakdown[status] = count;
        }
      });

      setReport({ stats, orders: data.items });
    } catch (error) {
      console.error('Erreur:', error);
      showNotification('Erreur génération rapport', 'error');
    } finally {
      setLoading(false);
    }
  };

  const exportToCSV = () => {
    if (!report) return;

    const csv = [
      ['Numéro', 'Date', 'Client', 'Email', 'Téléphone', 'Montant', 'Statut', 'Paiement'].join(','),
      ...report.orders.map(o => [
        o.orderNumber,
        new Date(o.createdAt).toLocaleString('fr-TN'),
        o.customerFullName || '',
        o.customerEmail || '',
        o.customerPhone || '',
        o.total.toFixed(3),
        getStatusLabel(o.status),
        o.paymentMethod === 0 ? 'COD' : 'Online'
      ].join(','))
    ].join('\n');

    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `rapport-commandes-${dateRange.from}-${dateRange.to}.csv`;
    link.click();
  };

  return (
    <div className="reports-page">
      <h1>Rapports et Statistiques</h1>

      <div className="report-config">
        <div className="form-group">
          <label>Type de rapport</label>
          <select value={reportType} onChange={(e) => setReportType(e.target.value)}>
            <option value="daily">Journalier</option>
            <option value="weekly">Hebdomadaire</option>
            <option value="monthly">Mensuel</option>
            <option value="custom">Personnalisé</option>
          </select>
        </div>

        <div className="form-group">
          <label>Date de début</label>
          <input
            type="date"
            value={dateRange.from}
            onChange={(e) => setDateRange({ ...dateRange, from: e.target.value })}
          />
        </div>

        <div className="form-group">
          <label>Date de fin</label>
          <input
            type="date"
            value={dateRange.to}
            onChange={(e) => setDateRange({ ...dateRange, to: e.target.value })}
          />
        </div>

        <button onClick={generateReport} disabled={loading} className="btn-primary">
          {loading ? 'Génération...' : 'Générer le rapport'}
        </button>
      </div>

      {report && (
        <div className="report-results">
          <div className="report-header">
            <h2>
              Rapport du {new Date(dateRange.from).toLocaleDateString('fr-TN')} au{' '}
              {new Date(dateRange.to).toLocaleDateString('fr-TN')}
            </h2>
            <button onClick={exportToCSV} className="btn-secondary">
              📥 Exporter CSV
            </button>
          </div>

          <div className="stats-grid">
            <StatCard
              title="Total commandes"
              value={report.stats.totalOrders}
              color="blue"
            />
            <StatCard
              title="Commandes livrées"
              value={report.stats.completedOrders}
              color="green"
            />
            <StatCard
              title="Commandes annulées"
              value={report.stats.canceledOrders}
              color="red"
            />
            <StatCard
              title="Chiffre d'affaires"
              value={`${report.stats.totalRevenue.toFixed(3)} TND`}
              color="green"
            />
            <StatCard
              title="Panier moyen"
              value={`${report.stats.averageOrderValue.toFixed(3)} TND`}
              color="teal"
            />
            <StatCard
              title="Paiement à la livraison"
              value={`${report.stats.paymentMethods.cod} (${((report.stats.paymentMethods.cod / report.stats.totalOrders) * 100).toFixed(1)}%)`}
              color="orange"
            />
          </div>

          <div className="status-breakdown">
            <h3>Répartition par statut</h3>
            <div className="breakdown-chart">
              {Object.entries(report.stats.statusBreakdown).map(([status, count]) => (
                <div key={status} className="breakdown-item">
                  <span className="status-label">{getStatusLabel(parseInt(status))}</span>
                  <div className="progress-bar">
                    <div
                      className="progress-fill"
                      style={{ width: `${(count / report.stats.totalOrders) * 100}%` }}
                    />
                  </div>
                  <span className="count">{count}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
```

---

## Bonnes Pratiques Admin

### 1. **Refresh automatique intelligent**

```javascript
// Refresh seulement les données nécessaires
useEffect(() => {
  const interval = setInterval(() => {
    // Refresh uniquement si l'utilisateur est actif sur la page
    if (document.visibilityState === 'visible') {
      fetchOrders();
    }
  }, 30000); // 30 secondes

  return () => clearInterval(interval);
}, []);
```

### 2. **Actions en masse**

```javascript
// BulkActions.jsx
export function BulkActions({ selectedOrders, onComplete }) {
  const [action, setAction] = useState('');

  const handleBulkAction = async () => {
    if (selectedOrders.length === 0) {
      showNotification('Aucune commande sélectionnée', 'warning');
      return;
    }

    if (!confirm(`Appliquer l'action à ${selectedOrders.length} commande(s) ?`)) {
      return;
    }

    try {
      const promises = selectedOrders.map(orderId =>
        fetch(`/api/orders/${orderId}/status`, {
          method: 'PUT',
          headers: {
            'Authorization': `Bearer ${adminToken}`,
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            status: parseInt(action),
            note: 'Action en masse'
          })
        })
      );

      await Promise.all(promises);

      showNotification(`${selectedOrders.length} commandes mises à jour`, 'success');
      onComplete();
    } catch (error) {
      showNotification('Erreur action en masse', 'error');
    }
  };

  return (
    <div className="bulk-actions">
      <select value={action} onChange={(e) => setAction(e.target.value)}>
        <option value="">Actions en masse...</option>
        <option value="3">Passer en préparation</option>
        <option value="4">Marquer comme prêtes</option>
        <option value="5">Expédier</option>
      </select>
      <button onClick={handleBulkAction} disabled={!action}>
        Appliquer
      </button>
    </div>
  );
}
```

### 3. **Raccourcis clavier**

```javascript
// Raccourcis clavier pour actions rapides
useEffect(() => {
  const handleKeyPress = (e) => {
    // Ctrl + R : Refresh
    if (e.ctrlKey && e.key === 'r') {
      e.preventDefault();
      fetchOrders();
    }

    // Ctrl + F : Focus sur recherche
    if (e.ctrlKey && e.key === 'f') {
      e.preventDefault();
      document.getElementById('search-input')?.focus();
    }

    // Echap : Fermer modal
    if (e.key === 'Escape') {
      setSelectedOrder(null);
    }
  };

  window.addEventListener('keydown', handleKeyPress);
  return () => window.removeEventListener('keydown', handleKeyPress);
}, []);
```

### 4. **Logs d'actions admin**

```javascript
// Tracker toutes les actions admin
const logAdminAction = async (action, orderId, details) => {
  try {
    await fetch('/api/admin/logs', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${adminToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        action,
        resource: 'order',
        resourceId: orderId,
        details,
        timestamp: new Date().toISOString()
      })
    });
  } catch (error) {
    console.error('Erreur log admin:', error);
  }
};

// Utilisation
const handleStatusChange = async (orderId, newStatus) => {
  await updateOrderStatus(orderId, newStatus);
  await logAdminAction('status_change', orderId, { newStatus });
};
```

---

## ✅ Checklist d'implémentation Admin

### Interface de base
- [ ] Dashboard avec statistiques en temps réel
- [ ] Liste des commandes avec pagination
- [ ] Filtres avancés (statut, paiement, date, recherche)
- [ ] Modal/Page détails commande
- [ ] Modal changement de statut
- [ ] Modal modification frais

### Fonctionnalités avancées
- [ ] Actions rapides sur les commandes
- [ ] Actions en masse (bulk actions)
- [ ] Système d'alertes automatiques
- [ ] Graphiques d'évolution
- [ ] Générateur de rapports
- [ ] Export CSV/PDF

### Optimisations
- [ ] Refresh automatique intelligent
- [ ] Raccourcis clavier
- [ ] Pagination efficace
- [ ] Chargement lazy des détails
- [ ] Cache des statistiques

### Sécurité et traçabilité
- [ ] Vérification rôle Admin
- [ ] Logs des actions admin
- [ ] Confirmation avant actions critiques
- [ ] Validation des données côté client
- [ ] Gestion des erreurs complète

---

## 📞 Support et Documentation

- 📖 **API Complète:** [ORDER_API_DOCUMENTATION.md](ORDER_API_DOCUMENTATION.md)
- 📦 **Gestion Stock Admin:** [STOCK_API_DOCUMENTATION.md](STOCK_API_DOCUMENTATION.md)
- 🛒 **API Cart:** [CART_API_DOCUMENTATION.md](CART_API_DOCUMENTATION.md)

---

**Dernière mise à jour:** 11 février 2026  
**Version:** 1.0  
**Maintenu par:** Équipe Backend Hlouwa
