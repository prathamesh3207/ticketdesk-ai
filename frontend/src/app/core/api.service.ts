import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../config';
import { Dashboard, KbArticle, TicketDetail, TicketListItem, User } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);

  meta(): Observable<{ aiMode: string; version: string }> {
    return this.http.get<{ aiMode: string; version: string }>(`${API_URL}/meta`);
  }

  dashboard(): Observable<Dashboard> {
    return this.http.get<Dashboard>(`${API_URL}/dashboard`);
  }

  staff(): Observable<User[]> {
    return this.http.get<User[]>(`${API_URL}/users/staff`);
  }

  tickets(opts: { status?: string; q?: string; mine?: boolean }): Observable<TicketListItem[]> {
    let params = new HttpParams();
    if (opts.status) params = params.set('status', opts.status);
    if (opts.q) params = params.set('q', opts.q);
    if (opts.mine) params = params.set('mine', 'true');
    return this.http.get<TicketListItem[]>(`${API_URL}/tickets`, { params });
  }

  ticket(id: number): Observable<TicketDetail> {
    return this.http.get<TicketDetail>(`${API_URL}/tickets/${id}`);
  }

  createTicket(title: string, description: string): Observable<TicketDetail> {
    return this.http.post<TicketDetail>(`${API_URL}/tickets`, { title, description });
  }

  setStatus(id: number, status: string): Observable<TicketDetail> {
    return this.http.put<TicketDetail>(`${API_URL}/tickets/${id}/status`, { status });
  }

  assign(id: number, agentId: number | null): Observable<TicketDetail> {
    return this.http.put<TicketDetail>(`${API_URL}/tickets/${id}/assign`, { agentId });
  }

  setMeta(id: number, meta: { priority?: string; category?: string }): Observable<TicketDetail> {
    return this.http.put<TicketDetail>(`${API_URL}/tickets/${id}/meta`, meta);
  }

  addComment(id: number, body: string, isInternal: boolean): Observable<TicketDetail> {
    return this.http.post<TicketDetail>(`${API_URL}/tickets/${id}/comments`, { body, isInternal });
  }

  suggestReply(id: number): Observable<{ reply: string; source: string }> {
    return this.http.post<{ reply: string; source: string }>(`${API_URL}/tickets/${id}/suggest-reply`, {});
  }

  retriage(id: number): Observable<TicketDetail> {
    return this.http.post<TicketDetail>(`${API_URL}/tickets/${id}/retriage`, {});
  }

  kb(q?: string): Observable<KbArticle[]> {
    let params = new HttpParams();
    if (q) params = params.set('q', q);
    return this.http.get<KbArticle[]>(`${API_URL}/kb`, { params });
  }

  saveKb(article: { id?: number; title: string; content: string; tags: string }): Observable<KbArticle> {
    const body = { title: article.title, content: article.content, tags: article.tags };
    return article.id
      ? this.http.put<KbArticle>(`${API_URL}/kb/${article.id}`, body)
      : this.http.post<KbArticle>(`${API_URL}/kb`, body);
  }

  deleteKb(id: number): Observable<void> {
    return this.http.delete<void>(`${API_URL}/kb/${id}`);
  }
}

/** Pulls a friendly message out of an API error. */
export function errorMessage(err: any): string {
  if (err?.status === 0) return 'Cannot reach the API. Is the backend running on http://localhost:5080 ?';
  const e = err?.error;
  if (e?.message) return e.message;
  if (e?.errors) {
    const first = Object.values(e.errors as Record<string, string[]>)[0];
    if (first?.length) return first[0];
  }
  if (err?.status === 403) return 'You do not have permission to do that.';
  return 'Something went wrong. Please try again.';
}
