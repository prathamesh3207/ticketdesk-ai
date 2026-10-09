import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService, errorMessage } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { KbArticle } from '../core/models';

@Component({
  selector: 'app-kb',
  standalone: true,
  imports: [FormsModule],
  template: `
    <header class="page-head">
      <div><h1>Knowledge base</h1><p class="muted">Help-centre articles. The AI matches these to incoming tickets.</p></div>
      @if (auth.isStaff()) { <button class="btn primary" (click)="startNew()">+ New article</button> }
    </header>

    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (editing(); as e) {
      <div class="card form">
        <h3>{{ e.id ? 'Edit article' : 'New article' }}</h3>
        <label>Title <input name="kbtitle" [(ngModel)]="e.title" maxlength="200"></label>
        <label>Tags <input name="kbtags" [(ngModel)]="e.tags" placeholder="space separated keywords"></label>
        <label>Content <textarea name="kbcontent" rows="6" [(ngModel)]="e.content"></textarea></label>
        <div class="form-foot">
          <button class="btn ghost" (click)="editing.set(null)">Cancel</button>
          <button class="btn primary" (click)="save()" [disabled]="e.title.length < 3 || e.content.length < 10">Save article</button>
        </div>
      </div>
    }

    <input class="search wide" type="search" placeholder="Search articles…" [(ngModel)]="q" (ngModelChange)="onSearch()">

    <div class="kb-grid">
      @for (a of items(); track a.id) {
        <article class="card kb-card">
          <h3>{{ a.title }}</h3>
          <p>{{ a.content }}</p>
          <div class="kb-foot">
            <span class="tags">
              @for (tag of tagList(a); track tag) { <span class="badge soft">{{ tag }}</span> }
            </span>
            @if (auth.isStaff()) {
              <span class="actions">
                <button class="link" (click)="edit(a)">Edit</button>
                @if (auth.user()?.role === 'Admin') { <button class="link danger" (click)="remove(a)">Delete</button> }
              </span>
            }
          </div>
        </article>
      } @empty {
        <div class="card muted">No articles found.</div>
      }
    </div>
  `,
})
export class KbComponent implements OnInit {
  private api = inject(ApiService);
  auth = inject(AuthService);

  items = signal<KbArticle[]>([]);
  editing = signal<{ id?: number; title: string; content: string; tags: string } | null>(null);
  error = signal('');
  q = '';
  private timer: ReturnType<typeof setTimeout> | undefined;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.kb(this.q).subscribe({
      next: (r) => this.items.set(r),
      error: (e) => this.error.set(errorMessage(e)),
    });
  }

  onSearch(): void {
    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.load(), 300);
  }

  tagList(a: KbArticle): string[] {
    return a.tags.split(' ').filter(Boolean).slice(0, 5);
  }

  startNew(): void {
    this.editing.set({ title: '', content: '', tags: '' });
  }

  edit(a: KbArticle): void {
    this.editing.set({ id: a.id, title: a.title, content: a.content, tags: a.tags });
  }

  save(): void {
    const e = this.editing();
    if (!e) return;
    this.api.saveKb(e).subscribe({
      next: () => {
        this.editing.set(null);
        this.load();
      },
      error: (err) => this.error.set(errorMessage(err)),
    });
  }

  remove(a: KbArticle): void {
    if (!confirm(`Delete "${a.title}"?`)) return;
    this.api.deleteKb(a.id).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(errorMessage(err)),
    });
  }
}
