import { Component, EventEmitter, Input, Output, inject, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Product, StockMovement, MovementType } from '../../core/models';

@Component({
  selector: 'app-stock-panel',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="panel" *ngIf="product">
      <h3>Stock — {{ product.name }} <small>(on hand: {{ product.stockOnHand }})</small></h3>

      <form [formGroup]="form" (ngSubmit)="onSubmit()">
        <label>Type
          <select formControlName="movementType">
            <option [value]="1">In</option>
            <option [value]="2">Out</option>
          </select>
        </label>

        <label>Quantity <input type="number" formControlName="quantity" /></label>
        <p class="err" *ngIf="f.quantity.touched && f.quantity.invalid">
          <span *ngIf="f.quantity.errors?.['min']">Quantity must be greater than zero.</span>
          <span *ngIf="f.quantity.errors?.['max']">Cannot exceed {{ product.stockOnHand }} in stock.</span>
        </p>

        <label>Note <input formControlName="note" /></label>

        <div class="row">
          <button type="submit" [disabled]="form.invalid">Apply</button>
          <button type="button" (click)="close.emit()">Close</button>
        </div>
      </form>

      <h4>Movement history</h4>
      <table>
        <tr *ngFor="let m of movements">
          <td>{{ m.movementType === 1 ? 'In' : 'Out' }}</td>
          <td>{{ m.quantity }}</td>
          <td>{{ m.note || '—' }}</td>
          <td>{{ m.createdAt | date:'short' }}</td>
        </tr>
        <tr *ngIf="movements.length === 0">
          <td colspan="4" class="empty">No movements yet.</td>
        </tr>
      </table>
    </div>
  `,
  styles: [`
    .panel { border: 1px solid #ddd; padding: 1rem; margin-bottom: 1rem; border-radius: 4px; }
    label { display: block; margin-bottom: .5rem; }
    input, select { width: 100%; padding: .4rem; box-sizing: border-box; }
    .err { color: #c0392b; font-size: .85rem; margin: -.25rem 0 .5rem; }
    .row { display: flex; gap: .5rem; margin: .75rem 0; }
    button { padding: .4rem 1rem; cursor: pointer; }
    table { width: 100%; border-collapse: collapse; }
    td { padding: .4rem; border-bottom: 1px solid #eee; }
    .empty { text-align: center; color: #888; }
  `]
})
export class StockPanelComponent implements OnChanges {
  private fb = inject(FormBuilder);

  @Input() product: Product | null = null;
  @Input() movements: StockMovement[] = [];
  @Output() adjust = new EventEmitter<{ movementType: MovementType; quantity: number; note: string | null }>();
  @Output() close = new EventEmitter<void>();

  form = this.fb.nonNullable.group({
    movementType: [1, [Validators.required]],
    quantity: [1, [Validators.required, Validators.min(1)]],
    note: ['']
  });

  get f() { return this.form.controls; }

  ngOnChanges(): void {
    this.form.controls.movementType.valueChanges.subscribe(() => this.applyMaxRule());
    this.applyMaxRule();
  }

  private applyMaxRule(): void {
    const isOut = Number(this.form.controls.movementType.value) === 2;
    const max = this.product?.stockOnHand ?? 0;

    this.form.controls.quantity.setValidators(
      isOut
        ? [Validators.required, Validators.min(1), Validators.max(max)]
        : [Validators.required, Validators.min(1)]
    );
    this.form.controls.quantity.updateValueAndValidity({ emitEvent: false });
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.adjust.emit({
      movementType: Number(v.movementType) as MovementType,
      quantity: Number(v.quantity),
      note: v.note || null
    });
  }
}