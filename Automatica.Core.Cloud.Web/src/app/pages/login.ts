import { Component, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { errorMessage } from '../core/api';
import { Auth } from '../core/auth';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  styleUrl: './auth-pages.scss',
  template: `
    <div class="auth-wrap">
      <form class="auth-card card" (ngSubmit)="submit()" #f="ngForm">
        <div class="auth-brand">
          <span class="mark" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="26" height="26"><path d="M4 18 12 4l8 14" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linejoin="round" stroke-linecap="round"/><circle cx="12" cy="14.5" r="2.2" fill="var(--lamp)"/></svg>
          </span>
          <div>
            <span class="eyebrow">Automatica</span>
            <h1>Cloud</h1>
          </div>
        </div>
        <p class="muted">Sign in to manage your Automatica servers, licenses and remote access.</p>

        <label class="field">
          <span>User name</span>
          <input class="input" name="userName" [(ngModel)]="userName" required autocomplete="username" autofocus />
        </label>
        <label class="field">
          <span>Password</span>
          <input class="input" name="password" type="password" [(ngModel)]="password" required autocomplete="current-password" />
        </label>

        @if (error()) {
          <div class="notice bad" role="alert">{{ error() }}</div>
        }

        <button class="btn primary block" type="submit" [disabled]="busy() || !f.valid">
          {{ busy() ? 'Signing in…' : 'Sign in' }}
        </button>
      </form>
    </div>
  `,
})
export class LoginPage {
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);
  readonly next = input<string>();

  protected userName = '';
  protected password = '';
  protected readonly busy = signal(false);
  protected readonly error = signal('');

  async submit(): Promise<void> {
    this.busy.set(true);
    this.error.set('');
    try {
      await this.auth.login(this.userName.trim(), this.password);
      this.password = '';
      const target = this.next();
      // only same-app paths, never an absolute url from the query string
      await this.router.navigateByUrl(target && target.startsWith('/') && !target.startsWith('//') ? target : '/servers');
    } catch (err) {
      const status = (err as { status?: number }).status;
      this.error.set(status === 401 || status === 400 ? 'User name or password is wrong, or the account is locked.' : errorMessage(err));
    } finally {
      this.busy.set(false);
    }
  }
}
