# 📞 Guide Admin - Gestion des Réclamations

> Guide complet pour l'interface d'administration de gestion des réclamations client avec exemples d'implémentation frontend.

**Audience:** Équipe Frontend - Interface Admin

**Date:** 11 février 2026

---

## 📋 Table des matières

1. [Vue d'ensemble](#vue-densemble)
2. [Architecture de l'interface](#architecture-de-linterface)
3. [Dashboard Réclamations](#dashboard-réclamations)
4. [Liste des Réclamations](#liste-des-réclamations)
5. [Détails d'une Réclamation](#détails-dune-réclamation)
6. [Gestion des Statuts](#gestion-des-statuts)
7. [Filtres et Recherche](#filtres-et-recherche)
8. [Workflows de Traitement](#workflows-de-traitement)
9. [Notifications et Priorités](#notifications-et-priorités)
10. [Statistiques et Rapports](#statistiques-et-rapports)
11. [Référence API Complète](#référence-api-complète)
12. [Implémentation Frontend](#implémentation-frontend)
13. [Bonnes Pratiques](#bonnes-pratiques)

---

## Vue d'ensemble

### Objectif du système de réclamations

Le système permet de :
- ✅ **Recevoir** les réclamations clients (publiques ou authentifiées)
- ✅ **Centraliser** toutes les demandes de support
- ✅ **Suivre** l'évolution du traitement (Nouveau → En cours → Résolu)
- ✅ **Lier** les réclamations aux commandes concernées
- ✅ **Répondre** efficacement avec des notes admin
- ✅ **Analyser** les tendances et problèmes récurrents

### Statuts disponibles

| Valeur | Statut | Description | Couleur |
|--------|--------|-------------|---------|
| 0 | New | Réclamation reçue, non traitée | 🔴 Rouge |
| 1 | InProgress | En cours de traitement | 🟡 Jaune |
| 2 | Resolved | Résolue et terminée | 🟢 Vert |

### Endpoints Admin

| Endpoint | Méthode | Description |
|----------|---------|-------------|
| `/api/reclamations` | GET | Liste paginée avec filtres |
| `/api/reclamations/{id}` | GET | Détails complets |
| `/api/reclamations/{id}` | PUT | Mettre à jour statut + note |
| `/api/reclamations/{id}` | DELETE | Suppression (soft delete) |

---

## Architecture de l'interface

### Structure recommandée

```
┌─────────────────────────────────────────────────────┐
│              Admin Reclamations Layout               │
│  ┌──────────┐  ┌─────────────────────────────────┐ │
│  │          │  │                                 │ │
│  │ Sidebar  │  │     Main Content Area           │ │
│  │          │  │                                 │ │
│  │ - Home   │  │  ┌──────────────────────────┐  │ │
│  │ - Orders │  │  │   Stats Dashboard        │  │ │
│  │ - Claims │  │  │   (New/InProgress/...)   │  │ │
│  │ - Stock  │  │  └──────────────────────────┘  │ │
│  │ - Users  │  │                                 │ │
│  │          │  │  ┌──────────────────────────┐  │ │
│  │          │  │  │   Reclamations List      │  │ │
│  │          │  │  │   (Table + Filters)      │  │ │
│  │          │  │  └──────────────────────────┘  │ │
│  │          │  │                                 │ │
│  │          │  │  ┌──────────────────────────┐  │ │
│  │          │  │  │   Details Modal/Panel    │  │ │
│  │          │  │  │   (Message + response)   │  │ │
│  │          │  │  └──────────────────────────┘  │ │
│  └──────────┘  └─────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

### Flux de traitement

```
┌──────────────┐
│   Nouvelle   │  Customer creates reclamation
│ Réclamation  │  (Public or Authenticated)
└───────┬──────┘
        │
        ▼
   ┌─────────┐
   │   New   │──────► Admin sees notification
   │  (0)    │        Priority: HIGH
   └────┬────┘
        │
        │ Admin takes action
        ▼
 ┌──────────────┐
 │  InProgress  │────► Admin investigating
 │     (1)      │      Customer notified
 └──────┬───────┘
        │
        │ Issue resolved
        ▼
   ┌──────────┐
   │ Resolved │────► Customer satisfied
   │   (2)    │      Case closed
   └──────────┘
```

---

## Dashboard Réclamations

### Composant Dashboard avec statistiques temps réel

```javascript
// ReclamationsDashboard.jsx
import React, { useState, useEffect } from 'react';

export function ReclamationsDashboard() {
  const [stats, setStats] = useState({
    newCount: 0,
    inProgressCount: 0,
    resolvedCount: 0,
    totalToday: 0,
    totalWeek: 0,
    averageResponseTime: 0,
    withOrderId: 0
  });

  useEffect(() => {
    fetchStats();
    
    // Refresh toutes les 30 secondes
    const interval = setInterval(fetchStats, 30000);
    return () => clearInterval(interval);
  }, []);

  const fetchStats = async () => {
    try {
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      
      const weekAgo = new Date(today);
      weekAgo.setDate(weekAgo.getDate() - 7);

      // Requêtes parallèles
      const [newData, inProgressData, resolvedData, todayData, weekData] = 
        await Promise.all([
          fetchReclamations({ status: 0, pageSize: 1 }),
          fetchReclamations({ status: 1, pageSize: 1 }),
          fetchReclamations({ status: 2, pageSize: 100 }),
          fetchReclamations({ from: today.toISOString(), pageSize: 100 }),
          fetchReclamations({ from: weekAgo.toISOString(), pageSize: 1000 })
        ]);

      // Calculer temps de résolution moyen
      const resolved = resolvedData.items.filter(r => r.status === 2);
      const avgTime = resolved.length > 0
        ? resolved.reduce((sum, r) => {
            const created = new Date(r.createdAt);
            const now = new Date();
            return sum + (now - created) / 1000 / 60 / 60; // heures
          }, 0) / resolved.length
        : 0;

      setStats({
        newCount: newData.totalCount,
        inProgressCount: inProgressData.totalCount,
        resolvedCount: resolvedData.totalCount,
        totalToday: todayData.totalCount,
        totalWeek: weekData.totalCount,
        averageResponseTime: avgTime,
        withOrderId: weekData.items.filter(r => r.orderId).length
      });
    } catch (error) {
      console.error('Erreur stats:', error);
    }
  };

  const fetchReclamations = async (params) => {
    const queryString = new URLSearchParams(params).toString();
    const response = await fetch(`/api/reclamations?${queryString}`, {
      headers: { 'Authorization': `Bearer ${adminToken}` }
    });
    return await response.json();
  };

  return (
    <div className="reclamations-dashboard">
      <div className="page-header">
        <h1>📞 Gestion des Réclamations</h1>
        <button onClick={fetchStats} className="btn-refresh">
          🔄 Actualiser
        </button>
      </div>

      {/* Statistiques prioritaires */}
      <div className="stats-grid priority">
        <StatCard
          title="Nouvelles"
          value={stats.newCount}
          color="red"
          icon="🆕"
          urgent={stats.newCount > 5}
          onClick={() => navigateTo('/admin/reclamations?status=0')}
        />
        
        <StatCard
          title="En cours"
          value={stats.inProgressCount}
          color="yellow"
          icon="⏳"
          onClick={() => navigateTo('/admin/reclamations?status=1')}
        />
        
        <StatCard
          title="Résolues"
          value={stats.resolvedCount}
          color="green"
          icon="✅"
          onClick={() => navigateTo('/admin/reclamations?status=2')}
        />
        
        <StatCard
          title="Aujourd'hui"
          value={stats.totalToday}
          color="blue"
          icon="📊"
        />
      </div>

      {/* Statistiques secondaires */}
      <div className="stats-grid secondary">
        <StatCard
          title="Cette semaine"
          value={stats.totalWeek}
          subtitle="réclamations reçues"
          color="indigo"
          icon="📈"
        />
        
        <StatCard
          title="Temps moyen de résolution"
          value={`${stats.averageResponseTime.toFixed(1)}h`}
          color="purple"
          icon="⏱️"
        />
        
        <StatCard
          title="Liées à des commandes"
          value={`${stats.withOrderId} (${((stats.withOrderId / stats.totalWeek) * 100).toFixed(0)}%)`}
          color="teal"
          icon="🔗"
        />
      </div>

      {/* Actions rapides */}
      <div className="quick-actions">
        <h2>Actions rapides</h2>
        <button
          onClick={() => navigateTo('/admin/reclamations?status=0')}
          className="btn-action"
        >
          Traiter les nouvelles réclamations
        </button>
        <button
          onClick={() => navigateTo('/admin/reclamations?status=1')}
          className="btn-action"
        >
          Suivre les réclamations en cours
        </button>
      </div>

      {/* Réclamations récentes */}
      <RecentReclamationsList limit={10} />
    </div>
  );
}

function StatCard({ title, value, subtitle, color, icon, urgent, onClick }) {
  return (
    <div
      className={`stat-card ${color} ${urgent ? 'urgent pulse' : ''} ${onClick ? 'clickable' : ''}`}
      onClick={onClick}
    >
      <div className="stat-icon">{icon}</div>
      <div className="stat-content">
        <h3>{title}</h3>
        <div className="stat-value">{value}</div>
        {subtitle && <div className="stat-subtitle">{subtitle}</div>}
        {urgent && <span className="urgent-badge">🚨 Urgent</span>}
      </div>
    </div>
  );
}
```

---

## Liste des Réclamations

### Interface complète avec filtres

```javascript
// ReclamationsListPage.jsx
import React, { useState, useEffect } from 'react';

export function ReclamationsListPage() {
  const [reclamations, setReclamations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 20,
    totalCount: 0
  });
  
  const [filters, setFilters] = useState({
    status: null,
    search: '',
    from: null,
    to: null
  });

  useEffect(() => {
    fetchReclamations();
  }, [pagination.page, filters]);

  const fetchReclamations = async () => {
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
      const response = await fetch(`/api/reclamations?${queryString}`, {
        headers: { 'Authorization': `Bearer ${adminToken}` }
      });

      if (!response.ok) throw new Error('Erreur chargement');

      const data = await response.json();
      
      setReclamations(data.items);
      setPagination(prev => ({
        ...prev,
        totalCount: data.totalCount
      }));
    } catch (error) {
      console.error('Erreur:', error);
      showNotification('Erreur chargement des réclamations', 'error');
    } finally {
      setLoading(false);
    }
  };

  const handleFilterChange = (field, value) => {
    setFilters(prev => ({ ...prev, [field]: value }));
    setPagination(prev => ({ ...prev, page: 1 }));
  };

  const handlePageChange = (newPage) => {
    setPagination(prev => ({ ...prev, page: newPage }));
  };

  return (
    <div className="reclamations-list-page">
      <div className="page-header">
        <h1>📞 Gestion des Réclamations</h1>
        <div className="header-actions">
          <button onClick={fetchReclamations} className="btn-refresh">
            🔄 Actualiser
          </button>
          <ExportButton filters={filters} />
        </div>
      </div>

      {/* Filtres */}
      <ReclamationsFilters
        filters={filters}
        onFilterChange={handleFilterChange}
        onReset={() => setFilters({
          status: null,
          search: '',
          from: null,
          to: null
        })}
      />

      {/* Stats rapides */}
      <div className="quick-stats">
        <span>Total: <strong>{pagination.totalCount}</strong> réclamations</span>
        <span>Page {pagination.page} sur {Math.ceil(pagination.totalCount / pagination.pageSize)}</span>
      </div>

      {/* Table */}
      {loading ? (
        <div className="loading">Chargement...</div>
      ) : reclamations.length === 0 ? (
        <div className="empty-state">
          <p>Aucune réclamation trouvée</p>
        </div>
      ) : (
        <>
          <ReclamationsTable
            reclamations={reclamations}
            onRefresh={fetchReclamations}
          />

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
// ReclamationsFilters.jsx
import React from 'react';

export function ReclamationsFilters({ filters, onFilterChange, onReset }) {
  return (
    <div className="filters-panel">
      <h3>Filtres</h3>

      <div className="filters-grid">
        {/* Recherche */}
        <div className="filter-group">
          <label>Rechercher</label>
          <input
            type="text"
            placeholder="Nom, email, sujet..."
            value={filters.search}
            onChange={(e) => onFilterChange('search', e.target.value)}
            className="search-input"
          />
        </div>

        {/* Statut */}
        <div className="filter-group">
          <label>Statut</label>
          <select
            value={filters.status ?? ''}
            onChange={(e) => onFilterChange('status', e.target.value ? parseInt(e.target.value) : null)}
          >
            <option value="">Tous</option>
            <option value="0">🆕 Nouvelles</option>
            <option value="1">⏳ En cours</option>
            <option value="2">✅ Résolues</option>
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

### Table des Réclamations

```javascript
// ReclamationsTable.jsx
import React, { useState } from 'react';

export function ReclamationsTable({ reclamations, onRefresh }) {
  const [selectedReclamation, setSelectedReclamation] = useState(null);

  const getStatusBadge = (status) => {
    const statusMap = {
      0: { label: 'Nouvelle', color: 'red', icon: '🆕', priority: 'urgent' },
      1: { label: 'En cours', color: 'yellow', icon: '⏳', priority: 'normal' },
      2: { label: 'Résolue', color: 'green', icon: '✅', priority: 'low' }
    };
    return statusMap[status] || statusMap[0];
  };

  const getAge = (createdAt) => {
    const now = new Date();
    const created = new Date(createdAt);
    const diffMs = now - created;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 60) return `${diffMins} min`;
    if (diffHours < 24) return `${diffHours}h`;
    return `${diffDays}j`;
  };

  return (
    <>
      <div className="table-container">
        <table className="reclamations-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Date</th>
              <th>Client</th>
              <th>Contact</th>
              <th>Sujet</th>
              <th>Commande</th>
              <th>Âge</th>
              <th>Statut</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {reclamations.map(rec => {
              const statusBadge = getStatusBadge(rec.status);
              const age = getAge(rec.createdAt);
              const isUrgent = rec.status === 0 && age.includes('j');

              return (
                <tr
                  key={rec.id}
                  className={`reclamation-row status-${rec.status} ${isUrgent ? 'urgent' : ''}`}
                >
                  <td>
                    <strong>#{rec.id}</strong>
                  </td>
                  
                  <td>
                    {new Date(rec.createdAt).toLocaleDateString('fr-TN', {
                      day: '2-digit',
                      month: 'short',
                      hour: '2-digit',
                      minute: '2-digit'
                    })}
                  </td>
                  
                  <td>
                    <div className="customer-info">
                      <strong>{rec.fullName}</strong>
                    </div>
                  </td>

                  <td>
                    <div className="contact-info">
                      <div>{rec.email}</div>
                      {rec.phone && <small>{rec.phone}</small>}
                    </div>
                  </td>
                  
                  <td>
                    <div className="subject">
                      {rec.subject || <em className="no-subject">Sans sujet</em>}
                    </div>
                  </td>

                  <td>
                    {rec.orderId ? (
                      <a
                        href={`/admin/orders/${rec.orderId}`}
                        className="order-link"
                        onClick={(e) => {
                          e.preventDefault();
                          navigateTo(`/admin/orders/${rec.orderId}`);
                        }}
                      >
                        🔗 #{rec.orderId}
                      </a>
                    ) : (
                      <span className="no-order">-</span>
                    )}
                  </td>
                  
                  <td>
                    <span className={`age ${isUrgent ? 'urgent-age' : ''}`}>
                      {age}
                      {isUrgent && ' ⚠️'}
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
                        onClick={() => setSelectedReclamation(rec)}
                        className="btn-icon"
                        title="Voir détails"
                      >
                        👁️
                      </button>
                      
                      {rec.status !== 2 && (
                        <QuickActionButton
                          reclamation={rec}
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
      {selectedReclamation && (
        <ReclamationDetailsModal
          reclamationId={selectedReclamation.id}
          onClose={() => setSelectedReclamation(null)}
          onUpdate={onRefresh}
        />
      )}
    </>
  );
}

// Bouton action rapide
function QuickActionButton({ reclamation, onUpdate }) {
  const actions = {
    0: { label: 'Prendre en charge', nextStatus: 1, icon: '✋' },
    1: { label: 'Résoudre', nextStatus: 2, icon: '✅' }
  };

  const action = actions[reclamation.status];
  if (!action) return null;

  const handleQuickAction = async () => {
    try {
      const response = await fetch(`/api/reclamations/${reclamation.id}`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${adminToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          status: action.nextStatus,
          adminNote: `Action rapide: ${action.label}`
        })
      });

      if (!response.ok) throw new Error('Erreur mise à jour');

      showNotification(`Réclamation ${action.label.toLowerCase()}`, 'success');
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

## Détails d'une Réclamation

### Modal de détails complets

```javascript
// ReclamationDetailsModal.jsx
import React, { useState, useEffect } from 'react';

export function ReclamationDetailsModal({ reclamationId, onClose, onUpdate }) {
  const [reclamation, setReclamation] = useState(null);
  const [loading, setLoading] = useState(true);
  const [isEditing, setIsEditing] = useState(false);

  useEffect(() => {
    fetchDetails();
  }, [reclamationId]);

  const fetchDetails = async () => {
    try {
      setLoading(true);
      const response = await fetch(`/api/reclamations/${reclamationId}`, {
        headers: { 'Authorization': `Bearer ${adminToken}` }
      });

      if (!response.ok) throw new Error('Erreur chargement');

      const data = await response.json();
      setReclamation(data);
    } catch (error) {
      console.error('Erreur:', error);
      showNotification('Erreur chargement réclamation', 'error');
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

  if (!reclamation) return null;

  const statusBadge = getStatusBadge(reclamation.status);
  const age = getAge(reclamation.createdAt);

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal-large" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Réclamation #{reclamation.id}</h2>
          <button onClick={onClose} className="btn-close">✕</button>
        </div>

        <div className="modal-body reclamation-details">
          {/* Informations principales */}
          <div className="info-grid">
            <InfoCard title="Statut">
              <span className={`badge badge-${statusBadge.color} badge-large`}>
                {statusBadge.icon} {statusBadge.label}
              </span>
              {reclamation.status !== 2 && (
                <button
                  onClick={() => setIsEditing(true)}
                  className="btn-link"
                >
                  Modifier
                </button>
              )}
            </InfoCard>

            <InfoCard title="Date de réception">
              <div>
                {new Date(reclamation.createdAt).toLocaleString('fr-TN', {
                  day: '2-digit',
                  month: 'long',
                  year: 'numeric',
                  hour: '2-digit',
                  minute: '2-digit'
                })}
              </div>
              <small className="age-indicator">Il y a {age}</small>
            </InfoCard>

            <InfoCard title="Commande liée">
              {reclamation.orderId ? (
                <a
                  href={`/admin/orders/${reclamation.orderId}`}
                  className="order-link"
                  onClick={(e) => {
                    e.preventDefault();
                    navigateTo(`/admin/orders/${reclamation.orderId}`);
                  }}
                >
                  🔗 Commande #{reclamation.orderId}
                </a>
              ) : (
                <span className="no-data">Aucune commande liée</span>
              )}
            </InfoCard>
          </div>

          {/* Informations client */}
          <div className="section">
            <h3>👤 Informations Client</h3>
            <div className="customer-details">
              <div className="detail-row">
                <span className="label">Nom complet:</span>
                <span className="value"><strong>{reclamation.fullName}</strong></span>
              </div>
              <div className="detail-row">
                <span className="label">Email:</span>
                <span className="value">
                  <a href={`mailto:${reclamation.email}`}>{reclamation.email}</a>
                </span>
              </div>
              {reclamation.phone && (
                <div className="detail-row">
                  <span className="label">Téléphone:</span>
                  <span className="value">
                    <a href={`tel:${reclamation.phone}`}>{reclamation.phone}</a>
                  </span>
                </div>
              )}
            </div>
          </div>

          {/* Sujet */}
          {reclamation.subject && (
            <div className="section">
              <h3>📌 Sujet</h3>
              <div className="subject-box">
                {reclamation.subject}
              </div>
            </div>
          )}

          {/* Message */}
          <div className="section">
            <h3>💬 Message du client</h3>
            <div className="message-box">
              <p className="message-content">{reclamation.message}</p>
            </div>
          </div>

          {/* Note admin */}
          {reclamation.adminNote && (
            <div className="section">
              <h3>📝 Note Admin</h3>
              <div className="admin-note-box">
                {reclamation.adminNote}
              </div>
            </div>
          )}

          {/* Actions */}
          <div className="modal-actions">
            {reclamation.status === 0 && (
              <button
                onClick={() => handleStatusChange(1, 'Réclamation prise en charge')}
                className="btn-primary"
              >
                ✋ Prendre en charge
              </button>
            )}
            {reclamation.status === 1 && (
              <button
                onClick={() => handleStatusChange(2, 'Réclamation résolue')}
                className="btn-success"
              >
                ✅ Marquer comme résolue
              </button>
            )}
            <button onClick={() => setIsEditing(true)} className="btn-secondary">
              ✏️ Modifier note
            </button>
            <button onClick={handleDelete} className="btn-danger">
              🗑️ Supprimer
            </button>
          </div>
        </div>

        {/* Modal d'édition */}
        {isEditing && (
          <UpdateReclamationModal
            reclamation={reclamation}
            onClose={() => setIsEditing(false)}
            onUpdate={() => {
              setIsEditing(false);
              fetchDetails();
              onUpdate();
            }}
          />
        )}
      </div>
    </div>
  );

  async function handleStatusChange(newStatus, note) {
    try {
      const response = await fetch(`/api/reclamations/${reclamationId}`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${adminToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          status: newStatus,
          adminNote: note
        })
      });

      if (!response.ok) throw new Error('Erreur mise à jour');

      showNotification('Statut mis à jour', 'success');
      fetchDetails();
      onUpdate();
    } catch (error) {
      console.error('Erreur:', error);
      showNotification('Erreur mise à jour', 'error');
    }
  }

  async function handleDelete() {
    if (!confirm('Êtes-vous sûr de vouloir supprimer cette réclamation ?')) {
      return;
    }

    try {
      const response = await fetch(`/api/reclamations/${reclamationId}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${adminToken}` }
      });

      if (!response.ok) throw new Error('Erreur suppression');

      showNotification('Réclamation supprimée', 'success');
      onClose();
      onUpdate();
    } catch (error) {
      console.error('Erreur:', error);
      showNotification('Erreur suppression', 'error');
    }
  }
}

function InfoCard({ title, children }) {
  return (
    <div className="info-card">
      <h4>{title}</h4>
      {children}
    </div>
  );
}

function getStatusBadge(status) {
  const statusMap = {
    0: { label: 'Nouvelle', color: 'red', icon: '🆕' },
    1: { label: 'En cours', color: 'yellow', icon: '⏳' },
    2: { label: 'Résolue', color: 'green', icon: '✅' }
  };
  return statusMap[status] || statusMap[0];
}

function getAge(createdAt) {
  const now = new Date();
  const created = new Date(createdAt);
  const diffMs = now - created;
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 60) return `${diffMins} minutes`;
  if (diffHours < 24) return `${diffHours} heures`;
  return `${diffDays} jours`;
}
```

---

## Gestion des Statuts

### Modal de mise à jour

```javascript
// UpdateReclamationModal.jsx
import React, { useState } from 'react';

export function UpdateReclamationModal({ reclamation, onClose, onUpdate }) {
  const [newStatus, setNewStatus] = useState(reclamation.status);
  const [adminNote, setAdminNote] = useState(reclamation.adminNote || '');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const statusOptions = [
    { value: 0, label: '🆕 Nouvelle', description: 'Réclamation non traitée' },
    { value: 1, label: '⏳ En cours', description: 'Traitement en cours' },
    { value: 2, label: '✅ Résolue', description: 'Problème résolu' }
  ];

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!adminNote.trim() && newStatus === 2) {
      showNotification('Veuillez ajouter une note pour résoudre la réclamation', 'warning');
      return;
    }

    try {
      setIsSubmitting(true);

      const response = await fetch(`/api/reclamations/${reclamation.id}`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${adminToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          status: newStatus,
          adminNote: adminNote.trim() || null
        })
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.detail || 'Erreur mise à jour');
      }

      showNotification('Réclamation mise à jour avec succès', 'success');
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
          <h3>Mettre à jour la réclamation</h3>
          <button onClick={onClose} className="btn-close">✕</button>
        </div>

        <form onSubmit={handleSubmit} className="modal-body">
          <div className="form-group">
            <label htmlFor="newStatus">Statut *</label>
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
            <small className="form-hint">
              {statusOptions.find(o => o.value === newStatus)?.description}
            </small>
          </div>

          <div className="form-group">
            <label htmlFor="adminNote">
              Note admin {newStatus === 2 && <span className="required">*</span>}
            </label>
            <textarea
              id="adminNote"
              value={adminNote}
              onChange={(e) => setAdminNote(e.target.value)}
              placeholder="Actions effectuées, résolution du problème..."
              rows={5}
              maxLength={600}
              className="form-control"
              required={newStatus === 2}
            />
            <small>{adminNote.length} / 600 caractères</small>
          </div>

          <div className="form-info">
            {newStatus === 1 && (
              <div className="alert alert-info">
                ℹ️ Marquer comme "En cours" signifie que vous prenez en charge cette réclamation.
              </div>
            )}
            {newStatus === 2 && (
              <div className="alert alert-success">
                ✅ Marquer comme "Résolue" fermera définitivement cette réclamation.
              </div>
            )}
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

### Workflow recommandé

```javascript
// ReclamationWorkflow.jsx
export function ReclamationWorkflow({ reclamation, onUpdate }) {
  const steps = [
    {
      status: 0,
      label: 'Nouvelle',
      icon: '🆕',
      description: 'Réclamation reçue, nécessite une prise en charge',
      actions: [
        {
          label: 'Prendre en charge',
          nextStatus: 1,
          note: 'Réclamation prise en charge par l\'équipe support'
        }
      ]
    },
    {
      status: 1,
      label: 'En cours',
      icon: '⏳',
      description: 'Traitement en cours, investigation du problème',
      actions: [
        {
          label: 'Résoudre',
          nextStatus: 2,
          note: '' // Note obligatoire - formulaire
        }
      ]
    },
    {
      status: 2,
      label: 'Résolue',
      icon: '✅',
      description: 'Problème résolu, cas fermé',
      actions: []
    }
  ];

  const currentStep = steps.find(s => s.status === reclamation.status);
  const currentIndex = steps.findIndex(s => s.status === reclamation.status);

  const handleAction = async (action) => {
    if (action.nextStatus === 2) {
      // Formulaire de résolution
      const note = prompt('Veuillez décrire la résolution du problème:');
      if (!note || !note.trim()) {
        showNotification('Note obligatoire pour résoudre la réclamation', 'warning');
        return;
      }
      action.note = note.trim();
    }

    try {
      const response = await fetch(`/api/reclamations/${reclamation.id}`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${adminToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          status: action.nextStatus,
          adminNote: action.note
        })
      });

      if (!response.ok) throw new Error('Erreur mise à jour');

      showNotification('Statut mis à jour', 'success');
      onUpdate();
    } catch (error) {
      console.error('Erreur:', error);
      showNotification('Erreur mise à jour statut', 'error');
    }
  };

  return (
    <div className="workflow-panel">
      <h3>Workflow de traitement</h3>
      
      <div className="workflow-steps">
        {steps.map((step, index) => (
          <div
            key={step.status}
            className={`workflow-step ${
              index < currentIndex ? 'completed' :
              index === currentIndex ? 'current' : 'pending'
            }`}
          >
            <div className="step-icon">{step.icon}</div>
            <div className="step-content">
              <div className="step-label">{step.label}</div>
              <div className="step-description">{step.description}</div>
              
              {index === currentIndex && step.actions.length > 0 && (
                <div className="step-actions">
                  {step.actions.map((action, i) => (
                    <button
                      key={i}
                      onClick={() => handleAction(action)}
                      className="btn-action"
                    >
                      {action.label}
                    </button>
                  ))}
                </div>
              )}
              
              {index < currentIndex && (
                <div className="step-completed">✓ Terminé</div>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
```

---

## Notifications et Priorités

### Système d'alertes automatiques

```javascript
// ReclamationAlerts.jsx
import React, { useState, useEffect } from 'react';

export function ReclamationAlerts() {
  const [alerts, setAlerts] = useState([]);

  useEffect(() => {
    checkAlerts();
    
    // Vérifier toutes les 2 minutes
    const interval = setInterval(checkAlerts, 120000);
    return () => clearInterval(interval);
  }, []);

  const checkAlerts = async () => {
    const newAlerts = [];

    // Nouvelles réclamations non traitées
    const newRecs = await fetchReclamations({ status: 0, pageSize: 100 });
    
    if (newRecs.totalCount > 0) {
      // Vérifier l'âge
      const urgent = newRecs.items.filter(r => {
        const age = Date.now() - new Date(r.createdAt).getTime();
        return age > 60 * 60 * 1000; // > 1 heure
      });

      if (urgent.length > 0) {
        newAlerts.push({
          id: 'new-urgent',
          type: 'urgent',
          title: 'Réclamations urgentes',
          message: `${urgent.length} nouvelle(s) réclamation(s) en attente depuis plus d'1 heure`,
          action: () => navigateTo('/admin/reclamations?status=0')
        });
      } else if (newRecs.totalCount > 5) {
        newAlerts.push({
          id: 'new-many',
          type: 'warning',
          title: 'Nouvelles réclamations',
          message: `${newRecs.totalCount} nouvelle(s) réclamation(s) en attente`,
          action: () => navigateTo('/admin/reclamations?status=0')
        });
      }
    }

    // Réclamations en cours trop longtemps
    const inProgress = await fetchReclamations({ status: 1, pageSize: 100 });
    const longInProgress = inProgress.items.filter(r => {
      const age = Date.now() - new Date(r.createdAt).getTime();
      return age > 24 * 60 * 60 * 1000; // > 24 heures
    });

    if (longInProgress.length > 0) {
      newAlerts.push({
        id: 'inprogress-long',
        type: 'warning',
        title: 'Réclamations en cours longue durée',
        message: `${longInProgress.length} réclamation(s) en cours depuis plus de 24h`,
        action: () => navigateTo('/admin/reclamations?status=1')
      });
    }

    setAlerts(newAlerts);
  };

  const fetchReclamations = async (params) => {
    const queryString = new URLSearchParams(params).toString();
    const response = await fetch(`/api/reclamations?${queryString}`, {
      headers: { 'Authorization': `Bearer ${adminToken}` }
    });
    return await response.json();
  };

  if (alerts.length === 0) return null;

  return (
    <div className="reclamation-alerts">
      {alerts.map(alert => (
        <div key={alert.id} className={`alert alert-${alert.type}`}>
          <span className="alert-icon">
            {alert.type === 'urgent' ? '🚨' : '⚠️'}
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

## Statistiques et Rapports

### Page de rapports

```javascript
// ReclamationsReports.jsx
import React, { useState } from 'react';

export function ReclamationsReports() {
  const [dateRange, setDateRange] = useState({
    from: new Date(new Date().setDate(new Date().getDate() - 30)).toISOString().split('T')[0],
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
        `/api/reclamations?from=${from.toISOString()}&to=${to.toISOString()}&pageSize=1000`,
        { headers: { 'Authorization': `Bearer ${adminToken}` } }
      );

      if (!response.ok) throw new Error('Erreur génération rapport');

      const data = await response.json();

      // Statistiques
      const stats = {
        total: data.totalCount,
        new: data.items.filter(r => r.status === 0).length,
        inProgress: data.items.filter(r => r.status === 1).length,
        resolved: data.items.filter(r => r.status === 2).length,
        withOrder: data.items.filter(r => r.orderId).length,
        averageResolutionTime: 0,
        topSubjects: {}
      };

      // Temps de résolution moyen
      const resolved = data.items.filter(r => r.status === 2);
      if (resolved.length > 0) {
        const totalTime = resolved.reduce((sum, r) => {
          const created = new Date(r.createdAt);
          const now = new Date();
          return sum + (now - created) / 1000 / 60 / 60; // heures
        }, 0);
        stats.averageResolutionTime = totalTime / resolved.length;
      }

      // Top sujets
      data.items.forEach(r => {
        const subject = r.subject || 'Sans sujet';
        stats.topSubjects[subject] = (stats.topSubjects[subject] || 0) + 1;
      });

      setReport({ stats, reclamations: data.items });
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
      ['ID', 'Date', 'Nom', 'Email', 'Téléphone', 'Sujet', 'Statut', 'Commande'].join(','),
      ...report.reclamations.map(r => [
        r.id,
        new Date(r.createdAt).toLocaleString('fr-TN'),
        r.fullName,
        r.email,
        r.phone || '',
        r.subject || '',
        getStatusLabel(r.status),
        r.orderId || ''
      ].join(','))
    ].join('\n');

    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `rapport-reclamations-${dateRange.from}-${dateRange.to}.csv`;
    link.click();
  };

  return (
    <div className="reports-page">
      <h1>Rapports - Réclamations</h1>

      <div className="report-config">
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
            <StatCard title="Total" value={report.stats.total} color="blue" />
            <StatCard title="Nouvelles" value={report.stats.new} color="red" />
            <StatCard title="En cours" value={report.stats.inProgress} color="yellow" />
            <StatCard title="Résolues" value={report.stats.resolved} color="green" />
            <StatCard
              title="Temps moyen de résolution"
              value={`${report.stats.averageResolutionTime.toFixed(1)}h`}
              color="purple"
            />
            <StatCard
              title="Liées à des commandes"
              value={`${report.stats.withOrder} (${((report.stats.withOrder / report.stats.total) * 100).toFixed(0)}%)`}
              color="teal"
            />
          </div>

          <div className="top-subjects">
            <h3>Sujets les plus fréquents</h3>
            <div className="subjects-list">
              {Object.entries(report.stats.topSubjects)
                .sort(([, a], [, b]) => b - a)
                .slice(0, 10)
                .map(([subject, count]) => (
                  <div key={subject} className="subject-item">
                    <span className="subject-name">{subject}</span>
                    <span className="subject-count">{count}</span>
                  </div>
                ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );

  function getStatusLabel(status) {
    const labels = { 0: 'Nouvelle', 1: 'En cours', 2: 'Résolue' };
    return labels[status] || 'Inconnu';
  }
}
```

---

## Référence API Complète

### Endpoints Admin

#### 1. **Liste des réclamations (avec filtres)**

```http
GET /api/reclamations
Authorization: Bearer {adminToken}
```

**Query Parameters:**

| Paramètre | Type | Requis | Description |
|-----------|------|--------|-------------|
| status | int | Non | Filtrer par statut (0=New, 1=InProgress, 2=Resolved) |
| search | string | Non | Rechercher dans nom, email, sujet |
| from | datetime | Non | Date de début (ISO 8601) |
| to | datetime | Non | Date de fin (ISO 8601) |
| page | int | Non | Numéro de page (défaut: 1) |
| pageSize | int | Non | Taille de page (1-100, défaut: 20) |

**Réponse 200:**

```json
{
  "items": [
    {
      "id": 1,
      "fullName": "Jean Dupont",
      "email": "jean@example.com",
      "phone": "+216 12 345 678",
      "subject": "Problème de livraison",
      "status": 0,
      "orderId": 123,
      "createdAt": "2026-02-11T10:30:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 45
}
```

#### 2. **Détails d'une réclamation**

```http
GET /api/reclamations/{id}
Authorization: Bearer {adminToken}
```

**Réponse 200:**

```json
{
  "id": 1,
  "fullName": "Jean Dupont",
  "email": "jean@example.com",
  "phone": "+216 12 345 678",
  "subject": "Problème de livraison",
  "message": "Ma commande n'est pas arrivée après 5 jours...",
  "status": 1,
  "orderId": 123,
  "adminNote": "Contact client effectué, livraison en cours de vérification",
  "createdAt": "2026-02-11T10:30:00Z"
}
```

#### 3. **Mettre à jour une réclamation**

```http
PUT /api/reclamations/{id}
Authorization: Bearer {adminToken}
Content-Type: application/json
```

**Body:**

```json
{
  "status": 2,
  "adminNote": "Problème résolu. Livraison effectuée avec succès le 12/02/2026."
}
```

**Validation:**
- `status` (requis): 0, 1, ou 2
- `adminNote` (optionnel): max 600 caractères

**Réponse 204:** No Content

#### 4. **Supprimer une réclamation (soft delete)**

```http
DELETE /api/reclamations/{id}
Authorization: Bearer {adminToken}
```

**Réponse 204:** No Content

### Erreurs

Format RFC 7807:

```json
{
  "status": 404,
  "title": "Introuvable",
  "detail": "Réclamation introuvable.",
  "instance": "/api/reclamations/999",
  "traceId": "00-abc123..."
}
```

---

## Bonnes Pratiques

### 1. **Temps de réponse**

```javascript
// SLA recommandé
const SLA = {
  NEW: 60, // minutes - prendre en charge dans l'heure
  IN_PROGRESS: 24 * 60, // minutes - résoudre en 24h
  URGENT: 30 // minutes - cas urgents
};

// Vérifier SLA
function checkSLA(reclamation) {
  const age = (Date.now() - new Date(reclamation.createdAt)) / 60000; // minutes
  
  if (reclamation.status === 0 && age > SLA.NEW) {
    return { breached: true, message: 'SLA dépassé: prise en charge requise' };
  }
  
  if (reclamation.status === 1 && age > SLA.IN_PROGRESS) {
    return { breached: true, message: 'SLA dépassé: résolution urgente' };
  }
  
  return { breached: false };
}
```

### 2. **Modèles de réponses**

```javascript
// Templates pour notes admin
const RESPONSE_TEMPLATES = {
  TAKEN: "Réclamation prise en charge. Investigation en cours.",
  
  ORDER_ISSUE: "Problème de commande identifié. Contact avec le client et l'équipe logistique effectué.",
  
  RESOLVED_REFUND: "Problème résolu. Remboursement effectué le {date}. Client satisfait.",
  
  RESOLVED_REPLACEMENT: "Problème résolu. Article de remplacement envoyé. Numéro de suivi: {tracking}.",
  
  RESOLVED_EXPLANATION: "Situation clarifiée avec le client. Malentendu résolu.",
  
  SPAM: "Réclamation identifiée comme spam ou doublon."
};

// Utilisation
function useTemplate(template, vars = {}) {
  let text = RESPONSE_TEMPLATES[template];
  Object.entries(vars).forEach(([key, value]) => {
    text = text.replace(`{${key}}`, value);
  });
  return text;
}
```

### 3. **Logs et audit**

```javascript
// Logger toutes les actions admin
async function logReclamationAction(action, reclamationId, details) {
  try {
    await fetch('/api/admin/logs', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${adminToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        action,
        resource: 'reclamation',
        resourceId: reclamationId,
        details,
        timestamp: new Date().toISOString()
      })
    });
  } catch (error) {
    console.error('Erreur log:', error);
  }
}

// Utilisation
async function handleStatusChange(recId, newStatus, note) {
  await updateReclamationStatus(recId, newStatus, note);
  await logReclamationAction('status_change', recId, { newStatus, note });
}
```

### 4. **Intégration email (optionnel)**

```javascript
// Notifier client par email
async function notifyCustomer(reclamation, template) {
  try {
    await fetch('/api/admin/emails/send', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${adminToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        to: reclamation.email,
        template: template,
        data: {
          fullName: reclamation.fullName,
          reclamationId: reclamation.id,
          adminNote: reclamation.adminNote
        }
      })
    });
  } catch (error) {
    console.error('Erreur envoi email:', error);
  }
}

// Templates email
const EMAIL_TEMPLATES = {
  TAKEN: 'reclamation_taken',
  RESOLVED: 'reclamation_resolved',
  UPDATE: 'reclamation_update'
};
```

---

## ✅ Checklist d'implémentation

### Interface de base
- [ ] Dashboard avec statistiques en temps réel
- [ ] Liste des réclamations avec pagination
- [ ] Filtres avancés (statut, recherche, dates)
- [ ] Modal/Page détails réclamation
- [ ] Modal mise à jour statut + note
- [ ] Suppression avec confirmation

### Fonctionnalités avancées
- [ ] Actions rapides (prendre en charge, résoudre)
- [ ] Système d'alertes automatiques (SLA)
- [ ] Lien vers commande associée
- [ ] Générateur de rapports
- [ ] Export CSV

### Optimisations
- [ ] Refresh automatique intelligent
- [ ] Indicateurs visuels d'urgence (âge)
- [ ] Templates de réponses
- [ ] Raccourcis clavier
- [ ] Cache des stats

### Sécurité et traçabilité
- [ ] Vérification rôle Admin
- [ ] Logs des actions admin
- [ ] Validation des données
- [ ] Gestion des erreurs
- [ ] Note obligatoire pour résolution

---

## 📞 Support et Documentation

- 📖 **API Commandes:** [ORDER_API_DOCUMENTATION.md](ORDER_API_DOCUMENTATION.md)
- 👨‍💼 **Gestion Commandes Admin:** [ADMIN_ORDERS_MANAGEMENT.md](ADMIN_ORDERS_MANAGEMENT.md)
- 📦 **API Stock:** [STOCK_API_DOCUMENTATION.md](STOCK_API_DOCUMENTATION.md)
- 🛒 **API Cart:** [CART_API_DOCUMENTATION.md](CART_API_DOCUMENTATION.md)

---

**Dernière mise à jour:** 11 février 2026  
**Version:** 1.0  
**Maintenu par:** Équipe Backend Hlouwa
