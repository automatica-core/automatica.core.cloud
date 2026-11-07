import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { roleNames } from './core/api';
import { Auth } from './core/auth';
import { Confirm, Modal, Theme } from './core/ui';

interface NavItem {
  path: string;
  label: string;
  icon: string;
  show: () => boolean;
}

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule, Modal],
  styleUrl: './shell.scss',
  template: `
    <div class="frame" [class.nav-open]="navOpen()">
      <aside class="nav">
        <a class="brand" routerLink="/servers" (click)="navOpen.set(false)">
          <span class="mark" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="22" height="22"><path d="M4 18 12 4l8 14" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linejoin="round" stroke-linecap="round"/><circle cx="12" cy="14.5" r="2.2" fill="var(--lamp)"/></svg>
          </span>
          <span>
            <strong>Automatica</strong>
            <small>Cloud</small>
          </span>
        </a>
        <nav>
          @for (item of visibleNav(); track item.path) {
            <a [routerLink]="item.path" routerLinkActive="active" (click)="navOpen.set(false)">
              <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true"><path [attr.d]="item.icon" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>
              {{ item.label }}
            </a>
          }
        </nav>
        <div class="nav-foot">
          <a routerLink="/account" routerLinkActive="active" class="me" (click)="navOpen.set(false)">
            <span class="avatar">{{ initials() }}</span>
            <span class="who">
              <strong>{{ auth.user()?.userName }}</strong>
              <small>{{ roleName() }}</small>
            </span>
          </a>
        </div>
      </aside>

      <div class="main">
        <header class="topbar">
          <button type="button" class="btn ghost icon menu" aria-label="Menu" (click)="navOpen.set(!navOpen())">
            <svg viewBox="0 0 24 24" width="20" height="20"><path d="M4 7h16M4 12h16M4 17h16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/></svg>
          </button>
          <span class="spacer"></span>
          <button type="button" class="btn ghost sm" (click)="theme.cycle()" [attr.aria-label]="'Theme: ' + theme.choice()">
            Theme: {{ theme.choice() }}
          </button>
          <button type="button" class="btn sm" (click)="auth.logout()">Sign out</button>
        </header>
        <main>
          <router-outlet />
        </main>
      </div>
      <div class="scrim" (click)="navOpen.set(false)"></div>
    </div>

    <dialog class="dlg" [appModal]="!!confirm.current()" (closed)="confirm.answer(false)">
      @if (confirm.current(); as c) {
        <form method="dialog" (submit)="$event.preventDefault(); confirm.answer(true)">
          <header>
            <h3>{{ c.title }}</h3>
          </header>
          <div class="body">
            <p style="margin: 0">{{ c.text }}</p>
            @if (c.typeToConfirm) {
              <label class="field">
                <span>Type <strong class="mono">{{ c.typeToConfirm }}</strong> to confirm</span>
                <input class="input" name="typed" [(ngModel)]="typed" autocomplete="off" />
              </label>
            }
          </div>
          <footer>
            <button type="button" class="btn" (click)="confirm.answer(false)">Cancel</button>
            <button type="submit" class="btn" [class.danger-fill]="c.danger" [class.primary]="!c.danger"
              [disabled]="!!c.typeToConfirm && typed !== c.typeToConfirm">{{ c.confirm }}</button>
          </footer>
        </form>
      }
    </dialog>
  `,
})
export class Shell {
  protected readonly auth = inject(Auth);
  protected readonly theme = inject(Theme);
  protected readonly confirm = inject(Confirm);
  protected readonly navOpen = signal(false);
  protected typed = '';

  private readonly nav: NavItem[] = [
    { path: '/servers', label: 'Servers', icon: 'M4 5h16v6H4zM4 13h16v6H4zM8 8h.01M8 16h.01', show: () => true },
    { path: '/licenses', label: 'Licenses', icon: 'M7 3h7l5 5v13H7zM14 3v5h5M10 13h6M10 17h6', show: () => this.auth.isAdmin() },
    { path: '/plugins', label: 'Plugins', icon: 'M9 3v4M15 3v4M6 7h12v5a6 6 0 0 1-12 0zM12 18v3', show: () => this.auth.isSysAdmin() },
    { path: '/versions', label: 'Versions', icon: 'M12 3v12M7 10l5 5 5-5M5 21h14', show: () => this.auth.isSysAdmin() },
    { path: '/deploy-keys', label: 'Deploy keys', icon: 'M15 7a4 4 0 1 1-3.9 4.9L4 19v-3h3v-3h3l1.1-1.1A4 4 0 0 1 15 7z', show: () => this.auth.isSysAdmin() },
    { path: '/users', label: 'Users', icon: 'M16 19v-1a4 4 0 0 0-8 0v1M12 11a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM20 19v-1a3 3 0 0 0-2-2.8M17 5.2a3 3 0 0 1 0 5.6', show: () => this.auth.isSysAdmin() },
  ];

  protected readonly visibleNav = computed(() => {
    this.auth.user();
    return this.nav.filter((n) => n.show());
  });

  protected readonly initials = computed(() => {
    const u = this.auth.user();
    const source = [u?.firstName, u?.lastName].filter(Boolean).join(' ') || u?.userName || '?';
    return source
      .split(/\s+/)
      .map((p) => p[0])
      .join('')
      .slice(0, 2)
      .toUpperCase();
  });

  protected readonly roleName = computed(() => {
    const u = this.auth.user();
    return u ? roleNames[u.role] : '';
  });
}
