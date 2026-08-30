import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, catchError, throwError, finalize, switchMap, debounceTime, distinctUntilChanged } from 'rxjs';
import { Product, StockMovement, MovementType, ApiError } from './models';

const API = 'http://localhost:5184/api';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);

  private productsSubject = new BehaviorSubject<Product[]>([]);
  private movementsSubject = new BehaviorSubject<StockMovement[]>([]);
  private searchSubject = new BehaviorSubject<string>('');
  private loadingSubject = new BehaviorSubject<boolean>(false);
  private errorSubject = new BehaviorSubject<ApiError | null>(null);

  readonly products$ = this.productsSubject.asObservable();
  readonly movements$ = this.movementsSubject.asObservable();
  readonly search$ = this.searchSubject.asObservable();
  readonly loading$ = this.loadingSubject.asObservable();
  readonly error$ = this.errorSubject.asObservable();

  readonly searchResults$ = this.searchSubject.pipe(
    debounceTime(300),
    distinctUntilChanged(),
    switchMap(term => this.fetch(term))
  );

  setSearch(term: string): void {
    this.searchSubject.next(term);
  }

  clearError(): void {
    this.errorSubject.next(null);
  }

  fetch(search = ''): Observable<Product[]> {
    this.loadingSubject.next(true);

    let params = new HttpParams();
    if (search.trim()) params = params.set('search', search.trim());

    return this.http.get<Product[]>(`${API}/products`, { params }).pipe(
      tap(list => this.productsSubject.next(list)),
      catchError(err => this.handle(err)),
      finalize(() => this.loadingSubject.next(false))
    );
  }

  loadMovements(productId: string): Observable<StockMovement[]> {
    return this.http.get<StockMovement[]>(`${API}/products/${productId}/movements`).pipe(
      tap(list => this.movementsSubject.next(list)),
      catchError(err => this.handle(err))
    );
  }

  create(payload: { name: string; sku: string; price: number; category: string | null }) {
    this.errorSubject.next(null);
    return this.http.post<string>(`${API}/products`, payload).pipe(
      catchError(err => this.handle(err))
    );
  }

  update(id: string, payload: { name: string; sku: string; price: number; category: string | null }) {
    this.errorSubject.next(null);
    return this.http.put<void>(`${API}/products/${id}`, { id, ...payload }).pipe(
      catchError(err => this.handle(err))
    );
  }

  delete(id: string) {
    this.errorSubject.next(null);
    return this.http.delete<void>(`${API}/products/${id}`).pipe(
      catchError(err => this.handle(err))
    );
  }

  deactivate(id: string) {
    this.errorSubject.next(null);
    return this.http.post<void>(`${API}/products/${id}/deactivate`, {}).pipe(
      catchError(err => this.handle(err))
    );
  }

  adjustStock(productId: string, movementType: MovementType, quantity: number, note: string | null) {
    this.errorSubject.next(null);
    return this.http.post<number>(`${API}/products/${productId}/adjust-stock`, {
      productId, movementType, quantity, note
    }).pipe(
      catchError(err => this.handle(err))
    );
  }

  private handle(err: any) {
    this.errorSubject.next(err.error as ApiError);
    return throwError(() => err);
  }
}