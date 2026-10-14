import { Component, computed, inject, signal, ElementRef, ViewChild, AfterViewChecked, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RagService } from '../../services/rag.service';
import { DocumentsStore } from '../../services/documents.store';

interface ChatMessage {
  sender: 'user' | 'assistant';
  text: string;
}

@Component({
  selector: 'app-chat-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.css'
})
export class ChatComponent implements AfterViewChecked, OnInit {
  private readonly rag = inject(RagService);
  readonly documentsStore = inject(DocumentsStore);

  @ViewChild('scroll')
  private readonly scrollRef!: ElementRef<HTMLDivElement>;

  readonly currentQuestion = signal('');
  readonly isAsking = signal(false);
  readonly messages = signal<ChatMessage[]>([]);

  readonly canSend = computed(() => this.currentQuestion().trim().length > 0 && !this.isAsking());

  ngOnInit() {
    void this.documentsStore.reload();
  }

  askQuestion() {
    const q = this.currentQuestion().trim();
    if (!q || this.isAsking()) {
      return;
    }

    const scope = this.documentsStore.selectedIds();
    const scopeNote = scope.length > 0
      ? ` (scoped to ${scope.length} selected document${scope.length === 1 ? '' : 's'})`
      : '';

    this.messages.update(msgs => [...msgs, { sender: 'user', text: q + scopeNote }]);
    this.currentQuestion.set('');
    this.isAsking.set(true);

    this.rag.askQuestion(q, scope).subscribe({
      next: (res) => {
        this.isAsking.set(false);
        this.messages.update(msgs => [...msgs, { sender: 'assistant', text: res.answer }]);
      },
      error: (err) => {
        this.isAsking.set(false);
        this.messages.update(msgs => [...msgs, { sender: 'assistant', text: `Error: ${err.error?.message ?? err.message}` }]);
      }
    });
  }

  onEnter(event: Event) {
    const keyboardEvent = event as KeyboardEvent;
    if (keyboardEvent.key !== 'Enter' || keyboardEvent.shiftKey) {
      return;
    }
    keyboardEvent.preventDefault();
    this.askQuestion();
  }

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  private scrollToBottom() {
    const el = this.scrollRef?.nativeElement;
    if (el) {
      el.scrollTop = el.scrollHeight;
    }
  }
}