import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../core/product.service';
import { AuthService } from '../../core/auth.service';
import { Product, MovementType } from '../../core/models';
import { ProductListComponent } from './product-list.component';
import { ProductFormComponent } from './product-form.component';
import { StockPanelComponent } from './stock-panel.component';

@Component({
  selector: 'app-products-page',
  standalone: true,
  imports: [CommonModule, FormsModule, ProductListComponent, ProductFormComponent, StockPanelComponent],
  template: `
    <div class="page">
      <header>
        <h2>Products</h2>
        <button (click)="auth.logout()">Logout</button>
      </header>

      <div class="toolbar">
        <input
          type="text"
          placeholder="Search name, SKU or category..."
          [ngModel]="searchTerm"
          (ngModelChange)="onSearch($event)" />
        <button (click)="newProduct()">+ New product</button>
      </div>

      <div class="alert" *ngIf="products.error$ | async as err" [class.warn]="isRetryable(err.code)">
        <strong>{{ label(err.code) }}</strong>
        <span>{{ err.message }}</span>
        <button (click)="products.clearError()">×</button>
      </div>

      <p class="loading" *ngIf="products.loading$ | async">Loading...</p>

      <app-product-form
        *ngIf="formOpen"
        [product]="editing"
        (save)="onSave($event)"
        (cancel)="closeForm()" />

      <app-stock-panel
        *ngIf="stockFor"
        [product]="stockFor"
        [movements]="(products.movements$ | async) ?? []"
        (adjust)="onAdjust($event)"
        (close)="stockFor = null" />

      <app-product-list
        [products]="(products.products$ | async) ?? []"
        (edit)="onEdit($event)"
        (stock)="onStock($event)"
        (remove)="onRemove($event)" />
    </div>
  `,
  styles: [`
    .page { max-width: 960px; margin: 2rem auto; font-family: system-ui; padding: 0 1rem; }
    header { display: flex; justify-content: space-between; align-items: center; }
    .toolbar { display: flex; gap: .5rem; margin: 1rem 0; }
    .toolbar input { flex: 1; padding: .5rem; }
    button { padding: .45rem .9rem; cursor: pointer; }
    .alert { display: flex; gap: .5rem; align-items: center; background: #fdecea; color: #922; padding: .6rem; border-radius: 4px; margin-bottom: 1rem; }
    .alert.warn { background: #fff6e0; color: #8a6100; }
    .alert button { margin-left: auto; border: none; background: none; font-size: 1.2rem; }
    .loading { color: #888; }
  `]
})
export class ProductsPageComponent implements OnInit {
  products = inject(ProductService);
  auth = inject(AuthService);

  searchTerm = '';
  formOpen = false;
  editing: Product | null = null;
  stockFor: Product | null = null;

  ngOnInit(): void {
    this.products.searchResults$.subscribe();
  }

  onSearch(term: string): void {
    this.searchTerm = term;
    this.products.setSearch(term);
  }

  label(code: string): string {
    switch (code) {
      case 'INSUFFICIENT_STOCK': return 'Insufficient stock';
      case 'CONCURRENCY_CONFLICT': return 'Conflict — please retry';
      case 'DELETE_BLOCKED': return 'Cannot delete';
      case 'DUPLICATE_SKU': return 'Duplicate SKU';
      case 'NOT_FOUND': return 'Not found';
      default: return 'Error';
    }
  }

  isRetryable(code: string): boolean {
    return code === 'CONCURRENCY_CONFLICT' || code === 'INSUFFICIENT_STOCK';
  }

  newProduct(): void {
    this.editing = null;
    this.formOpen = true;
  }

  onEdit(p: Product): void {
    this.editing = p;
    this.formOpen = true;
  }

  closeForm(): void {
    this.formOpen = false;
    this.editing = null;
  }

  onSave(payload: { name: string; sku: string; price: number; category: string | null }): void {
    const done = () => { this.closeForm(); this.reload(); };

    if (this.editing) {
      this.products.update(this.editing.id, payload).subscribe({
        next: done,
        error: () => {}
      });
    } else {
      this.products.create(payload).subscribe({
        next: done,
        error: () => {}
      });
    }
  }

  onStock(p: Product): void {
    this.stockFor = p;
    this.products.loadMovements(p.id).subscribe();
  }

  onAdjust(e: { movementType: MovementType; quantity: number; note: string | null }): void {
    if (!this.stockFor) return;
    const id = this.stockFor.id;

    this.products.adjustStock(id, e.movementType, e.quantity, e.note).subscribe({
      next: () => {
        this.products.loadMovements(id).subscribe();
        this.reload(() => {
          this.stockFor = (this.currentList().find(p => p.id === id)) ?? null;
        });
      },
      error: () => {}
    });
  }

  onRemove(p: Product): void {
    this.products.delete(p.id).subscribe({
      next: () => this.reload(),
      error: (err) => {
        if (err?.error?.code === 'DELETE_BLOCKED') {
          this.products.deactivate(p.id).subscribe({ next: () => this.reload() });
        }
      }
    });
  }

  private currentList(): Product[] {
    let list: Product[] = [];
    this.products.products$.subscribe(l => list = l).unsubscribe();
    return list;
  }

  private reload(after?: () => void): void {
    this.products.fetch(this.searchTerm).subscribe({
      next: () => after?.(),
      error: () => {}
    });
  }
}