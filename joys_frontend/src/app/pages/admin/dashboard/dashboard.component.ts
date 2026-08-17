import { Component, OnInit } from '@angular/core';
import { OrderService } from '../../../core/services/order.service';
import { StockService } from '../../../core/services/stock.service';
import { Order, ORDER_STATUS_MAP, ORDER_STATUS_LABELS } from '../../../core/models/order.models';
import { StockItem } from '../../../core/models/stock.models';

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './dashboard.component.html',
  standalone: false,
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  loading = true;
  stats = {
    todayOrders: 0,
    todayRevenue: 0,
    pendingOrders: 0
  };
  recentOrders: any[] = [];
  lowStockItems: any[] = [];

  constructor(
    private orderService: OrderService,
    private stockService: StockService
  ) { }

  ngOnInit() {
    this.loadDashboardData();
  }

  getStatusLabel(status: number): string {
    const key = ORDER_STATUS_MAP[status];
    return key ? (ORDER_STATUS_LABELS[key] || key) : `${status}`;
  }

  getStatusClass(status: number): string {
    const key = ORDER_STATUS_MAP[status];
    return key ? key.toLowerCase() : '';
  }

  loadDashboardData() {
    this.loading = true;

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const todayISO = today.toISOString();

    this.orderService.getOrders({ page: 1, pageSize: 100 }).subscribe({
      next: (res) => {
        this.recentOrders = res.items.slice(0, 5).map((o: Order) => ({
          orderNumber: o.orderNumber,
          customerName: o.customerFullName || o.customerEmail || 'Client',
          total: o.total,
          status: o.status
        }));

        const todayOrders = res.items.filter((o: Order) => {
          const orderDate = new Date(o.createdAt);
          orderDate.setHours(0, 0, 0, 0);
          return orderDate.toISOString() === todayISO;
        });

        this.stats.todayOrders = todayOrders.length;
        this.stats.todayRevenue = todayOrders.reduce((acc: number, curr: Order) => acc + curr.total, 0);
        this.stats.pendingOrders = res.items.filter((o: Order) => o.status === 0).length;

        this.loading = false;
      },
      error: () => this.loading = false
    });

    this.stockService.getStockItems().subscribe({
      next: (items: StockItem[]) => {
        this.lowStockItems = items
          .filter((i: StockItem) => i.available < 20)
          .slice(0, 5)
          .map((i: StockItem) => ({
            name: `Article #${i.articleId}`,
            categoryName: i.articleVariantId ? `Var: ${i.articleVariantId}` : 'Standard',
            quantity: i.available
          }));
      }
    });
  }
}
