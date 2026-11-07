import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { Api, User, UserSave, errorMessage, roleNames, userRoles } from '../core/api';
import { Auth } from '../core/auth';
import { Confirm, Modal, SecretOnce, Toasts, fmtDateTime, utc } from '../core/ui';

const blank = (): UserSave & { objId?: string } => ({
  userName: '',
  email: null,
  firstName: null,
  lastName: null,
  publisherName: null,
  role: 'Viewer',
  enabled: true,
});

@Component({
  selector: 'app-users',
  imports: [FormsModule, Modal, SecretOnce],
  template: `
    <section class="page">
      <div class="page-head">
        <div>
          <span class="eyebrow">Administration</span>
          <h1>Users</h1>
          <p class="muted">Accounts of the cloud portal. New accounts get a random password that is shown once.</p>
        </div>
        <button type="button" class="btn primary" (click)="edit(null)">Add user</button>
      </div>

      <div class="toolbar">
        <input class="input" style="max-width: 320px" type="search" placeholder="Search name or email"
          [ngModel]="query()" (ngModelChange)="query.set($event)" aria-label="Search users" />
        <span class="spacer"></span>
        <span class="muted">{{ filtered().length }} of {{ users().length }}</span>
      </div>

      @if (loading()) {
        <div class="card empty">Loading users…</div>
      } @else {
        <div class="card card-flush table-wrap">
          <table class="grid">
            <thead>
              <tr>
                <th>User</th>
                <th>Role</th>
                <th>Status</th>
                <th class="hide-sm">Last sign-in</th>
              </tr>
            </thead>
            <tbody>
              @for (u of filtered(); track u.objId) {
                <tr class="clickable" tabindex="0" (click)="edit(u)" (keydown.enter)="edit(u)">
                  <td>
                    <strong>{{ u.userName }}</strong>
                    <div class="sub">{{ fullName(u) }}{{ u.email ? ' · ' + u.email : '' }}</div>
                  </td>
                  <td>{{ roleNames[u.role] }}</td>
                  <td>
                    @if (!u.enabled) {
                      <span class="pill bad">disabled</span>
                    } @else if (isLocked(u)) {
                      <span class="pill warn">locked</span>
                    } @else if (!u.hasPassword) {
                      <span class="pill warn">no password</span>
                    } @else {
                      <span class="pill ok">active</span>
                    }
                  </td>
                  <td class="hide-sm">{{ fmtDateTime(u.lastLoginAt) }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>

    <dialog class="dlg" [appModal]="open()" (closed)="close()">
      <form (ngSubmit)="save()" #uf="ngForm">
        <header>
          <h3>{{ password() ? 'Initial password' : form.objId ? 'Edit user' : 'Add user' }}</h3>
        </header>
        <div class="body">
          @if (password(); as p) {
            <p style="margin: 0">Hand it to <strong>{{ form.userName }}</strong> over a separate channel. They should change it after signing in.</p>
            <app-secret-once label="Password" [value]="p" />
          } @else {
            <div class="form-grid">
              <label class="field">
                <span>User name</span>
                <input class="input" name="userName" [(ngModel)]="form.userName" required minlength="2" maxlength="64" pattern="^[A-Za-z0-9._@\\-]+$"
                  autocomplete="off" />
              </label>
              <label class="field">
                <span>Email</span>
                <input class="input" type="email" name="email" [(ngModel)]="form.email" email maxlength="254" />
              </label>
              <label class="field">
                <span>First name</span>
                <input class="input" name="firstName" [(ngModel)]="form.firstName" maxlength="100" />
              </label>
              <label class="field">
                <span>Last name</span>
                <input class="input" name="lastName" [(ngModel)]="form.lastName" maxlength="100" />
              </label>
              <label class="field">
                <span>Role</span>
                <select class="input" name="role" [(ngModel)]="form.role" [disabled]="isSelf()">
                  @for (r of roles; track r) {
                    <option [value]="r">{{ roleNames[r] }}</option>
                  }
                </select>
              </label>
              <label class="field">
                <span>Publisher name</span>
                <input class="input" name="publisherName" [(ngModel)]="form.publisherName" maxlength="100" />
              </label>
              <label class="check full">
                <input type="checkbox" name="enabled" [(ngModel)]="form.enabled" [disabled]="isSelf()" />
                Account enabled
              </label>
            </div>
            @if (selected(); as u) {
              @if (isLocked(u)) {
                <div class="notice">Locked after failed sign-ins until {{ fmtDateTime(u.lockedUntil) }}. Resetting the password unlocks it.</div>
              }
            }
            @if (dialogError()) {
              <div class="notice bad" role="alert">{{ dialogError() }}</div>
            }
          }
        </div>
        <footer>
          @if (password()) {
            <button type="button" class="btn primary" (click)="close()">Done</button>
          } @else {
            @if (selected(); as u) {
              @if (!isSelf()) {
                <button type="button" class="btn danger" (click)="remove(u)">Delete</button>
              }
              <button type="button" class="btn" (click)="reset(u)">Reset password</button>
            }
            <span class="spacer"></span>
            <button type="button" class="btn" (click)="close()">Cancel</button>
            <button type="submit" class="btn primary" [disabled]="busy() || !uf.valid">{{ form.objId ? 'Save' : 'Add user' }}</button>
          }
        </footer>
      </form>
    </dialog>
  `,
})
export class UsersPage implements OnInit {
  private readonly api = inject(Api);
  private readonly auth = inject(Auth);
  private readonly toasts = inject(Toasts);
  private readonly confirm = inject(Confirm);
  protected readonly fmtDateTime = fmtDateTime;
  protected readonly roleNames = roleNames;
  protected readonly roles = userRoles;

