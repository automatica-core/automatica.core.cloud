import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { errorMessage, roleNames } from '../core/api';
import { Auth } from '../core/auth';
import { Toasts, fmtDateTime } from '../core/ui';

@Component({
  selector: 'app-account',
  imports: [FormsModule],
  template: `
    <section class="page narrow">
      <div class="page-head">
        <div>
          <span class="eyebrow">Account</span>
          <h1>{{ auth.user()?.userName }}</h1>
        </div>
      </div>

      @if (auth.user(); as u) {
        <div class="card">
          <dl class="kv">
            <dt>Name</dt>
            <dd>{{ fullName() || '–' }}</dd>
            <dt>Email</dt>
            <dd>{{ u.email ?? '–' }}</dd>
            <dt>Role</dt>
            <dd>{{ roleNames[u.role] }}</dd>
            <dt>Last sign-in</dt>
            <dd>{{ fmtDateTime(u.lastLoginAt) }}</dd>
          </dl>
        </div>
      }

      <form class="card stack" (ngSubmit)="change()" #pf="ngForm">
        <h3 style="margin: 0">Change password</h3>
        <p class="muted" style="margin: 0">At least 12 characters. Other sessions are signed out.</p>
        <input type="text" name="user" autocomplete="username" [value]="auth.user()?.userName" hidden />
        <label class="field">
          <span>Current password</span>
          <input class="input" type="password" name="current" [(ngModel)]="current" required autocomplete="current-password" />
        </label>
        <label class="field">
          <span>New password</span>
          <input class="input" type="password" name="next" [(ngModel)]="next" required minlength="12" maxlength="256" autocomplete="new-password" />
        </label>
        <label class="field">
          <span>Repeat new password</span>
          <input class="input" type="password" name="repeat" [(ngModel)]="repeat" required autocomplete="new-password" />
        </label>
        @if (repeat && next !== repeat) {
          <span class="error-text">The passwords don't match.</span>
        }
        @if (error()) {
          <div class="notice bad" role="alert">{{ error() }}</div>
        }
        <div>
          <button type="submit" class="btn primary" [disabled]="busy() || !pf.valid || next !== repeat">Change password</button>
        </div>
      </form>
    </section>
  `,
  styles: `
    .narrow {
      max-width: 640px;
    }
    .stack {
      display: flex;
      flex-direction: column;
      gap: var(--space-3);
    }
  `,
})
export class AccountPage {
  protected readonly auth = inject(Auth);
  private readonly toasts = inject(Toasts);
  protected readonly roleNames = roleNames;
  protected readonly fmtDateTime = fmtDateTime;

  protected readonly fullName = () => {
    const u = this.auth.user();
    return [u?.firstName, u?.lastName].filter((v) => !!v).join(' ');
  };

  protected current = '';
  protected next = '';
  protected repeat = '';
  protected readonly busy = signal(false);
  protected readonly error = signal('');

  async change(): Promise<void> {
    this.busy.set(true);
    this.error.set('');
    try {
      await this.auth.changePassword(this.current, this.next);
      this.current = this.next = this.repeat = '';
      this.toasts.ok('Password changed.');
    } catch (err) {
      this.error.set((err as { status?: number }).status === 400 ? 'The current password is wrong or the new one is too weak.' : errorMessage(err));
    } finally {
      this.busy.set(false);
    }
  }
}
