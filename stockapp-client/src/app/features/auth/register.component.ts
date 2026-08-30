import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-page">
      <h2>Create account</h2>

      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>
          Full name
          <input type="text" formControlName="fullName" />
        </label>
        <p class="field-error" *ngIf="fullName.touched && fullName.invalid">
          Full name is required.
        </p>

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
        <div class="field-error" *ngIf="password.touched && password.invalid">
          <div *ngIf="password.errors?.['required']">Password is required.</div>
          <div *ngIf="password.errors?.['minlength']">At least 8 characters.</div>
          <div *ngIf="password.errors?.['uppercase']">Must contain an uppercase letter.</div>
          <div *ngIf="password.errors?.['lowercase']">Must contain a lowercase letter.</div>
          <div *ngIf="password.errors?.['digit']">Must contain a digit.</div>
        </div>

        <div class="server-error" *ngIf="auth.error$ | async as err">
          <div>{{ err.message }}</div>
          <div *ngFor="let entry of fieldErrors(err)">{{ entry }}</div>
        </div>

        <button type="submit" [disabled]="form.invalid || (auth.loading$ | async)">
          {{ (auth.loading$ | async) ? 'Creating...' : 'Register' }}
        </button>
      </form>

      <p>Already have an account? <a routerLink="/login">Login</a></p>
    </div>
  `,
  styles: [`
    .auth-page { max-width: 360px; margin: 3rem auto; font-family: system-ui; }
    label { display: block; margin-bottom: .75rem; }
    input { width: 100%; padding: .5rem; margin-top: .25rem; box-sizing: border-box; }
    button { width: 100%; padding: .6rem; cursor: pointer; }
    button:disabled { opacity: .5; cursor: not-allowed; }
    .field-error { color: #c0392b; font-size: .85rem; margin: -.5rem 0 .75rem; }
    .server-error { color: #c0392b; background: #fdecea; padding: .5rem; border-radius: 4px; margin-bottom: .75rem; }
  `]
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  auth = inject(AuthService);

  form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [
      Validators.required,
      Validators.minLength(8),
      this.pattern(/[A-Z]/, 'uppercase'),
      this.pattern(/[a-z]/, 'lowercase'),
      this.pattern(/[0-9]/, 'digit')
    ]]
  });

  get fullName() { return this.form.controls.fullName; }
  get email() { return this.form.controls.email; }
  get password() { return this.form.controls.password; }

  private pattern(regex: RegExp, key: string) {
    return (control: { value: string }) =>
      !control.value || regex.test(control.value) ? null : { [key]: true };
  }

  fieldErrors(err: { errors: Record<string, string[]> | null }): string[] {
    if (!err.errors) return [];
    return Object.values(err.errors).flat();
  }

  submit(): void {
    if (this.form.invalid) return;

    const { fullName, email, password } = this.form.getRawValue();

    this.auth.register(fullName, email, password).subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => {}
    });
  }
}