import { Component, inject, signal, OnInit } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { RagService } from '../../services/rag.service';
import { DocumentsStore } from '../../services/documents.store';

const MAX_FILE_BYTES = 25 * 1024 * 1024;

const delay = (ms: number) => new Promise<void>((resolve) => setTimeout(resolve, ms));

interface ExtractWorkerResponse {
  ok: boolean;
  text?: string;
  error?: string;
}

@Component({
  selector: 'app-upload-panel',
  standalone: true,
  imports: [],
  templateUrl: './upload.component.html',
  styleUrl: './upload.component.css'
})
export class UploadComponent implements OnInit {
  private readonly rag = inject(RagService);
  readonly documentsStore = inject(DocumentsStore);

  readonly selectedFile = signal<File | null>(null);
  readonly fileName = signal('');
  readonly fileSize = signal(0);
  readonly isDragging = signal(false);
  readonly isUploading = signal(false);
  readonly deletingId = signal<string | null>(null);
  readonly status = signal('');
  readonly statusKind = signal<'idle' | 'ok' | 'err'>('idle');

  private pollingId: string | null = null;

  ngOnInit() {
    void this.documentsStore.reload();
  }

  async deleteDocument(id: string, title: string) {
    if (!window.confirm(`Delete “${title}”? This removes the document and all of its chunks.`)) {
      return;
    }

    this.deletingId.set(id);
    try {
      // If the doc is still being ingested, stop polling for it before removing.
      if (this.pollingId === id) {
        this.pollingId = null;
      }
      await this.documentsStore.remove(id);
      if (this.isUploading()) {
        this.isUploading.set(false);
        this.status.set('Upload cancelled (document removed).');
        this.statusKind.set('idle');
      } else {
        this.status.set('Document deleted.');
        this.statusKind.set('ok');
      }
    } catch (err) {
      this.status.set(`Delete failed: ${(err as Error).message}`);
      this.statusKind.set('err');
    } finally {
      this.deletingId.set(null);
    }
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragging.set(true);
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    this.isDragging.set(false);
    const file = event.dataTransfer?.files?.[0] ?? null;
    if (file) {
      this.prepareFile(file);
    }
  }

  onFileChange(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (file) {
      this.prepareFile(file);
    }
    input.value = '';
  }

  private prepareFile(file: File) {
    this.selectedFile.set(file);
    this.fileName.set(file.name);
    this.fileSize.set(file.size);
    this.status.set('');
    this.statusKind.set('idle');

    if (file.size > MAX_FILE_BYTES) {
      this.isUploading.set(false);
      this.status.set(`${file.name} is too large (${this.formatSize(file.size)}). Max supported size is ${this.formatSize(MAX_FILE_BYTES)}.`);
      this.statusKind.set('err');
      return;
    }

    void this.ingest();
  }

  async ingest() {
    const file = this.selectedFile();
    if (!file || this.isUploading()) {
      return;
    }

    this.isUploading.set(true);
    this.status.set(`Reading “${file.name}”…`);
    this.statusKind.set('idle');

    let content: string;
    try {
      // Extraction runs in a Web Worker so the main thread stays responsive even
      // for large PDFs/spreadsheets.
      this.status.set(`Extracting text from “${file.name}”…`);
      content = await this.extractInWorker(file);
    } catch (err) {
      this.finishWithErr(`Could not read file: ${this.msg(err)}`);
      return;
    }

    try {
      // The backend returns immediately (202): ingestion runs in the background.
      this.status.set('Submitting document…');
      const res = await firstValueFrom(this.rag.uploadDocument(file.name, content));
      this.pollingId = res.id;
      this.status.set('Queued — embedding chunks…');
      void this.pollStatus(res.id);
    } catch (err) {
      this.finishWithErr(`Upload failed: ${this.errorMessage(err)}`);
    }
  }

  private async pollStatus(id: string) {
    while (this.pollingId === id && this.isUploading()) {
      await delay(1000);

      let doc;
      try {
        doc = await firstValueFrom(this.rag.getDocument(id));
      } catch (err) {
        if ((err as { status?: number })?.status === 404) {
          this.finishWithErr('Upload cancelled (document no longer exists).');
          await this.documentsStore.reload();
          return;
        }
        continue; // transient network/backend hiccup — keep polling
      }

      if (doc.status === 'ready') {
        this.finishWithOk(`Document ready — ${doc.chunkCount} chunk${doc.chunkCount === 1 ? '' : 's'} indexed.`);
        await this.documentsStore.reload();
        return;
      }

      if (doc.status === 'failed') {
        this.finishWithErr(`Ingestion failed: ${doc.error || 'unknown error'}`);
        await this.documentsStore.reload();
        return;
      }

      this.status.set(
        doc.totalChunks > 0 && doc.chunksCompleted > 0
          ? `Processing — embedded ${doc.chunksCompleted} / ${doc.totalChunks} chunks…`
          : 'Processing — embedding chunks…'
      );
      this.statusKind.set('idle');
    }
  }

  private extractInWorker(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const worker = new Worker(new URL('../../workers/extract.worker.ts', import.meta.url), { type: 'module' });

      worker.onmessage = (event: MessageEvent<ExtractWorkerResponse>) => {
        worker.terminate();
        if (event.data.ok && event.data.text !== undefined) {
          resolve(event.data.text);
        } else {
          reject(new Error(event.data.error ?? 'Extraction failed without a message.'));
        }
      };

      worker.onerror = (event) => {
        worker.terminate();
        reject(new Error(`Extraction worker error: ${event.message}`));
      };

      worker.postMessage({ name: file.name, type: file.type, file });
    });
  }

  private finishWithOk(message: string) {
    this.isUploading.set(false);
    this.pollingId = null;
    this.status.set(message);
    this.statusKind.set('ok');
  }

  private finishWithErr(message: string) {
    this.isUploading.set(false);
    this.pollingId = null;
    this.status.set(message);
    this.statusKind.set('err');
  }

  private errorMessage(err: unknown): string {
    const anyErr = err as { error?: { message?: string }; message?: string };
    return anyErr?.error?.message ?? anyErr?.message ?? this.msg(err);
  }

  private msg(err: unknown): string {
    return err instanceof Error ? err.message : String(err);
  }

  formatSize(bytes: number): string {
    if (bytes === 0) {
      return '0 B';
    }
    const units = ['B', 'KB', 'MB', 'GB'];
    const i = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
    return `${(bytes / Math.pow(1024, i)).toFixed(1)} ${units[i]}`;
  }
}