import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { Api, CoreServer, MailRecipient, User, errorMessage } from '../core/api';
import { Auth } from '../core/auth';
import { Confirm, Modal, SecretOnce, Toasts, fmtDate, fmtDateTime, relative, utc } from '../core/ui';

type Health = 'ok' | 'warn' | 'bad';

@Component({
  selector: 'app-servers',
  imports: [FormsModule, Modal, SecretOnce],
  template: `
    <section class="page">
      <div class="page-head">
        <div>
          <span class="eyebrow">Remote access</span>
          <h1>Servers</h1>
          <p class="muted">
            Every Automatica server talks to the cloud with its own API key. The key is shown once, when the server is added or the key is rotated.
          </p>
        </div>
        <button type="button" class="btn primary" (click)="openCreate()">Add server</button>
      </div>

      <div class="toolbar">
        <input class="input search" type="search" placeholder="Search name, id or owner" [ngModel]="query()" (ngModelChange)="query.set($event)" aria-label="Search servers" />
        @if (auth.isAdmin()) {
          <label class="check">
            <input type="checkbox" [(ngModel)]="mineOnly" (ngModelChange)="load()" />
            Only my servers
          </label>
        }
        <span class="spacer"></span>
        <span class="muted">{{ filtered().length }} of {{ servers().length }}</span>
      </div>

      @if (loading()) {
        <div class="card empty">Loading servers…</div>
      } @else if (!servers().length) {
        <div class="card empty">
          <strong>No servers yet.</strong>
          <span>Add your first Automatica server to get an API key for it.</span>
        </div>
      } @else {
        <div class="card card-flush table-wrap">
          <table class="grid">
            <thead>
              <tr>
                <th>Server</th>
                @if (auth.isAdmin()) {
                  <th class="hide-sm">Owner</th>
                }
                <th class="hide-sm">Version</th>
                <th>Last seen</th>
                <th>License</th>
                <th class="hide-sm">Remote</th>
              </tr>
            </thead>
            <tbody>
              @for (s of filtered(); track s.objId) {
                <tr class="clickable" [class.selected]="selected()?.objId === s.objId" (click)="open(s)" tabindex="0" (keydown.enter)="open(s)">
                  <td>
                    <strong>{{ s.serverName }}</strong>
                    <div class="sub mono">{{ s.serverGuid }}</div>
                  </td>
                  @if (auth.isAdmin()) {
                    <td class="hide-sm">{{ s.ownerName ?? '–' }}</td>
                  }
                  <td class="mono hide-sm">{{ s.version && s.version !== '0.0.0.0' ? s.version : '–' }}</td>
                  <td>
                    <span class="status-dot" [class]="health(s)"></span>
                    {{ relative(s.lastKnownConnection) }}
                  </td>
                  <td>
                    @if (s.license; as l) {
                      <span class="pill" [class.ok]="!expired(l.expiresAt)" [class.bad]="expired(l.expiresAt)">
                        {{ expired(l.expiresAt) ? 'expired' : 'until' }} {{ fmtDate(l.expiresAt) }}
                      </span>
                    } @else {
                      <span class="pill">none</span>
                    }
                  </td>
                  <td class="hide-sm">
                    @if (s.license?.allowRemoteControl) {
                      <span class="pill accent">enabled</span>
                    } @else {
                      <span class="muted">–</span>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>

    <!-- add server -->
    <dialog class="dlg" [appModal]="createOpen()" (closed)="closeCreate()">
      <form (ngSubmit)="create()" #cf="ngForm">
        <header>
          <h3>{{ created() ? 'Server added' : 'Add server' }}</h3>
        </header>
        <div class="body">
          @if (created(); as c) {
            <p style="margin: 0">
              Enter the server id and this API key in the cloud settings of <strong>{{ c.name }}</strong>.
            </p>
            <dl class="kv">
              <dt>Server id</dt>
              <dd class="mono">{{ c.guid }}</dd>
            </dl>
            <app-secret-once label="API key" [value]="c.key" />
          } @else {
            <label class="field">
              <span>Name</span>
              <input class="input" name="name" [(ngModel)]="form.serverName" required maxlength="200" />
            </label>
            <label class="field">
              <span>Server id <small>(optional)</small></span>
              <input class="input mono" name="guid" [(ngModel)]="form.serverGuid" placeholder="generated when empty"
                pattern="^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$" />
              <small>Use the id of an existing installation to move it to this account.</small>
            </label>
            @if (auth.isSysAdmin()) {
              <label class="field">
                <span>Owner</span>
                <select class="input" name="owner" [(ngModel)]="form.ownerId">
                  <option [ngValue]="null">Me</option>
                  @for (u of users(); track u.objId) {
                    <option [ngValue]="u.objId">{{ u.userName }}</option>
                  }
                </select>
              </label>
            }
            @if (dialogError()) {
              <div class="notice bad" role="alert">{{ dialogError() }}</div>
            }
          }
        </div>
        <footer>
          @if (created()) {
            <button type="button" class="btn primary" (click)="closeCreate()">Done</button>
          } @else {
            <button type="button" class="btn" (click)="closeCreate()">Cancel</button>
            <button type="submit" class="btn primary" [disabled]="busy() || !cf.valid">Add server</button>
          }
        </footer>
      </form>
    </dialog>

    <!-- server details -->
    <dialog class="dlg wide" [appModal]="!!selected()" (closed)="close()">
      @if (selected(); as s) {
        <header>
          <span class="eyebrow">Server</span>
          <h3>{{ s.serverName }}</h3>
        </header>
        <div class="body">
          @if (rotated()) {
            <app-secret-once label="New API key" [value]="rotated()!" />
          }
          <dl class="kv">
            <dt>Server id</dt>
            <dd class="mono">{{ s.serverGuid }}</dd>
            <dt>Version</dt>
            <dd class="mono">{{ s.version && s.version !== '0.0.0.0' ? s.version : '–' }}{{ s.rid ? ' · ' + s.rid : '' }}</dd>
            <dt>Last seen</dt>
            <dd>{{ fmtDateTime(s.lastKnownConnection) }}</dd>
            <dt>API key</dt>
            <dd>{{ s.hasApiKey ? 'created ' + fmtDateTime(s.apiKeyCreatedAt) : 'not set (legacy key was invalidated)' }}</dd>
            @if (s.lastKnownRemoteConnectUrl) {
              <dt>Remote URL</dt>
              <dd><a [href]="s.lastKnownRemoteConnectUrl" target="_blank" rel="noopener noreferrer">{{ s.lastKnownRemoteConnectUrl }}</a></dd>
            }
            @if (s.license; as l) {
              <dt>License</dt>
              <dd>
                {{ l.licensedTo }} · until {{ fmtDate(l.expiresAt) }} · {{ l.maxDatapoints }} datapoints · {{ l.maxUsers }} users
                @if (l.allowTextToSpeech) {
                  · TTS
                }
              </dd>
            }
          </dl>

          <form class="form-grid" (ngSubmit)="save()" #ef="ngForm">
            <label class="field">
              <span>Name</span>
              <input class="input" name="editName" [(ngModel)]="edit.serverName" required maxlength="200" />
            </label>
            @if (auth.isSysAdmin()) {
              <label class="field">
                <span>Owner</span>
                <select class="input" name="editOwner" [(ngModel)]="edit.ownerId">
                  @for (u of users(); track u.objId) {
                    <option [ngValue]="u.objId">{{ u.userName }}</option>
                  }
                </select>
              </label>
            }
            <div class="full">
              <button type="submit" class="btn sm" [disabled]="busy() || !ef.valid || !ef.dirty">Save changes</button>
            </div>
          </form>

          <div>
            <h4 class="section-title">Notification recipients</h4>
            <p class="muted" style="margin: 0 0 var(--space-3)">
              The server may only send mail to its owner and to the confirmed addresses below (at most 10).
            </p>
            <div class="card card-flush table-wrap">
              <table class="grid">
                <tbody>
                  @for (r of recipients(); track r.objId) {
                    <tr>
                      <td>
                        {{ r.email }}
                        @if (r.isOwner) {
                          <span class="pill accent">owner</span>
                        }
                      </td>
                      <td>
                        @if (r.verified) {
                          <span class="pill ok">confirmed</span>
                        } @else {
                          <span class="pill warn">waiting for confirmation</span>
                        }
                      </td>
                      <td class="actions">
                        @if (!r.isOwner) {
                          @if (!r.verified) {
                            <button type="button" class="btn ghost sm" (click)="resend(r)">Resend</button>
                          }
                          <button type="button" class="btn ghost sm danger" (click)="removeRecipient(r)">Remove</button>
                        }
                      </td>
                    </tr>
                  } @empty {
                    <tr>
                      <td class="muted">No recipients.</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
            <form class="toolbar add-recipient" (ngSubmit)="addRecipient()" #rf="ngForm">
              <input class="input" type="email" name="newRecipient" [(ngModel)]="newRecipient" required email placeholder="name@example.com"
                aria-label="Add recipient" />
              <button type="submit" class="btn sm" [disabled]="busy() || !rf.valid">Send confirmation</button>
            </form>
          </div>
        </div>
        <footer>
          <button type="button" class="btn danger" (click)="remove(s)">Delete server</button>
          <span class="spacer"></span>
          <button type="button" class="btn" (click)="rotate(s)">Rotate API key</button>
          <button type="button" class="btn primary" (click)="close()">Close</button>
        </footer>
      }
    </dialog>
  `,
  styles: `
    .search {
      max-width: 320px;
    }
    .section-title {
      margin: 0 0 var(--space-1);
    }
    .add-recipient {
      margin-top: var(--space-3);
      flex-wrap: nowrap;
    }
    .add-recipient .input {
      flex: 1;
      width: auto;
    }
    td .sub.mono {
      white-space: nowrap;
    }
    @media (max-width: 599.98px) {
      td .sub.mono {
        white-space: normal;
        word-break: break-all;
      }
    }
  `,
})
export class ServersPage implements OnInit {
  protected readonly auth = inject(Auth);
  private readonly api = inject(Api);
  private readonly toasts = inject(Toasts);
  private readonly confirm = inject(Confirm);

