import { Component, EventEmitter, Input, Output, inject, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Product } from '../../core/models';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <form [formGroup]="form" (ngSubmit)="onSubmit()" class="panel">
      <h3>{{ product ? 'Edit product' : 'New product' }}</h3>

      <label>Name <input formControlName="name" /></label>
      <p class="err" *ngIf="f.name.touched && f.name.invalid">Name is required.</p>

      <label>SKU <input formControlName="sku" /></label>
      <p class="err" *ngIf="f.sku.touched && f.sku.invalid">SKU is required.</p>

      <label>Price <input type="number" step="0.01" formControlName="price" /></label>
      <p class="err" *ngIf="f.price.touched && f.price.invalid">Price must be greater than zero.</p>

      <label>Category <input formControlName="category" /></label>

      <div class="row">
        <button type="submit" [disabled]="form.invalid">Save</button>
        <button type="button" (click)="cancel.emit()">Cancel</button>
      </div>
    </form>
  `,
  styles: [`
    .panel { border: 1px solid #ddd; padding: 1rem; margin-bottom: 1rem; border-radius: 4px; }
    label { display: block; margin-bottom: .5rem; }
    input { width: 100%; padding: .4rem; box-sizing: border-box; }
    .err { color: #c0392b; font-size: .85rem; margin: -.25rem 0 .5rem; }
    .row { display: flex; gap: .5rem; margin-top: .75rem; }
    button { padding: .4rem 1rem; cursor: pointer; }
  `]
})
export class ProductFormComponent implements OnChanges {
  private fb = inject(FormBuilder);

  @Input() product: Product | null = null;
  @Output() save = new EventEmitter<{ name: string; sku: string; price: number; category: string | null }>();
  @Output() cancel = new EventEmitter<void>();

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    sku: ['', [Validators.required, Validators.maxLength(64)]],
    price: [0, [Validators.required, Validators.min(0.01)]],
    category: ['']
  });

  get f() { return this.form.controls; }

  ngOnChanges(): void {
    if (this.product) {
      this.form.patchValue({
        name: this.product.name,
        sku: this.product.sku,
        price: this.product.price,
        category: this.product.category ?? ''
      });
    } else {
      this.form.reset({ name: '', sku: '', price: 0, category: '' });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.save.emit({ ...v, category: v.category || null });
  }
}