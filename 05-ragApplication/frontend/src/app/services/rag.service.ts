import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface UploadResponse {
  id: string;
  status?: string;
  message: string;
}

export interface AskResponse {
  answer: string;
}

export interface DocumentInfo {
  id: string;
  title: string;
  createdAt: string;
  chunkCount: number;
  status: 'queued' | 'processing' | 'ready' | 'failed';
  error?: string | null;
  totalChunks: number;
  chunksCompleted: number;
}

@Injectable({
  providedIn: 'root'
})
export class RagService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api';

  uploadDocument(title: string, content: string): Observable<UploadResponse> {
    return this.http.post<UploadResponse>(`${this.apiUrl}/documents/upload`, { title, content });
  }

  getDocument(id: string): Observable<DocumentInfo> {
    return this.http.get<DocumentInfo>(`${this.apiUrl}/documents/${id}`);
  }

  getDocuments(): Observable<DocumentInfo[]> {
    return this.http.get<DocumentInfo[]>(`${this.apiUrl}/documents`);
  }

  deleteDocument(id: string): Observable<UploadResponse> {
    return this.http.delete<UploadResponse>(`${this.apiUrl}/documents/${id}`);
  }

  askQuestion(question: string, documentIds?: string[]): Observable<AskResponse> {
    return this.http.post<AskResponse>(`${this.apiUrl}/chat/ask`, {
      question,
      documentIds: documentIds && documentIds.length > 0 ? documentIds : undefined
    });
  }
}