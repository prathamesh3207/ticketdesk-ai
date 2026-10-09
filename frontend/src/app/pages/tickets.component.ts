import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService, errorMessage } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { STATUSES, TicketListItem } from '../core/models';
import { LabelPipe, SlugPipe } from '../core/pipes';

@Component({
  selector: 'app-tickets',
  standalone: true,
  imports: [DatePipe, FormsModule, RouterLink, LabelPipe, SlugPipe],
  template: `
    <header class="page-head">
      <div>
        <h1>{{ auth.isStaff() ? 'All tickets' : 'My tickets' }}</h1>
        <p class="muted">{{ items().length }} ticket(s) shown</p>
      </div>
      <a routerLink="/tickets/new" class="btn primary">+ New ticket</a>
    </header>

    <div class="toolbar">
      <div class="chips">
        <button class="chip" [class.on]="status === ''" (click)="setStatus('')">All</button>
        @for (s of statuses; track s) {
          <button class="chip" [class.on]="status === s" (click)="setStatus(s)">{{ s | label }}</button>
        }
      </div>
      <div class="toolbar-right">
        @if (auth.isStaff()) {
          <label class="check"><input type="checkbox" [(ngModel)]="mine" (ngModelChange)="load()"> Assigned to me</label>
        }
        <input class="search" type="search" placeholder="Search tickets…" [(ngModel)]="q" (ngModelChange)="onSearch()">
      </div>
    </div>

    @if (error()) { <div class="alert error">{{ error() }}</div> }

    <div class="card table-card">
      <table class="table">
        <thead>
          <tr><th>#</th><th>Ticket</th><th>Status</th><th>Priority</th><th>Category</th>
              @if (auth.isStaff()) { <th>Customer</th><th>Assignee</th> }
              <th>Created</th></tr>
        </thead>
        <tbody>
          @for (t of items(); track t.id) {
            <tr class="row-link" [routerLink]="['/tickets', t.id]">
              <td class="muted">#{{ t.id }}</td>
              <td class="title-cell">
                <b>{{ t.title }}</b>
                @if (t.sentiment === 'Negative') { <span class="mood" title="Negative sentiment">😟</span> }
                @if (t.sentiment === 'Positive') { <span class="mood" title="Positive sentiment">😊</span> }
              </td>
              <td><span class="badge" [class]="'badge s-' + (t.status | slug)">{{ t.status | label }}</span></td>
              <td><span class="badge" [class]="'badge p-' + (t.priority | slug)">{{ t.priority }}</span></td>
              <td><span class="badge soft">{{ t.category }}</span></td>
              @if (auth.isStaff()) {
                <td>{{ t.createdByName }}</td>
                <td>{{ t.assignedToName ?? '—' }}</td>
              }
              <td class="muted nowrap">{{ t.createdAt | date: 'MMM d, h:mm a' }}</td>
            </tr>
          } @empty {
            <tr><td [attr.colspan]="auth.isStaff() ? 8 : 6" class="empty">
              {{ loading() ? 'Loading…' : 'No tickets match your filters.' }}
            </td></tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class TicketsComponent implements OnInit {
  private api = inject(ApiService);
  auth = inject(AuthService);

  items = signal<TicketListItem[]>([]);
  loading = signal(true);
  error = signal('');
  statuses = STATUSES;
  status = '';
  q = '';
  mine = false;
  private timer: ReturnType<typeof setTimeout> | undefined;

  ngOnInit(): void {
    this.load();
  }

  setStatus(s: string): void {
    this.status = s;
    this.load();
  }

  onSearch(): void {
    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.load(), 300);
  }

  load(): void {
    this.loading.set(true);
    this.api.tickets({ status: this.status, q: this.q, mine: this.mine }).subscribe({
      next: (r) => {
        this.items.set(r);
        this.loading.set(false);
        this.error.set('');
      },
      error: (e) => {
        this.error.set(errorMessage(e));
        this.loading.set(false);
      },
    });
  }
}
