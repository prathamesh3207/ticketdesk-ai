import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, errorMessage } from '../core/api.service';
import { Dashboard, LabelCount } from '../core/models';
import { SlugPipe } from '../core/pipes';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, SlugPipe],
  template: `
    <header class="page-head">
      <div><h1>Dashboard</h1><p class="muted">Live overview of the support queue</p></div>
      <a routerLink="/tickets/new" class="btn primary">+ New ticket</a>
    </header>

    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (d(); as d) {
      <section class="stats">
        <div class="stat"><span>Total tickets</span><b>{{ d.total }}</b></div>
        <div class="stat blue"><span>Open</span><b>{{ d.open }}</b></div>
        <div class="stat amber"><span>In progress</span><b>{{ d.inProgress }}</b></div>
        <div class="stat green"><span>Resolved / closed</span><b>{{ d.resolved + d.closed }}</b></div>
        <div class="stat red"><span>Urgent &amp; active</span><b>{{ d.urgentOpen }}</b></div>
        <div class="stat violet"><span>Avg. resolution</span><b>{{ d.avgResolutionHours }}<small>h</small></b></div>
      </section>

      @if (d.negativeOpen > 0) {
        <div class="alert warn">😟 {{ d.negativeOpen }} active ticket(s) have negative customer sentiment. Worth a look first.</div>
      }

      <section class="grid-2">
        <div class="card">
          <h3>Tickets created, last 7 days</h3>
          <div class="vbars">
            @for (x of d.last7Days; track x.label) {
              <div class="vbar">
                <span class="vbar-val">{{ x.count }}</span>
                <div class="vbar-track"><div class="vbar-fill" [style.height.%]="pct(x.count, max(d.last7Days))"></div></div>
                <span class="vbar-label">{{ x.label }}</span>
              </div>
            }
          </div>
        </div>

        <div class="card">
          <h3>Status split</h3>
          <div class="donut-wrap">
            <div class="donut" [style.background]="donut(d)"><div class="donut-hole"><b>{{ d.total }}</b><small>tickets</small></div></div>
            <ul class="legend">
              <li><i style="background:#6366f1"></i>Open <b>{{ d.open }}</b></li>
              <li><i style="background:#f59e0b"></i>In progress <b>{{ d.inProgress }}</b></li>
              <li><i style="background:#10b981"></i>Resolved <b>{{ d.resolved }}</b></li>
              <li><i style="background:#94a3b8"></i>Closed <b>{{ d.closed }}</b></li>
            </ul>
          </div>
        </div>
      </section>

      <section class="grid-2">
        <div class="card">
          <h3>By category <span class="tag ai">AI-tagged</span></h3>
          @for (x of d.byCategory; track x.label) {
            <div class="hbar">
              <span class="hbar-label">{{ x.label }}</span>
              <div class="hbar-track"><div class="hbar-fill" [style.width.%]="pct(x.count, max(d.byCategory))"></div></div>
              <b>{{ x.count }}</b>
            </div>
          }
        </div>

        <div class="card">
          <h3>By priority <span class="tag ai">AI-tagged</span></h3>
          @for (x of d.byPriority; track x.label) {
            <div class="hbar">
              <span class="hbar-label">{{ x.label }}</span>
              <div class="hbar-track"><div class="hbar-fill" [class]="'p-' + (x.label | slug)" [style.width.%]="pct(x.count, max(d.byPriority))"></div></div>
              <b>{{ x.count }}</b>
            </div>
          }
        </div>
      </section>
    }
    @if (!d() && !error()) {
      <div class="card muted">Loading dashboard…</div>
    }
  `,
})
export class DashboardComponent implements OnInit {
  private api = inject(ApiService);
  d = signal<Dashboard | null>(null);
  error = signal('');

  ngOnInit(): void {
    this.api.dashboard().subscribe({
      next: (r) => this.d.set(r),
      error: (e) => this.error.set(errorMessage(e)),
    });
  }

  max(items: LabelCount[]): number {
    return Math.max(1, ...items.map((i) => i.count));
  }

  pct(value: number, max: number): number {
    return Math.round((value / max) * 100);
  }

  donut(d: Dashboard): string {
    if (!d.total) return '#e2e8f0';
    const parts = [
      { v: d.open, c: '#6366f1' },
      { v: d.inProgress, c: '#f59e0b' },
      { v: d.resolved, c: '#10b981' },
      { v: d.closed, c: '#94a3b8' },
    ];
    let acc = 0;
    const stops = parts.map((p) => {
      const start = (acc / d.total) * 100;
      acc += p.v;
      const end = (acc / d.total) * 100;
      return `${p.c} ${start}% ${end}%`;
    });
    return `conic-gradient(${stops.join(', ')})`;
  }
}
