import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { Api, DeployKey, DeployKeyCreate, DeployScope, User, deployScopes, errorMessage, roleAtLeast } from '../core/api';
import { Confirm, Modal, SecretOnce, Toasts, fmtDate, fmtDateTime, utc } from '../core/ui';

const scopeText: Record<DeployScope, string> = {
  DeployPlugin: 'Publish plugins',
  DeployUpdate: 'Publish update packages',
  DeployDocker: 'Publish Docker versions',
  ReadPlugins: 'Read plugin list',
};

const blank = (): DeployKeyCreate => ({ name: '', scopes: ['DeployPlugin'], allowedBranches: 'develop', publisherUserId: null, expiresInDays: 365 });

@Component({
  selector: 'app-deploy-keys',
  imports: [FormsModule, Modal, SecretOnce],
  template: `
    <section class="page">
      <div class="page-head">
        <div>
          <span class="eyebrow">Publishing</span>
          <h1>Deploy keys</h1>
          <p class="muted">
            Keys for CI pipelines (automatica-cli). Each key is limited to its scopes and branches and is shown once when it is created.
          </p>
        </div>
        <button type="button" class="btn primary" (click)="openCreate()">Create deploy key</button>
      </div>

      @if (loading()) {
        <div class="card empty">Loading deploy keys…</div>
      } @else if (!keys().length) {
        <div class="card empty">
          <strong>No deploy keys.</strong>
          <span>Create one per pipeline, so a leaked key can be revoked without touching the others.</span>
        </div>
      } @else {
        <div class="card card-flush table-wrap">
          <table class="grid">
            <thead>
              <tr>
                <th>Name</th>
                <th>Scopes</th>
                <th class="hide-sm">Branches</th>
                <th class="hide-sm">Publishes as</th>
                <th>Expires</th>
                <th class="hide-sm">Last used</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (k of keys(); track k.objId) {
                <tr>
                  <td>
                    <strong>{{ k.name }}</strong>
                    <div class="sub mono">{{ k.keyPrefix }}…</div>
                  </td>
                  <td>
                    @for (s of k.scopes; track s) {
                      <span class="pill">{{ s }}</span>
                    }
                  </td>
                  <td class="mono hide-sm">{{ k.allowedBranches }}</td>
                  <td class="hide-sm">{{ k.publisherUserName ?? '–' }}</td>
                  <td>
                    @if (k.revokedAt) {
                      <span class="pill bad">revoked {{ fmtDate(k.revokedAt) }}</span>
                    } @else if (k.expiresAt && isPast(k.expiresAt)) {
                      <span class="pill bad">expired</span>
                    } @else {
                      {{ k.expiresAt ? fmtDate(k.expiresAt) : 'never' }}
                    }
                  </td>
                  <td class="hide-sm">
                    {{ fmtDateTime(k.lastUsedAt) }}
                    <div class="sub mono">{{ k.lastUsedIp ?? '' }}</div>
                  </td>
                  <td class="actions">
                    @if (!k.revokedAt) {
                      <button type="button" class="btn ghost sm danger" (click)="revoke(k)">Revoke</button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>

    <dialog class="dlg" [appModal]="open()" (closed)="close()">
      <form (ngSubmit)="create()" #kf="ngForm">
        <header>
          <h3>{{ created() ? 'Deploy key created' : 'Create deploy key' }}</h3>
        </header>
        <div class="body">
          @if (created(); as c) {
            <p style="margin: 0">Store it as a secret variable in the pipeline and pass it to automatica-cli.</p>
            <app-secret-once label="Deploy key" [value]="c" />
          } @else {
            <label class="field">
              <span>Name</span>
              <input class="input" name="name" [(ngModel)]="form.name" required minlength="2" maxlength="100" placeholder="e.g. drivers pipeline" />
            </label>
            <fieldset class="field">
              <span>Scopes</span>
              <div class="checks">
                @for (s of scopes; track s) {
                  <label class="check">
                    <input type="checkbox" [checked]="form.scopes.includes(s)" (change)="toggle(s)" />
                    {{ scopeText[s] }}
                  </label>
                }
              </div>
            </fieldset>
            <label class="field">
              <span>Branches</span>
              <input class="input mono" name="branches" [(ngModel)]="form.allowedBranches" required
                pattern="^(\\*|[A-Za-z0-9._\\-]+)(,(\\*|[A-Za-z0-9._\\-]+))*$" />
              <small>Comma separated, <span class="mono">*</span> for all. Uploads to other branches are refused.</small>
            </label>
            <div class="form-grid">
              <label class="field">
                <span>Publishes as</span>
                <select class="input" name="publisher" [(ngModel)]="form.publisherUserId">
                  <option [ngValue]="null">Me</option>
                  @for (u of publishers(); track u.objId) {
                    <option [ngValue]="u.objId">{{ u.userName }}{{ u.publisherName ? ' · ' + u.publisherName : '' }}</option>
                  }
                </select>
              </label>
              <label class="field">
                <span>Valid for (days)</span>
                <input class="input" type="number" name="days" [(ngModel)]="form.expiresInDays" min="1" max="3650" required />
              </label>
            </div>
            @if (dialogError()) {
              <div class="notice bad" role="alert">{{ dialogError() }}</div>
            }
          }
        </div>
        <footer>
          @if (created()) {
            <button type="button" class="btn primary" (click)="close()">Done</button>
          } @else {
            <button type="button" class="btn" (click)="close()">Cancel</button>
            <button type="submit" class="btn primary" [disabled]="busy() || !kf.valid || !form.scopes.length">Create key</button>
          }
        </footer>
      </form>
    </dialog>
  `,
  styles: `
    fieldset.field {
      border: 0;
      padding: 0;
      margin: 0;
    }
    td .pill + .pill {
      margin-left: 4px;
    }
  `,
})
export class DeployKeysPage implements OnInit {
  private readonly api = inject(Api);
  private readonly toasts = inject(Toasts);
  private readonly confirm = inject(Confirm);
  protected readonly fmtDate = fmtDate;
  protected readonly fmtDateTime = fmtDateTime;
  protected readonly scopes = deployScopes;
  protected readonly scopeText = scopeText;

