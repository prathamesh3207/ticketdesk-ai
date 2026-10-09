import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { ApiService } from './core/api.service';
import { InitialsPipe } from './core/pipes';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, InitialsPipe],
  template: `
    <div class="shell" [class.bare]="!auth.user()">
      @if (auth.user(); as u) {
        <aside class="sidebar">
          <div class="brand">
            <span class="logo">T</span>
            <div><b>TicketDesk</b><small>AI support desk</small></div>
          </div>

          <nav>
            @if (auth.isStaff()) {
              <a routerLink="/dashboard" routerLinkActive="active">📊 <span>Dashboard</span></a>
            }
            <a routerLink="/tickets" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">🎫 <span>Tickets</span></a>
            <a routerLink="/tickets/new" routerLinkActive="active">➕ <span>New ticket</span></a>
            <a routerLink="/kb" routerLinkActive="active">📚 <span>Knowledge base</span></a>
          </nav>

          <div class="side-foot">
            <div class="ai-chip" [class.live]="aiMode() === 'Gemini'">
              ✨ AI: {{ aiMode() === 'Gemini' ? 'Gemini (live)' : aiMode() }}
            </div>
            <div class="me">
              <span class="avatar">{{ u.fullName | initials }}</span>
              <div><b>{{ u.fullName }}</b><small>{{ u.role }}</small></div>
            </div>
            <button class="btn ghost block" (click)="auth.logout()">Sign out</button>
          </div>
        </aside>
      }
      <main class="main"><router-outlet /></main>
    </div>
  `,
})
export class AppComponent {
  auth = inject(AuthService);
  private api = inject(ApiService);
  aiMode = signal('…');

  constructor() {
    this.api.meta().subscribe({
      next: (m) => this.aiMode.set(m.aiMode),
      error: () => this.aiMode.set('API offline'),
    });
  }
}
