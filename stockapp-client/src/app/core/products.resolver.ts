import { ResolveFn } from '@angular/router';
import { inject } from '@angular/core';
import { ProductService } from './product.service';
import { Product } from './models';

export const productsResolver: ResolveFn<Product[]> = () =>
  inject(ProductService).fetch();