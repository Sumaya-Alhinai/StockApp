import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="pager" *ngIf="totalCount > 0">
      <span>Showing {{ firstItem }}–{{ lastItem }} of {{ totalCount }}</span>

      <div class="controls">
        <button type="button" [disabled]="page <= 1 || disabled" (click)="pageChange.emit(page - 1)">‹ Prev</button>
        <span class="position">Page {{ page }} of {{ totalPages }}</span>
        <button type="button" [disabled]="page >= totalPages || disabled" (click)="pageChange.emit(page + 1)">Next ›</button>

        <select [value]="pageSize" (change)="onPageSize($event)" [disabled]="disabled">
          <option [value]="5">5 / page</option>
          <option [value]="10">10 / page</option>
          <option [value]="25">25 / page</option>
        </select>
      </div>
    </div>
  `,
  styles: [`
    .pager { display: flex; align-items: center; justify-content: space-between;
             gap: 1rem; flex-wrap: wrap; margin-top: 1rem; font-size: .9rem; color: var(--muted); }
    .controls { display: flex; align-items: center; gap: .35rem; }
    .position { padding: 0 .6rem; white-space: nowrap; }
    .controls button { padding: .35rem .6rem; font-size: .85rem; }
    .controls button:disabled { opacity: .4; cursor: not-allowed; }
    .controls select { padding: .35rem .5rem; margin-left: .5rem; }
  `]
})
export class PaginationComponent {
  @Input() page = 1;
  @Input() pageSize = 10;
  @Input() totalCount = 0;
  @Input() totalPages = 0;
  @Input() disabled = false;

  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();

  get firstItem(): number { return this.totalCount === 0 ? 0 : (this.page - 1) * this.pageSize + 1; }
  get lastItem(): number { return Math.min(this.page * this.pageSize, this.totalCount); }

  onPageSize(event: Event): void {
    this.pageSizeChange.emit(Number((event.target as HTMLSelectElement).value));
  }
}