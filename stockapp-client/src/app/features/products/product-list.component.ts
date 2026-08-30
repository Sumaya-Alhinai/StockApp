import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Product } from '../../core/models';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <table>
      <thead>
        <tr>
          <th>Name</th><th>SKU</th><th>Price</th><th>Category</th>
          <th>Stock</th><th>Status</th><th></th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let p of products" [class.inactive]="!p.isActive">
          <td>{{ p.name }}</td>
          <td>{{ p.sku }}</td>
          <td>{{ p.price | number:'1.2-2' }}</td>
          <td>{{ p.category || '—' }}</td>
          <td>{{ p.stockOnHand }}</td>
          <td>{{ p.isActive ? 'Active' : 'Inactive' }}</td>
          <td class="actions">
            <button (click)="edit.emit(p)">Edit</button>
            <button (click)="stock.emit(p)">Stock</button>
            <button (click)="remove.emit(p)">Delete</button>
          </td>
        </tr>
        <tr *ngIf="products.length === 0">
          <td colspan="7" class="empty">No products found.</td>
        </tr>
      </tbody>
    </table>
  `,
  styles: [`
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: .5rem; border-bottom: 1px solid #ddd; text-align: left; }
    th { background: #f5f5f5; }
    .inactive { opacity: .5; }
    .actions button { margin-right: .25rem; cursor: pointer; }
    .empty { text-align: center; color: #888; padding: 1.5rem; }
  `]
})
export class ProductListComponent {
  @Input() products: Product[] = [];
  @Output() edit = new EventEmitter<Product>();
  @Output() stock = new EventEmitter<Product>();
  @Output() remove = new EventEmitter<Product>();
}