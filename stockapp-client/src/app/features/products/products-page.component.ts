import { Component, inject, OnInit, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ProductService } from '../../core/product.service';
import { AuthService } from '../../core/auth.service';
import { Product, MovementType } from '../../core/models';
import { ProductListComponent } from './product-list.component';
import { ProductFormComponent } from './product-form.component';
import { StockPanelComponent } from './stock-panel.component';
import { PaginationComponent } from './pagination.component';

@Component({
  selector: 'app-products-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ProductListComponent,
    ProductFormComponent,
    StockPanelComponent,
    PaginationComponent
  ],
  template: `
    <div class="page">
      <header>
        <h2>Products</h2>
        <button (click)="logout()">Logout</button>
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

      <app-pagination
        [page]="(products.page$ | async) ?? 1"
        [pageSize]="(products.pageSize$ | async) ?? 10"
        [totalCount]="(products.totalCount$ | async) ?? 0"
        [totalPages]="(products.totalPages$ | async) ?? 0"
        [disabled]="(products.loading$ | async) ?? false"
        (pageChange)="onPageChange($event)"
        (pageSizeChange)="onPageSizeChange($event)" />
    </div>
  `,
  styles: [`
    .page { max-width: 980px; margin: 2.5rem auto; padding: 0 1.25rem; }
    header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    header h2 { margin: 0; }
    .toolbar { display: flex; gap: .6rem; margin-bottom: 1.25rem; }
    .toolbar input { flex: 1; }
    .alert {
      display: flex;
      gap: .6rem;
      align-items: center;
      background: var(--danger-bg);
      color: var(--danger);
      padding: .75rem .9rem;
      border-radius: 6px;
      margin-bottom: 1.25rem;
      font-size: .92rem;
    }
    .alert.warn { background: var(--warn-bg); color: var(--warn); }
    .alert button { margin-left: auto; border: none; background: none; font-size: 1.3rem; line-height: 1; padding: 0 .3rem; color: inherit; }
    .loading { color: var(--muted); font-size: .9rem; }
  `]
})
export class ProductsPageComponent implements OnInit {
  private destroyRef = inject(DestroyRef);

  products = inject(ProductService);
  auth = inject(AuthService);

  searchTerm = '';
  formOpen = false;
  editing: Product | null = null;
  stockFor: Product | null = null;

  ngOnInit(): void {
    // ProductService is root-provided, so the search term outlives this
    // component. The resolver already fetched with that term — mirror it
    // into the input box so the field matches the rows being displayed.
    this.searchTerm = this.products.currentSearch;

    // The only stream here that never completes on its own, so the only one
    // that needs tearing down. HTTP calls complete after one value.
    this.products.searchResults$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => {} });
  }

  onSearch(term: string): void {
    this.searchTerm = term;
    this.products.setSearch(term);
  }

  onPageChange(page: number): void {
    this.products.goToPage(page).subscribe({ error: () => {} });
  }

  onPageSizeChange(size: number): void {
    this.products.setPageSize(size).subscribe({ error: () => {} });
  }

  logout(): void {
    this.products.reset();
    this.auth.logout();
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
    const isNew = !this.editing;

    const done = () => {
      this.closeForm();
      // A new product sorts first (CreatedAt descending), so jump to page 1 to see it.
      isNew
        ? this.products.goToPage(1).subscribe({ error: () => {} })
        : this.reload();
    };

    if (this.editing) {
      this.products.update(this.editing.id, payload)
        .subscribe({ next: done, error: () => {} });
    } else {
      this.products.create(payload)
        .subscribe({ next: done, error: () => {} });
    }
  }

  onStock(p: Product): void {
    this.stockFor = p;
    this.products.loadMovements(p.id).subscribe({ error: () => {} });
  }

  onAdjust(e: { movementType: MovementType; quantity: number; note: string | null }): void {
    if (!this.stockFor) return;

    const productId = this.stockFor.id;

    this.products.adjustStock(productId, e.movementType, e.quantity, e.note).subscribe({
      next: () => {
        this.products.loadMovements(productId).subscribe({ error: () => {} });

        // Read the refreshed list synchronously. Subscribing to products$ here
        // would register a new permanent listener on every adjustment.
        this.reload(() => {
          this.stockFor = this.products.currentProducts.find(p => p.id === productId) ?? null;
        });
      },
      error: () => {}
    });
  }

  onRemove(p: Product): void {
    this.products.delete(p.id).subscribe({
      next: () => this.reload(),
      error: (err) => {
        // DELETE_BLOCKED means nothing changed server-side. Deactivating is a
        // separate decision, so ask before sending the follow-up request.
        if (err?.error?.code !== 'DELETE_BLOCKED') return;

        const confirmed = confirm(
          `"${p.name}" has stock movement history and cannot be deleted. Deactivate it instead?`
        );
        if (!confirmed) return;

        this.products.deactivate(p.id).subscribe({
          next: () => this.reload(),
          error: () => {}
        });
      }
    });
  }

  private reload(after?: () => void): void {
    this.products.fetch().subscribe({
      next: items => {
        // Deleting the last row of the last page would leave an empty table.
        if (items.length === 0 && this.products.currentPage > 1) {
          this.products.goToPage(this.products.currentPage - 1)
            .subscribe({ next: () => after?.(), error: () => {} });
          return;
        }
        after?.();
      },
      error: () => {}
    });
  }
}