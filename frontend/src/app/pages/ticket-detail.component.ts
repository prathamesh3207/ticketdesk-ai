import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService, errorMessage } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { CATEGORIES, PRIORITIES, STATUSES, TicketDetail, User } from '../core/models';
import { InitialsPipe, LabelPipe, SlugPipe } from '../core/pipes';

@Component({
  selector: 'app-ticket-detail',
  standalone: true,
  imports: [DatePipe, FormsModule, RouterLink, LabelPipe, SlugPipe, InitialsPipe],
  template: `
    <a routerLink="/tickets" class="back">← All tickets</a>

    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (t(); as d) {
      <div class="detail-grid">
        <div class="col-main">
          <!-- header -->
          <div class="card">
            <div class="badges">
              <span class="muted">#{{ d.id }}</span>
              <span class="badge" [class]="'badge s-' + (d.status | slug)">{{ d.status | label }}</span>
              <span class="badge" [class]="'badge p-' + (d.priority | slug)">{{ d.priority }}</span>
              <span class="badge soft">{{ d.category }}</span>
              <span class="badge" [class]="'badge m-' + (d.sentiment | slug)">
                {{ d.sentiment === 'Negative' ? '😟' : d.sentiment === 'Positive' ? '😊' : '😐' }} {{ d.sentiment }}
              </span>
            </div>
            <h1 class="ticket-title">{{ d.title }}</h1>
            <p class="muted">Raised by <b>{{ d.createdByName }}</b> · {{ d.createdAt | date: 'MMM d, y, h:mm a' }}</p>
            <p class="desc">{{ d.description }}</p>
          </div>

          <!-- AI insight -->
          @if (d.aiSummary) {
            <div class="card ai-card">
              <div class="ai-head">
                <h3>✨ AI insight</h3>
                <span class="tag ai">{{ d.aiSource === 'Seed' ? 'Demo data' : d.aiSource }}</span>
                @if (auth.isStaff()) {
                  <button class="btn ghost sm" (click)="retriage()" [disabled]="aiBusy()">Re-run triage</button>
                }
              </div>
              <p>{{ d.aiSummary }}</p>
            </div>
          }

          <!-- conversation -->
          <div class="card">
            <h3>Conversation</h3>
            <div class="thread">
              @for (c of d.comments; track c.id) {
                <div class="msg" [class.mine]="c.authorId === auth.user()?.id" [class.internal]="c.isInternal">
                  <span class="avatar sm">{{ c.authorName | initials }}</span>
                  <div class="bubble">
                    <div class="msg-head">
                      <b>{{ c.authorName }}</b>
                      <span class="role">{{ c.authorRole }}</span>
                      @if (c.isInternal) { <span class="tag warn">Internal note</span> }
                      <span class="muted">{{ c.createdAt | date: 'MMM d, h:mm a' }}</span>
                    </div>
                    <p>{{ c.body }}</p>
                  </div>
                </div>
              } @empty {
                <p class="muted">No replies yet.</p>
              }
            </div>

            <div class="reply">
              <textarea rows="5" [(ngModel)]="reply" name="reply"
                [placeholder]="auth.isStaff() ? 'Write a reply to the customer…' : 'Write a reply…'"></textarea>
              <div class="reply-foot">
                <div class="left">
                  @if (auth.isStaff()) {
                    <button class="btn ai" (click)="suggest()" [disabled]="aiBusy()">
                      {{ aiBusy() ? '✨ Thinking…' : '✨ Suggest reply with AI' }}
                    </button>
                    <label class="check"><input type="checkbox" [(ngModel)]="internal" name="internal"> Internal note</label>
                    @if (suggestSource()) { <small class="muted">Drafted by {{ suggestSource() }}. Review before sending.</small> }
                  }
                </div>
                <button class="btn primary" (click)="send()" [disabled]="!reply.trim() || busy()">
                  {{ internal ? 'Add note' : 'Send reply' }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- side panel -->
        <aside class="col-side">
          <div class="card">
            <h3>Details</h3>
            @if (auth.isStaff()) {
              <label>Status
                <select [ngModel]="d.status" (ngModelChange)="setStatus($event)" name="status">
                  @for (s of statuses; track s) { <option [value]="s">{{ s | label }}</option> }
                </select>
              </label>
              <label>Priority
                <select [ngModel]="d.priority" (ngModelChange)="setMeta({ priority: $event })" name="priority">
                  @for (p of priorities; track p) { <option [value]="p">{{ p }}</option> }
                </select>
              </label>
              <label>Category
                <select [ngModel]="d.category" (ngModelChange)="setMeta({ category: $event })" name="category">
                  @for (c of categories; track c) { <option [value]="c">{{ c }}</option> }
                </select>
              </label>
              <label>Assignee
                <select [ngModel]="d.assignedToId" (ngModelChange)="assign($event)" name="assignee">
                  <option [ngValue]="null">Unassigned</option>
                  @for (u of staff(); track u.id) { <option [ngValue]="u.id">{{ u.fullName }} ({{ u.role }})</option> }
                </select>
              </label>
            } @else {
              <p class="kv"><span>Assigned to</span><b>{{ d.assignedToName ?? 'Not assigned yet' }}</b></p>
              <p class="kv"><span>Last update</span><b>{{ d.updatedAt | date: 'MMM d, h:mm a' }}</b></p>
              @if (d.status === 'Closed' || d.status === 'Resolved') {
                <button class="btn ghost block" (click)="setStatus('Open')">Reopen ticket</button>
              } @else {
                <button class="btn ghost block" (click)="setStatus('Closed')">Close ticket</button>
              }
            }
            @if (d.resolvedAt) {
              <p class="kv"><span>Resolved</span><b>{{ d.resolvedAt | date: 'MMM d, h:mm a' }}</b></p>
            }
          </div>

          @if (auth.isStaff()) {
            <div class="card">
              <h3>📚 Suggested articles</h3>
              @for (s of d.suggestions; track s.id) {
                <div class="kb-sug">
                  <b>{{ s.title }}</b>
                  <p class="muted small">{{ s.snippet }}</p>
                </div>
              } @empty {
                <p class="muted small">No matching articles found.</p>
              }
            </div>
          }
        </aside>
      </div>
    }
    @if (!t() && !error()) {
      <div class="card muted">Loading ticket…</div>
    }
  `,
})
export class TicketDetailComponent implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  auth = inject(AuthService);

  t = signal<TicketDetail | null>(null);
  staff = signal<User[]>([]);
  error = signal('');
  busy = signal(false);
  aiBusy = signal(false);
  suggestSource = signal('');

  reply = '';
  internal = false;
  statuses = STATUSES;
  priorities = PRIORITIES;
  categories = CATEGORIES;

  private id = 0;

  ngOnInit(): void {
    if (this.auth.isStaff()) this.api.staff().subscribe((u) => this.staff.set(u));
    this.route.paramMap.subscribe((p) => {
      this.id = Number(p.get('id'));
      this.t.set(null);
      this.error.set('');
      this.api.ticket(this.id).subscribe({
        next: (d) => this.t.set(d),
        error: (e) => this.error.set(errorMessage(e)),
      });
    });
  }

  setStatus(status: string): void {
    this.run(this.api.setStatus(this.id, status));
  }

  setMeta(meta: { priority?: string; category?: string }): void {
    this.run(this.api.setMeta(this.id, meta));
  }

  assign(agentId: number | null): void {
    this.run(this.api.assign(this.id, agentId));
  }

  retriage(): void {
    this.aiBusy.set(true);
    this.api.retriage(this.id).subscribe({
      next: (d) => {
        this.t.set(d);
        this.aiBusy.set(false);
      },
      error: (e) => {
        this.error.set(errorMessage(e));
        this.aiBusy.set(false);
      },
    });
  }

  suggest(): void {
    this.aiBusy.set(true);
    this.api.suggestReply(this.id).subscribe({
      next: (r) => {
        this.reply = r.reply;
        this.internal = false;
        this.suggestSource.set(r.source);
        this.aiBusy.set(false);
      },
      error: (e) => {
        this.error.set(errorMessage(e));
        this.aiBusy.set(false);
      },
    });
  }

  send(): void {
    if (!this.reply.trim()) return;
    this.busy.set(true);
    this.api.addComment(this.id, this.reply.trim(), this.internal).subscribe({
      next: (d) => {
        this.t.set(d);
        this.reply = '';
        this.internal = false;
        this.suggestSource.set('');
        this.busy.set(false);
      },
      error: (e) => {
        this.error.set(errorMessage(e));
        this.busy.set(false);
      },
    });
  }

  private run(call: ReturnType<ApiService['setStatus']>): void {
    this.error.set('');
    call.subscribe({
      next: (d) => this.t.set(d),
      error: (e) => this.error.set(errorMessage(e)),
    });
  }
}
