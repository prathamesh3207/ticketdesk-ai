import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { errorMessage } from '../core/api.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <div class="auth-wrap">
      <section class="auth-hero">
        <div class="brand light"><span class="logo">T</span><div><b>TicketDesk</b><small>AI support desk</small></div></div>
        <h1>Get help,<br>faster.</h1>
        <p>Create a customer account, raise a ticket and track it from open to resolved. Our AI makes sure it reaches the right person with the right priority.</p>
      </section>

      <section class="auth-card">
        <h2>Create your account</h2>
        <p class="muted">Customer accounts only. Agents are added by an admin.</p>

        @if (error()) { <div class="alert error">{{ error() }}</div> }

        <form (ngSubmit)="submit()">
          <label>Full name
            <input name="fullName" [(ngModel)]="fullName" placeholder="Your name" required>
          </label>
          <label>Email
            <input type="email" name="email" [(ngModel)]="email" placeholder="you@company.com" required>
          </label>
          <label>Password
            <input type="password" name="password" [(ngModel)]="password" placeholder="At least 6 characters" required>
          </label>
          <button class="btn primary block" type="submit" [disabled]="loading()">
            {{ loading() ? 'Creating…' : 'Create account' }}
          </button>
        </form>

        <p class="muted center">Already registered? <a routerLink="/login">Sign in</a></p>
      </section>
    </div>
  `,
})
export class RegisterComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  fullName = '';
  email = '';
  password = '';
  error = signal('');
  loading = signal(false);

  submit(): void {
    this.loading.set(true);
    this.error.set('');
    this.auth.register(this.fullName, this.email, this.password).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (e) => {
        this.error.set(errorMessage(e));
        this.loading.set(false);
      },
    });
  }
}
