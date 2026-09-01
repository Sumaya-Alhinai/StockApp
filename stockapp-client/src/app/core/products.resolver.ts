import { ResolveFn } from '@angular/router';
import { inject } from '@angular/core';
import { ProductService } from './product.service';
import { Product } from './models';
import { retry, catchError } from 'rxjs/operators';
import { of } from 'rxjs';


 
export const productsResolver: ResolveFn<Product[]> = () =>
  inject(ProductService).fetch().pipe(
    retry({ count: 2, delay: 500 }),
   
    catchError(() => {
      console.error('Failed to load products after retries');
      return of([]);
    })
  );