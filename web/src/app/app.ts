import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  ElementRef,
  Injector,
  OnInit,
  afterNextRender,
  computed,
  inject,
  signal,
  viewChild
} from '@angular/core';
import { ChatService } from './chat.service';
import { Passage } from './models';

interface ChatEntry {
  key: number;
  question: string;
  answer: string;
  passages: Passage[];
  pending: boolean;
  error: string;
}

@Component({
  selector: 'app-root',
  imports: [DecimalPipe],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private readonly chat = inject(ChatService);
  private readonly injector = inject(Injector);
  private readonly list = viewChild<ElementRef<HTMLElement>>('list');
  private nextKey = 1;

  protected readonly entries = signal<ChatEntry[]>([]);
  protected readonly draft = signal('');
  protected readonly busy = signal(false);
  protected readonly notice = signal('');
  protected readonly canSend = computed(() => this.draft().trim().length > 0 && !this.busy());

  ngOnInit(): void {
    this.chat.history(50).subscribe({
      next: items => {
        const restored = [...items].reverse().map(item => ({
          key: this.nextKey++,
          question: item.question,
          answer: item.answer,
          passages: item.passages,
          pending: false,
          error: ''
        }));
        this.entries.set(restored);
        this.scrollDown();
      },
      error: () => this.notice.set("Historique indisponible : l'API ne répond pas.")
    });
  }

  protected onInput(event: Event): void {
    this.draft.set((event.target as HTMLTextAreaElement).value);
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  protected send(): void {
    if (!this.canSend()) {
      return;
    }
    const question = this.draft().trim();
    const key = this.nextKey++;
    this.entries.update(list => [
      ...list,
      { key, question, answer: '', passages: [], pending: true, error: '' }
    ]);
    this.draft.set('');
    this.notice.set('');
    this.busy.set(true);
    this.scrollDown();
    this.chat.ask(question).subscribe({
      next: response => {
        this.patch(key, { answer: response.answer, passages: response.passages, pending: false });
        this.busy.set(false);
        this.scrollDown();
      },
      error: (error: HttpErrorResponse) => {
        this.patch(key, { pending: false, error: this.describe(error) });
        this.busy.set(false);
        this.scrollDown();
      }
    });
  }

  protected clear(): void {
    this.chat.clearHistory().subscribe({
      next: () => {
        this.entries.set([]);
        this.notice.set('');
      },
      error: () => this.notice.set("Suppression impossible : l'API ne répond pas.")
    });
  }

  private patch(key: number, changes: Partial<ChatEntry>): void {
    this.entries.update(list =>
      list.map(entry => (entry.key === key ? { ...entry, ...changes } : entry))
    );
  }

  private describe(error: HttpErrorResponse): string {
    if (error.status === 0 || error.status === 502 || error.status === 504) {
      return "L'API est injoignable.";
    }
    if (error.status === 400) {
      return 'Question invalide.';
    }
    if (error.status === 503) {
      return 'Le service RAG est indisponible, réessaie dans un instant.';
    }
    return 'Erreur inattendue (code ' + error.status + ').';
  }

  private scrollDown(): void {
    afterNextRender(
      () => {
        const element = this.list()?.nativeElement;
        if (element) {
          element.scrollTop = element.scrollHeight;
        }
      },
      { injector: this.injector }
    );
  }
}