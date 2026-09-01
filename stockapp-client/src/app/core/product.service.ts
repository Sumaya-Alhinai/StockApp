import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
  BehaviorSubject, Observable,
  tap, map, catchError, throwError, finalize,
  switchMap, debounceTime, distinctUntilChanged
} from 'rxjs';
import { Product, StockMovement, MovementType, ApiError, PagedResult } from './models';

const API = 'http://localhost:5184/api';
const DEFAULT_PAGE_SIZE = 10;

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);

  private productsSubject = new BehaviorSubject<Product[]>([]);
  private movementsSubject = new BehaviorSubject<StockMovement[]>([]);
  private searchSubject = new BehaviorSubject<string>('');
  private loadingSubject = new BehaviorSubject<boolean>(false);
  private errorSubject = new BehaviorSubject<ApiError | null>(null);

  // ---- paging state ----
  private pageSubject = new BehaviorSubject<number>(1);
  private pageSizeSubject = new BehaviorSubject<number>(DEFAULT_PAGE_SIZE);
  private totalCountSubject = new BehaviorSubject<number>(0);
  private totalPagesSubject = new BehaviorSubject<number>(0);

  readonly products$ = this.productsSubject.asObservable();
  readonly movements$ = this.movementsSubject.asObservable();
  readonly search$ = this.searchSubject.asObservable();
  readonly loading$ = this.loadingSubject.asObservable();
  readonly error$ = this.errorSubject.asObservable();
  readonly page$ = this.pageSubject.asObservable();
  readonly pageSize$ = this.pageSizeSubject.asObservable();
  readonly totalCount$ = this.totalCountSubject.asObservable();
  readonly totalPages$ = this.totalPagesSubject.asObservable();

  /** A new search always restarts at page 1 — page 3 of the old result set is meaningless for the new one. */
  readonly searchResults$ = this.searchSubject.pipe(
    debounceTime(300),
    distinctUntilChanged(),
    switchMap(term => this.fetch(term, 1))
  );

  // ---- synchronous reads of current state (no subscribe/unsubscribe dance) ----
  get currentProducts(): Product[] { return this.productsSubject.value; }
  get currentSearch(): string { return this.searchSubject.value; }
  get currentPage(): number { return this.pageSubject.value; }
  get currentPageSize(): number { return this.pageSizeSubject.value; }
  get totalPages(): number { return this.totalPagesSubject.value; }

  setSearch(term: string): void {
    this.searchSubject.next(term);
  }

  clearError(): void {
    this.errorSubject.next(null);
  }

  /** Clears cached state so one user's list never shows up in the next session. */
  reset(): void {
    this.productsSubject.next([]);
    this.movementsSubject.next([]);
    this.searchSubject.next('');
    this.errorSubject.next(null);
    this.pageSubject.next(1);
    this.totalCountSubject.next(0);
    this.totalPagesSubject.next(0);
  }

  goToPage(page: number): Observable<Product[]> {
    const target = Math.min(Math.max(page, 1), Math.max(this.totalPages, 1));
    return this.fetch(this.currentSearch, target);
  }

  setPageSize(size: number): Observable<Product[]> {
    this.pageSizeSubject.next(size);
    return this.fetch(this.currentSearch, 1, size);  // a new page size invalidates the old page index
  }

  /**
   * The API returns PagedResult<T>, not a bare array. The envelope is unwrapped
   * here so every consumer keeps working with Product[] and only this service
   * knows about paging metadata.
   */
  fetch(
    search: string = this.currentSearch,
    pageNumber: number = this.currentPage,
    pageSize: number = this.currentPageSize
  ): Observable<Product[]> {
    this.loadingSubject.next(true);

    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    if (search.trim()) params = params.set('search', search.trim());

    return this.http.get<PagedResult<Product>>(`${API}/products`, { params }).pipe(
      tap(result => {
        this.productsSubject.next(result.items ?? []);
        this.pageSubject.next(result.pageNumber);
        this.pageSizeSubject.next(result.pageSize);
        this.totalCountSubject.next(result.totalCount);
        this.totalPagesSubject.next(result.totalPages);
      }),
      map(result => result.items ?? []),
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
    this.errorSubject.next(err?.error as ApiError);
    return throwError(() => err);
  }
}