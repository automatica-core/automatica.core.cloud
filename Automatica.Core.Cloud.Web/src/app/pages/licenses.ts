import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { Api, CoreServer, GenerateLicense, License, errorMessage } from '../core/api';
import { Confirm, Modal, Toasts, fmtDate, utc } from '../core/ui';

function blank(): GenerateLicense {
  const expires = new Date();
  expires.setFullYear(expires.getFullYear() + 1);
  return {
    coreServerId: '',
    expiresAt: expires.toISOString().slice(0, 10),
    maxDatapoints: 1000,
    maxUsers: 5,
    licensedTo: '',
    email: null,
    allowRemoteControl: true,
    maxRemoteTunnels: 5,
    maxRecordingDataPoints: 100,
    maxSatellites: 0,
    allowTextToSpeech: false,
    features: [],
  };
}

@Component({
  selector: 'app-licenses',
  imports: [FormsModule, Modal],
  template: `
    <section class="page">
      <div class="page-head">
        <div>
          <span class="eyebrow">Administration</span>
          <h1>Licenses</h1>
          <p class="muted">Signed licenses per server. Generating a new license replaces the one the server has.</p>
        </div>
        <button type="button" class="btn primary" (click)="openGenerate()">Generate license</button>
      </div>

      <div class="toolbar">
        <input class="input" style="max-width: 320px" type="search" placeholder="Search server, owner or licensee"
          [ngModel]="query()" (ngModelChange)="query.set($event)" aria-label="Search licenses" />
        <span class="spacer"></span>
        <span class="muted">{{ filtered().length }} of {{ licenses().length }}</span>
      </div>

      @if (loading()) {
        <div class="card empty">Loading licenses…</div>
      } @else if (!licenses().length) {
        <div class="card empty"><strong>No licenses.</strong></div>
      } @else {
        <div class="card card-flush table-wrap">
          <table class="grid">
            <thead>
              <tr>
                <th>Server</th>
                <th>Licensed to</th>
                <th>Expires</th>
                <th class="hide-sm">Limits</th>
                <th class="hide-sm">Features</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (l of filtered(); track l.objId) {
                <tr>
                  <td>
                    <strong>{{ l.serverName ?? '–' }}</strong>
                    <div class="sub">{{ l.ownerName ?? '' }}</div>
                  </td>
                  <td>
                    {{ l.licensedTo }}
                    <div class="sub">{{ l.email ?? '' }}</div>
                  </td>
                  <td>
                    <span class="pill" [class.ok]="!expired(l)" [class.bad]="expired(l)">{{ fmtDate(l.expiresAt) }}</span>
                  </td>
                  <td class="sub hide-sm">
                    {{ l.maxDatapoints }} dp · {{ l.maxUsers }} users<br />
                    {{ l.allowRemoteControl ? l.maxRemoteTunnels + ' tunnels' : 'no remote' }} · {{ l.maxSatellites }} satellites
                  </td>
                  <td class="hide-sm">
                    @if (l.allowTextToSpeech) {
                      <span class="pill accent">TTS</span>
                    }
                    @for (f of l.features; track f) {
                      <span class="pill">{{ f }}</span>
                    }
                  </td>
                  <td class="actions">
                    <button type="button" class="btn ghost sm" (click)="copyKey(l)">Copy key</button>
                    <button type="button" class="btn ghost sm" (click)="downloadKey(l)">Download</button>
                    <button type="button" class="btn ghost sm danger" (click)="remove(l)">Delete</button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>

    <dialog class="dlg wide" [appModal]="open()" (closed)="open.set(false)">
      <form (ngSubmit)="generate()" #gf="ngForm">
        <header>
          <h3>Generate license</h3>
        </header>
        <div class="body">
          <div class="form-grid">
            <label class="field full">
              <span>Server</span>
              <select class="input" name="server" [(ngModel)]="form.coreServerId" required (ngModelChange)="prefill()">
                <option value="" disabled>Choose a server</option>
                @for (s of servers(); track s.objId) {
                  <option [value]="s.objId">{{ s.serverName }}{{ s.ownerName ? ' · ' + s.ownerName : '' }}</option>
                }
              </select>
            </label>
            <label class="field">
              <span>Licensed to</span>
              <input class="input" name="licensedTo" [(ngModel)]="form.licensedTo" required maxlength="200" />
            </label>
            <label class="field">
              <span>Email</span>
              <input class="input" type="email" name="email" [(ngModel)]="form.email" email maxlength="254" />
            </label>
            <label class="field">
              <span>Expires</span>
              <input class="input" type="date" name="expiresAt" [(ngModel)]="form.expiresAt" required />
            </label>
            <label class="field">
              <span>Datapoints</span>
              <input class="input" type="number" name="dp" [(ngModel)]="form.maxDatapoints" min="0" required />
            </label>
            <label class="field">
              <span>Users</span>
              <input class="input" type="number" name="users" [(ngModel)]="form.maxUsers" min="0" required />
            </label>
            <label class="field">
              <span>Recording datapoints</span>
              <input class="input" type="number" name="rec" [(ngModel)]="form.maxRecordingDataPoints" min="0" required />
            </label>
            <label class="field">
              <span>Satellites</span>
              <input class="input" type="number" name="sat" [(ngModel)]="form.maxSatellites" min="0" max="1000" required />
            </label>
            <label class="field">
              <span>Remote tunnels</span>
              <input class="input" type="number" name="tunnels" [(ngModel)]="form.maxRemoteTunnels" min="0" max="1000" required
                [readonly]="!form.allowRemoteControl" />
            </label>
            <div class="checks full">
              <label class="check"><input type="checkbox" name="remote" [(ngModel)]="form.allowRemoteControl" /> Remote access</label>
              <label class="check"><input type="checkbox" name="tts" [(ngModel)]="form.allowTextToSpeech" /> Text to speech</label>
            </div>
            @if (features().length) {
              <fieldset class="field full">
                <span>Features</span>
                <div class="checks">
                  @for (f of features(); track f) {
                    <label class="check">
                      <input type="checkbox" [checked]="form.features.includes(f)" (change)="toggleFeature(f)" />
                      {{ f }}
                    </label>
                  }
                </div>
              </fieldset>
            }
          </div>
          @if (dialogError()) {
            <div class="notice bad" role="alert">{{ dialogError() }}</div>
          }
        </div>
        <footer>
          <button type="button" class="btn" (click)="open.set(false)">Cancel</button>
          <button type="submit" class="btn primary" [disabled]="busy() || !gf.valid">Generate</button>
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
export class LicensesPage implements OnInit {
  private readonly api = inject(Api);
  private readonly toasts = inject(Toasts);
  private readonly confirm = inject(Confirm);
  protected readonly fmtDate = fmtDate;

  protected readonly licenses = signal<License[]>([]);
  protected readonly servers = signal<CoreServer[]>([]);
  protected readonly features = signal<string[]>([]);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly query = signal('');
  protected readonly open = signal(false);
  protected readonly dialogError = signal('');
  protected form: GenerateLicense = blank();

  protected readonly filtered = computed(() => {
    const q = this.query().trim().toLowerCase();
    return this.licenses().filter(
      (l) => !q || [l.serverName, l.ownerName, l.licensedTo, l.email].some((v) => (v ?? '').toLowerCase().includes(q)),
    );
  });

  async ngOnInit(): Promise<void> {
    try {
      const [licenses, servers, features] = await Promise.all([
        firstValueFrom(this.api.licenses()),
        firstValueFrom(this.api.servers()),
        firstValueFrom(this.api.licenseFeatures()),
      ]);
      this.licenses.set(licenses);
      this.servers.set(servers);
      this.features.set(features);
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  protected expired(l: License): boolean {
    return utc(l.expiresAt).getTime() < Date.now();
  }

  openGenerate(): void {
    this.form = blank();
    this.dialogError.set('');
    this.open.set(true);
  }

  /** Takes over the values of the server's current license, if there is one. */
  prefill(): void {
    const current = this.licenses().find((l) => l.coreServerId === this.form.coreServerId);
    if (current) {
      const { objId: _o, serverName: _s, serverGuid: _g, ownerName: _n, ...rest } = current;
      this.form = { ...rest, expiresAt: this.form.expiresAt, features: [...rest.features] };
    }
  }

  toggleFeature(f: string): void {
    this.form.features = this.form.features.includes(f) ? this.form.features.filter((x) => x !== f) : [...this.form.features, f];
  }

  async generate(): Promise<void> {
    this.busy.set(true);
    this.dialogError.set('');
    try {
      const license = await firstValueFrom(
        this.api.generateLicense({ ...this.form, email: this.form.email || null, expiresAt: new Date(this.form.expiresAt).toISOString() }),
      );
      this.licenses.update((list) => [license, ...list.filter((l) => l.coreServerId !== license.coreServerId)]);
      this.open.set(false);
      this.toasts.ok(`License for ${license.serverName ?? 'the server'} generated.`);
    } catch (err) {
      this.dialogError.set(errorMessage(err));
    } finally {
      this.busy.set(false);
    }
  }

  async copyKey(l: License): Promise<void> {
    try {
      const key = await firstValueFrom(this.api.licenseKey(l.objId));
      await navigator.clipboard.writeText(key);
      this.toasts.ok('License key copied.');
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    }
  }

  async downloadKey(l: License): Promise<void> {
    try {
      const key = await firstValueFrom(this.api.licenseKey(l.objId));
      const url = URL.createObjectURL(new Blob([key], { type: 'text/plain' }));
      const a = document.createElement('a');
      a.href = url;
      a.download = `${(l.serverName ?? 'automatica').replace(/[^A-Za-z0-9._-]+/g, '_')}.license`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    }
  }

  async remove(l: License): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Delete license?',
      text: `${l.serverName ?? 'The server'} falls back to the free limits the next time it checks its license.`,
      confirm: 'Delete license',
      danger: true,
    });
    if (!ok) return;
    try {
      await firstValueFrom(this.api.deleteLicense(l.objId));
      this.licenses.update((list) => list.filter((x) => x.objId !== l.objId));
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    }
  }
}
