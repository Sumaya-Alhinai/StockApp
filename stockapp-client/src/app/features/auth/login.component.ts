import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-page">
      <h2>Login</h2>

      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>
          Email
          <input type="email" formControlName="email" />
        </label>
        <p class="field-error" *ngIf="email.touched && email.invalid">
          <span *ngIf="email.errors?.['required']">Email is required.</span>
          <span *ngIf="email.errors?.['email']">Email format is invalid.</span>
        </p>

        <label>
          Password
          <input type="password" formControlName="password" />
        </label>
        <p class="field-error" *ngIf="password.touched && password.invalid">
          Password is required.
        </p>

        <p class="server-error" *ngIf="auth.error$ | async as err">
          {{ err.message }}
        </p>

        <button type="submit" [disabled]="form.invalid || (auth.loading$ | async)">
          {{ (auth.loading$ | async) ? 'Signing in...' : 'Login' }}
        </button>
      </form>

      <p>No account? <a routerLink="/register">Register</a></p>
    </div>
  `,
  styles: [`
    .auth-page { max-width: 360px; margin: 3rem auto; font-family: system-ui; }
    label { display: block; margin-bottom: .75rem; }
    input { width: 100%; padding: .5rem; margin-top: .25rem; box-sizing: border-box; }
    button { width: 100%; padding: .6rem; cursor: pointer; }
    button:disabled { opacity: .5; cursor: not-allowed; }
    .field-error { color: #c0392b; font-size: .85rem; margin: -.5rem 0 .75rem; }
    .server-error { color: #c0392b; background: #fdecea; padding: .5rem; border-radius: 4px; }
  `]
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  auth = inject(AuthService);

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  get email() { return this.form.controls.email; }
  get password() { return this.form.controls.password; }

  submit(): void {
    if (this.form.invalid) return;

    const { email, password } = this.form.getRawValue();

    this.auth.login(email, password).subscribe({
      next: () => this.router.navigate(['/products']),
      error: () => {}
    });
  }
}