  protected readonly fmtDate = fmtDate;
  protected readonly fmtDateTime = fmtDateTime;
  protected readonly relative = relative;

  protected readonly servers = signal<CoreServer[]>([]);
  protected readonly users = signal<User[]>([]);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly query = signal('');
  protected mineOnly = false;

  protected readonly filtered = computed(() => {
    const q = this.query().trim().toLowerCase();
    const list = this.servers();
    return q
      ? list.filter((s) => [s.serverName, s.serverGuid, s.ownerName ?? ''].some((v) => v.toLowerCase().includes(q)))
      : list;
  });

  // create
  protected readonly createOpen = signal(false);
  protected readonly created = signal<{ name: string; guid: string; key: string } | null>(null);
  protected readonly dialogError = signal('');
  protected form: { serverName: string; serverGuid: string; ownerId: string | null } = { serverName: '', serverGuid: '', ownerId: null };

  // details
  protected readonly selected = signal<CoreServer | null>(null);
  protected readonly recipients = signal<MailRecipient[]>([]);
  protected readonly rotated = signal<string | null>(null);
  protected edit: { serverName: string; ownerId: string | null } = { serverName: '', ownerId: null };
  protected newRecipient = '';

  async ngOnInit(): Promise<void> {
    await this.load();
    if (this.auth.isSysAdmin()) {
      try {
        this.users.set(await firstValueFrom(this.api.users()));
      } catch {
        // owner picker stays empty
      }
    }
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      this.servers.set(await firstValueFrom(this.api.servers(this.mineOnly)));
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  protected expired(date: string): boolean {
    return utc(date).getTime() < Date.now();
  }

  protected health(s: CoreServer): Health {
    if (!s.lastKnownConnection) {
      return 'bad';
    }
    const age = Date.now() - utc(s.lastKnownConnection).getTime();
    return age < 2 * 3600_000 ? 'ok' : age < 7 * 86400_000 ? 'warn' : 'bad';
  }

  // --- create ---

  openCreate(): void {
    this.form = { serverName: '', serverGuid: '', ownerId: null };
    this.created.set(null);
    this.dialogError.set('');
    this.createOpen.set(true);
  }

  closeCreate(): void {
    // the key is dropped from memory as soon as the dialog closes
    this.createOpen.set(false);
    this.created.set(null);
  }

  async create(): Promise<void> {
    this.busy.set(true);
    this.dialogError.set('');
    try {
      const res = await firstValueFrom(
        this.api.createServer({
          serverName: this.form.serverName.trim(),
          serverGuid: this.form.serverGuid.trim() || null,
          ownerId: this.form.ownerId,
        }),
      );
      this.created.set({ name: res.server.serverName, guid: res.server.serverGuid, key: res.apiKey });
      this.servers.update((list) => [...list, res.server].sort((a, b) => a.serverName.localeCompare(b.serverName)));
    } catch (err) {
      this.dialogError.set(errorMessage(err));
    } finally {
      this.busy.set(false);
    }
  }

  // --- details ---

  async open(s: CoreServer): Promise<void> {
    this.selected.set(s);
    this.rotated.set(null);
    this.edit = { serverName: s.serverName, ownerId: s.ownerId };
    this.newRecipient = '';
    this.recipients.set([]);
    try {
      this.recipients.set(await firstValueFrom(this.api.mailRecipients(s.objId)));
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    }
  }

  close(): void {
    this.selected.set(null);
    this.rotated.set(null);
  }

  private replace(server: CoreServer): void {
    this.servers.update((list) => list.map((s) => (s.objId === server.objId ? server : s)));
    if (this.selected()?.objId === server.objId) {
      this.selected.set(server);
    }
  }

  async save(): Promise<void> {
    const s = this.selected();
    if (!s) return;
    this.busy.set(true);
    try {
      const ownerChanged = this.auth.isSysAdmin() && this.edit.ownerId !== s.ownerId;
      const updated = await firstValueFrom(
        this.api.updateServer(s.objId, { serverName: this.edit.serverName.trim(), ownerId: ownerChanged ? this.edit.ownerId : null }),
      );
      this.replace(updated);
      this.toasts.ok('Server saved.');
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    } finally {
      this.busy.set(false);
    }
  }

  async rotate(s: CoreServer): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Rotate API key?',
      text: `${s.serverName} stops reaching the cloud (remote access, mail, updates) until the new key is entered on the server.`,
      confirm: 'Rotate key',
      danger: true,
    });
    if (!ok) return;
    try {
      const res = await firstValueFrom(this.api.rotateKey(s.objId));
      this.replace(res.server);
      this.rotated.set(res.apiKey);
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    }
  }

  async remove(s: CoreServer): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Delete server?',
      text: `This removes ${s.serverName}, its license, remote access and mail recipients. The server can no longer use the cloud.`,
      confirm: 'Delete server',
      danger: true,
      typeToConfirm: s.serverName,
    });
    if (!ok) return;
    try {
      await firstValueFrom(this.api.deleteServer(s.objId));
      this.servers.update((list) => list.filter((x) => x.objId !== s.objId));
      this.close();
      this.toasts.ok('Server deleted.');
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    }
  }

  async addRecipient(): Promise<void> {
    const s = this.selected();
    if (!s) return;
    this.busy.set(true);
    try {
      const r = await firstValueFrom(this.api.addMailRecipient(s.objId, this.newRecipient.trim()));
      this.recipients.update((list) => [...list.filter((x) => x.objId !== r.objId), r]);
      this.newRecipient = '';
      this.toasts.ok(`Confirmation mail sent to ${r.email}.`);
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    } finally {
      this.busy.set(false);
    }
  }

  async resend(r: MailRecipient): Promise<void> {
    const s = this.selected();
    if (!s) return;
    try {
      await firstValueFrom(this.api.resendVerification(s.objId, r.objId));
      this.toasts.ok(`Confirmation mail sent to ${r.email}.`);
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    }
  }

  async removeRecipient(r: MailRecipient): Promise<void> {
    const s = this.selected();
    if (!s) return;
    try {
      await firstValueFrom(this.api.deleteMailRecipient(s.objId, r.objId));
      this.recipients.update((list) => list.filter((x) => x.objId !== r.objId));
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    }
  }
}
