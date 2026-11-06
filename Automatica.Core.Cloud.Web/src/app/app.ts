import { Component, ElementRef, effect, inject, viewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Theme, Toasts } from './core/ui';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: `
    <router-outlet />
    <div class="toasts" aria-live="polite" popover="manual" #host>
      @for (t of toasts.items(); track t.id) {
        <div class="toast" [class.bad]="t.kind === 'bad'" (click)="toasts.dismiss(t.id)">{{ t.text }}</div>
      }
    </div>
  `,
  styles: `
    :host {
      display: block;
      height: 100%;
    }
    .toasts {
      position: fixed;
      inset: auto 16px 16px auto;
      margin: 0;
      padding: 0;
      border: 0;
      background: transparent;
      overflow: visible;
      display: flex;
      flex-direction: column;
      gap: 8px;
      max-width: min(420px, calc(100vw - 32px));
    }
    .toast {
      padding: 10px 14px;
      border-radius: var(--radius-md);
      background: var(--fg);
      color: var(--bg);
      box-shadow: var(--shadow);
      font-weight: 600;
      cursor: pointer;
    }
    .toast.bad {
      background: var(--bad);
      color: #fff;
    }
  `,
})
export class App {
  protected readonly toasts = inject(Toasts);
  // instantiated here so the stored theme applies before the first page renders
  private readonly theme = inject(Theme);
  private readonly host = viewChild.required<ElementRef<HTMLElement>>('host');

  constructor() {
    // a popover lives in the top layer; re-showing it puts it above any open modal dialog
    effect(() => {
      const el = this.host().nativeElement;
      const any = this.toasts.items().length > 0;
      if (!el.showPopover) {
        return;
      }
      if (el.matches(':popover-open')) {
        el.hidePopover();
      }
      if (any) {
        el.showPopover();
      }
    });
  }
}
