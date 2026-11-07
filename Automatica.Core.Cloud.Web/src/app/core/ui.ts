import { Component, Directive, ElementRef, Injectable, effect, inject, input, output, signal } from '@angular/core';

// --- toasts ---

export interface Toast {
  id: number;
  text: string;
  kind: 'ok' | 'bad';
}

@Injectable({ providedIn: 'root' })
export class Toasts {
  private next = 1;
  readonly items = signal<Toast[]>([]);

  ok(text: string): void {
    this.push(text, 'ok');
  }

  bad(text: string): void {
    this.push(text, 'bad');
  }

  dismiss(id: number): void {
    this.items.update((list) => list.filter((t) => t.id !== id));
  }

  private push(text: string, kind: Toast['kind']): void {
    const id = this.next++;
    this.items.update((list) => [...list, { id, text, kind }]);
    setTimeout(() => this.dismiss(id), kind === 'bad' ? 7000 : 3500);
  }
}

// --- confirm dialog (rendered once by the shell) ---

export interface ConfirmRequest {
  title: string;
  text: string;
  confirm: string;
  danger: boolean;
  /** when set, the user has to type this value to confirm */
  typeToConfirm?: string;
  resolve: (ok: boolean) => void;
}

@Injectable({ providedIn: 'root' })
export class Confirm {
  readonly current = signal<ConfirmRequest | null>(null);

  ask(options: { title: string; text: string; confirm?: string; danger?: boolean; typeToConfirm?: string }): Promise<boolean> {
    return new Promise((resolve) => {
      this.current.set({ confirm: 'Confirm', danger: false, ...options, resolve });
    });
  }

  answer(ok: boolean): void {
    const request = this.current();
    this.current.set(null);
    request?.resolve(ok);
  }
}

// --- theme ---

export type ThemeChoice = 'system' | 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class Theme {
  readonly choice = signal<ThemeChoice>(read());

  constructor() {
    effect(() => {
      const value = this.choice();
      const root = document.documentElement;
      if (value === 'system') {
        root.removeAttribute('data-theme');
      } else {
        root.setAttribute('data-theme', value);
      }
      try {
        localStorage.setItem('acc-theme', value);
      } catch {
        // storage blocked: the choice just isn't remembered
      }
    });
  }

  cycle(): void {
    const order: ThemeChoice[] = ['system', 'light', 'dark'];
    this.choice.set(order[(order.indexOf(this.choice()) + 1) % order.length]);
  }
}

function read(): ThemeChoice {
  try {
    const value = localStorage.getItem('acc-theme');
    return value === 'light' || value === 'dark' ? value : 'system';
  } catch {
    return 'system';
  }
}

// --- <dialog> helper: [appModal]="open" shows it modally; (closed) fires on Esc / backdrop / close() ---

@Directive({
  selector: 'dialog[appModal]',
  host: {
    '(close)': 'closed.emit()',
    '(click)': 'backdrop($event)',
  },
})
export class Modal {
  readonly appModal = input.required<boolean>();
  readonly closed = output<void>();
  private readonly el = inject<ElementRef<HTMLDialogElement>>(ElementRef);

  constructor() {
    effect(() => {
      const dialog = this.el.nativeElement;
      if (this.appModal() && !dialog.open) {
        dialog.showModal();
      } else if (!this.appModal() && dialog.open) {
        dialog.close();
      }
    });
  }

  backdrop(event: MouseEvent): void {
    if (event.target === this.el.nativeElement) {
      this.el.nativeElement.close();
    }
  }
}

// --- a secret that is shown exactly once ---

@Component({
  selector: 'app-secret-once',
  template: `
    <div class="secret-box" role="alert">
      <div>
        <strong>{{ label() }}</strong>
        <div class="muted">
          Copy it now. It is stored hashed and <strong>cannot be shown again</strong>; if it is lost, create a new one.
        </div>
      </div>
      <div class="value">
        <code data-testid="secret-value">{{ value() }}</code>
        <button type="button" class="btn sm" (click)="copy()">{{ copied() ? 'Copied' : 'Copy' }}</button>
      </div>
    </div>
  `,
})
export class SecretOnce {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  protected readonly copied = signal(false);

  async copy(): Promise<void> {
    try {
      await navigator.clipboard.writeText(this.value());
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    } catch {
      // clipboard blocked: the value is still selectable
    }
  }
}

// --- formatting ---

/**
 * The api stores UTC but values read back from SQL carry no offset ("2026-11-06T21:22:00"),
 * which the browser would read as local time. Treat any value without an offset as UTC.
 */
export function utc(value: string): Date {
  return new Date(/[zZ]|[+-]\d\d:?\d\d$/.test(value) || !value.includes('T') ? value : `${value}Z`);
}

export function fmtDate(value: string | null | undefined): string {
  if (!value) {
    return '–';
  }
  const d = utc(value);
  return isNaN(d.getTime()) ? '–' : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

export function fmtDateTime(value: string | null | undefined): string {
  if (!value) {
    return '–';
  }
  const d = utc(value);
  return isNaN(d.getTime())
    ? '–'
    : d.toLocaleString(undefined, { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
}

export function relative(value: string | null | undefined): string {
  if (!value) {
    return 'never';
  }
  const ms = Date.now() - utc(value).getTime();
  if (isNaN(ms)) {
    return 'never';
  }
  const min = Math.round(ms / 60_000);
  if (min < 2) return 'just now';
  if (min < 60) return `${min} min ago`;
  const h = Math.round(min / 60);
  if (h < 48) return `${h} h ago`;
  return `${Math.round(h / 24)} days ago`;
}
