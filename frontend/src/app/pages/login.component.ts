import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { errorMessage } from '../core/api.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <div class="auth-wrap">
      <section class="auth-hero">
        <div class="brand light"><span class="logo">T</span><div><b>TicketDesk</b><small>AI support desk</small></div></div>
        <h1>Support that<br>triages itself.</h1>
        <p>Every ticket is categorised, prioritised and summarised by AI the moment it arrives, and agents get a drafted reply plus the right help-centre articles.</p>
        <ul class="hero-points">
          <li>✨ AI triage: category, priority, sentiment</li>
          <li>💬 One-click suggested replies</li>
          <li>📚 Knowledge-base matching</li>
          <li>📊 Live dashboard for the team</li>
        </ul>
      </section>

      <section class="auth-card">
        <h2>Welcome back</h2>
        <p class="muted">Sign in to continue</p>

        @if (error()) { <div class="alert error">{{ error() }}</div> }

        <form (ngSubmit)="submit()">
          <label>Email
            <input type="email" name="email" [(ngModel)]="email" placeholder="you@company.com" required autocomplete="username">
          </label>
          <label>Password
            <input type="password" name="password" [(ngModel)]="password" placeholder="••••••••" required autocomplete="current-password">
          </label>
          <button class="btn primary block" type="submit" [disabled]="loading()">
            {{ loading() ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>

        <div class="demo">
          <span class="muted">Try a demo account</span>
          <div class="demo-row">
            @for (d of demos; track d.label) {
              <button type="button" class="chip" (click)="fill(d.email, d.password)">{{ d.label }}</button>
            }
          </div>
        </div>

        <p class="muted center">New customer? <a routerLink="/register">Create an account</a></p>
      </section>
    </div>
  `,
})
export class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  email = '';
  password = '';
  error = signal('');
  loading = signal(false);

  demos = [
    { label: 'Admin', email: 'admin@ticketdesk.com', password: 'Admin@123' },
    { label: 'Agent', email: 'agent@ticketdesk.com', password: 'Agent@123' },
    { label: 'Customer', email: 'customer@ticketdesk.com', password: 'Customer@123' },
  ];

  fill(email: string, password: string): void {
    this.email = email;
    this.password = password;
    this.submit();
  }

  submit(): void {
    if (!this.email || !this.password) return;
    this.loading.set(true);
    this.error.set('');
    this.auth.login(this.email, this.password).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (e) => {
        this.error.set(errorMessage(e));
        this.loading.set(false);
      },
    });
  }
}