  protected readonly keys = signal<DeployKey[]>([]);
  protected readonly publishers = signal<User[]>([]);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly open = signal(false);
  protected readonly created = signal<string | null>(null);
  protected readonly dialogError = signal('');
  protected form: DeployKeyCreate = blank();

  async ngOnInit(): Promise<void> {
    try {
      const [keys, users] = await Promise.all([firstValueFrom(this.api.deployKeys()), firstValueFrom(this.api.users())]);
      this.keys.set(keys);
      this.publishers.set(users.filter((u) => u.enabled && roleAtLeast(u.role, 'Manager')));
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  protected isPast(date: string): boolean {
    return utc(date).getTime() < Date.now();
  }

  openCreate(): void {
    this.form = blank();
    this.created.set(null);
    this.dialogError.set('');
    this.open.set(true);
  }

  close(): void {
    this.open.set(false);
    this.created.set(null);
  }

  toggle(s: DeployScope): void {
    this.form.scopes = this.form.scopes.includes(s) ? this.form.scopes.filter((x) => x !== s) : [...this.form.scopes, s];
  }

  async create(): Promise<void> {
    this.busy.set(true);
    this.dialogError.set('');
    try {
      const res = await firstValueFrom(this.api.createDeployKey({ ...this.form, name: this.form.name.trim(), allowedBranches: this.form.allowedBranches.trim() }));
      this.keys.update((list) => [res.deployKey, ...list]);
      this.created.set(res.key);
    } catch (err) {
      this.dialogError.set(errorMessage(err));
    } finally {
      this.busy.set(false);
    }
  }

  async revoke(k: DeployKey): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Revoke deploy key?',
      text: `Pipelines using "${k.name}" can no longer publish. This cannot be undone.`,
      confirm: 'Revoke',
      danger: true,
    });
    if (!ok) return;
    try {
      const updated = await firstValueFrom(this.api.revokeDeployKey(k.objId));
      this.keys.update((list) => list.map((x) => (x.objId === updated.objId ? updated : x)));
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    }
  }
}
