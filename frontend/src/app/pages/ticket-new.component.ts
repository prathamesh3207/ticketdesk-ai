import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService, errorMessage } from '../core/api.service';

@Component({
  selector: 'app-ticket-new',
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <header class="page-head">
      <div><h1>Raise a ticket</h1><p class="muted">Describe the problem; AI will categorise and prioritise it instantly.</p></div>
      <a routerLink="/tickets" class="btn ghost">Cancel</a>
    </header>

    <div class="grid-main">
      <form class="card form" (ngSubmit)="submit()">
        @if (error()) { <div class="alert error">{{ error() }}</div> }

        <label>Title
          <input name="title" [(ngModel)]="title" maxlength="200" placeholder="e.g. Payment deducted twice for my subscription" required>
        </label>
        <label>Description
          <textarea name="description" [(ngModel)]="description" rows="9" maxlength="4000"
            placeholder="What happened? What did you expect? Include any error message or steps to reproduce." required></textarea>
        </label>

        <div class="form-foot">
          <small class="muted">{{ description.length }}/4000</small>
          <button class="btn primary" type="submit" [disabled]="busy() || title.length < 5 || description.length < 10">
            {{ busy() ? '✨ AI is triaging…' : 'Submit ticket' }}
          </button>
        </div>
      </form>

      <aside class="card tips">
        <h3>✨ What the AI will do</h3>
        <ul class="plain">
          <li><b>Category:</b> Billing, Technical, Account, Feature Request or General</li>
          <li><b>Priority:</b> Low to Urgent, based on impact and wording</li>
          <li><b>Sentiment:</b> so frustrated customers are noticed first</li>
          <li><b>Summary:</b> a one-line digest for the agent</li>
        </ul>
        <p class="muted small">Tip: the more detail you include, the faster we can help.</p>
      </aside>
    </div>
  `,
})
export class TicketNewComponent {
  private api = inject(ApiService);
  private router = inject(Router);

  title = '';
  description = '';
  busy = signal(false);
  error = signal('');

  submit(): void {
    this.busy.set(true);
    this.error.set('');
    this.api.createTicket(this.title.trim(), this.description.trim()).subscribe({
      next: (t) => this.router.navigate(['/tickets', t.id]),
      error: (e) => {
        this.error.set(errorMessage(e));
        this.busy.set(false);
      },
    });
  }
}