  protected readonly users = signal<User[]>([]);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly query = signal('');
  protected readonly open = signal(false);
  protected readonly selected = signal<User | null>(null);
  protected readonly password = signal<string | null>(null);
  protected readonly dialogError = signal('');
  protected form: UserSave & { objId?: string } = blank();

  protected readonly isSelf = computed(() => !!this.selected() && this.selected()!.objId === this.auth.user()?.objId);

  protected readonly filtered = computed(() => {
    const q = this.query().trim().toLowerCase();
    return this.users().filter(
      (u) => !q || [u.userName, u.email, u.firstName, u.lastName].some((v) => (v ?? '').toLowerCase().includes(q)),
    );
  });

  async ngOnInit(): Promise<void> {
    try {
      this.users.set(await firstValueFrom(this.api.users()));
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  protected fullName(u: User): string {
    return [u.firstName, u.lastName].filter(Boolean).join(' ');
  }

  protected isLocked(u: User): boolean {
    return !!u.lockedUntil && utc(u.lockedUntil).getTime() > Date.now();
  }

  edit(u: User | null): void {
    this.selected.set(u);
    this.password.set(null);
    this.dialogError.set('');
    this.form = u
      ? {
          objId: u.objId,
          userName: u.userName,
          email: u.email,
          firstName: u.firstName,
          lastName: u.lastName,
          publisherName: u.publisherName,
          role: u.role,
          enabled: u.enabled,
        }
      : blank();
    this.open.set(true);
  }

  close(): void {
    this.open.set(false);
    this.password.set(null);
    this.selected.set(null);
  }

  private upsert(u: User): void {
    this.users.update((list) =>
      (list.some((x) => x.objId === u.objId) ? list.map((x) => (x.objId === u.objId ? u : x)) : [...list, u]).sort((a, b) =>
        a.userName.localeCompare(b.userName),
      ),
    );
  }

  async save(): Promise<void> {
    this.busy.set(true);
    this.dialogError.set('');
    const { objId, ...data } = this.form;
    const body: UserSave = { ...data, userName: data.userName.trim(), email: data.email || null };
    try {
      if (objId) {
        const u = await firstValueFrom(this.api.updateUser(objId, body));
        this.upsert(u);
        this.close();
        this.toasts.ok('User saved.');
      } else {
        const res = await firstValueFrom(this.api.createUser(body));
        this.upsert(res.user);
        this.selected.set(res.user);
        if (res.initialPassword) {
          this.password.set(res.initialPassword);
        } else {
          this.close();
        }
      }
    } catch (err) {
      this.dialogError.set(errorMessage(err));
    } finally {
      this.busy.set(false);
    }
  }

  async reset(u: User): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Reset password?',
      text: `${u.userName} gets a new random password and is signed out everywhere.`,
      confirm: 'Reset password',
      danger: true,
    });
    if (!ok) return;
    try {
      const res = await firstValueFrom(this.api.resetPassword(u.objId, null));
      this.upsert(res.user);
      this.password.set(res.initialPassword);
    } catch (err) {
      this.dialogError.set(errorMessage(err));
    }
  }

  async remove(u: User): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Delete user?',
      text: `${u.userName} is removed. Servers they own have to be moved to another user first.`,
      confirm: 'Delete user',
      danger: true,
      typeToConfirm: u.userName,
    });
    if (!ok) return;
    try {
      await firstValueFrom(this.api.deleteUser(u.objId));
      this.users.update((list) => list.filter((x) => x.objId !== u.objId));
      this.close();
    } catch (err) {
      this.dialogError.set(errorMessage(err));
    }
  }
}
