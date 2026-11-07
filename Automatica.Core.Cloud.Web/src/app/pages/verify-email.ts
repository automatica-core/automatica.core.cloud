import { Component, OnInit, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Api, errorMessage } from '../core/api';

@Component({
  selector: 'app-verify-email',
  imports: [RouterLink],
  styleUrl: './auth-pages.scss',
  template: `
    <div class="auth-wrap">
      <div class="auth-card card">
        <span class="eyebrow">Automatica Cloud</span>
        <h1 style="margin: 0">Confirm address</h1>
        @switch (state()) {
          @case ('busy') {
            <p class="muted">Confirming…</p>
          }
          @case ('ok') {
            <div class="notice">Thanks, the address is confirmed. Your Automatica server may now send notifications to it.</div>
          }
          @default {
            <div class="notice bad" role="alert">{{ error() }}</div>
          }
        }
        <a class="btn block" routerLink="/servers">Open Automatica Cloud</a>
      </div>
    </div>
  `,
})
export class VerifyEmailPage implements OnInit {
  private readonly api = inject(Api);
  readonly token = input<string>();
  protected readonly state = signal<'busy' | 'ok' | 'bad'>('busy');
  protected readonly error = signal('');

  async ngOnInit(): Promise<void> {
    const token = this.token();
    if (!token) {
      this.state.set('bad');
      this.error.set('The link is incomplete.');
      return;
    }
    try {
      await firstValueFrom(this.api.verifyMail(token));
      this.state.set('ok');
    } catch (err) {
      this.state.set('bad');
      this.error.set((err as { status?: number }).status === 400 ? 'The link is invalid or has expired.' : errorMessage(err));
    }
  }
}
