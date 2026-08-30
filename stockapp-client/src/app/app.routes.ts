import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { productsResolver } from './core/products.resolver';

export const routes: Routes = [
  { path: '', redirectTo: 'products', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'products',
    canActivate: [authGuard],
    resolve: { products: productsResolver },
    loadComponent: () => import('./features/products/products-page.component').then(m => m.ProductsPageComponent)
  }
];