import { Injectable, inject, signal, computed } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { RagService, DocumentInfo } from './rag.service';

@Injectable({
  providedIn: 'root'
})
export class DocumentsStore {
  private readonly rag = inject(RagService);

  readonly documents = signal<DocumentInfo[]>([]);
  readonly selectedIds = signal<string[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');

  readonly selectedCount = computed(() => this.selectedIds().length);

  async reload(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      this.documents.set(await firstValueFrom(this.rag.getDocuments()));
    } catch (err) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  toggle(id: string): void {
    const current = this.selectedIds();
    this.selectedIds.set(
      current.includes(id) ? current.filter(x => x !== id) : [...current, id]
    );
  }

  async remove(id: string): Promise<void> {
    await firstValueFrom(this.rag.deleteDocument(id));
    this.selectedIds.set(this.selectedIds().filter(x => x !== id));
    await this.reload();
  }

  clear(): void {
    this.selectedIds.set([]);
  }
}