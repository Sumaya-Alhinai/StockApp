import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, catchError, throwError, finalize } from 'rxjs';
import { Router } from '@angular/router';
import { AuthUser, RegisterResult, ApiError } from './models';

const API = 'http://localhost:5184/api';
const STORAGE_KEY = 'stockapp_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private userSubject = new BehaviorSubject<AuthUser | null>(this.readStored());
  private loadingSubject = new BehaviorSubject<boolean>(false);
  private errorSubject = new BehaviorSubject<ApiError | null>(null);

  readonly user$ = this.userSubject.asObservable();
  readonly loading$ = this.loadingSubject.asObservable();
  readonly error$ = this.errorSubject.asObservable();

  private readStored(): AuthUser | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : null;
  }

  get token(): string | null {
    return this.userSubject.value?.token ?? null;
  }

  get isLoggedIn(): boolean {
    return this.token !== null;
  }

  clearError(): void {
    this.errorSubject.next(null);
  }

  register(fullName: string, email: string, password: string): Observable<RegisterResult> {
    this.loadingSubject.next(true);
    this.errorSubject.next(null);

    return this.http.post<RegisterResult>(`${API}/auth/register`, { fullName, email, password })
      .pipe(
        catchError(err => {
          this.errorSubject.next(err.error as ApiError);
          return throwError(() => err);
        }),
        finalize(() => this.loadingSubject.next(false))
      );
  }

  login(email: string, password: string): Observable<AuthUser> {
    this.loadingSubject.next(true);
    this.errorSubject.next(null);

    return this.http.post<AuthUser>(`${API}/auth/login`, { email, password })
      .pipe(
        tap(user => {
          localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
          this.userSubject.next(user);
        }),
        catchError(err => {
          this.errorSubject.next(err.error as ApiError);
          return throwError(() => err);
        }),
        finalize(() => this.loadingSubject.next(false))
      );
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.userSubject.next(null);
    this.router.navigate(['/login']);
  }
}