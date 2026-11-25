import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ChatResponse, HistoryItem } from './models';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);

  ask(question: string): Observable<ChatResponse> {
    return this.http.post<ChatResponse>('/api/chat', { question });
  }

  history(limit = 50): Observable<HistoryItem[]> {
    return this.http.get<HistoryItem[]>('/api/history', { params: { limit } });
  }

  clearHistory(): Observable<void> {
    return this.http.delete<void>('/api/history');
  }
}