import { Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { Api, Plugin, errorMessage } from '../core/api';
import { Confirm, Toasts } from '../core/ui';
import { Upload, UploadRequest } from './upload';

@Component({
  selector: 'app-plugins',
  imports: [FormsModule, Upload],
  template: `
    <section class="page">
      <div class="page-head">
        <div>
          <span class="eyebrow">Publishing</span>
          <h1>Plugins</h1>
          <p class="muted">Drivers and logic packages offered to Automatica servers. Only public versions reach servers.</p>
        </div>
      </div>

      <app-upload [busy]="uploading()" (upload)="upload($event)" />

      <div class="toolbar">
        <input class="input" style="max-width: 320px" type="search" placeholder="Search name, component or publisher"
          [ngModel]="query()" (ngModelChange)="query.set($event)" aria-label="Search plugins" />
        <select class="input" style="max-width: 180px" [ngModel]="branch()" (ngModelChange)="branch.set($event)" aria-label="Branch">
          <option value="">All branches</option>
          @for (b of branches(); track b) {
            <option [value]="b">{{ b }}</option>
          }
        </select>
        <label class="check">
          <input type="checkbox" [ngModel]="latestOnly()" (ngModelChange)="latestOnly.set($event)" />
          Latest version only
        </label>
        <span class="spacer"></span>
        <span class="muted">{{ filtered().length }} of {{ plugins().length }}</span>
      </div>

      @if (loading()) {
        <div class="card empty">Loading plugins…</div>
      } @else {
        <div class="card card-flush table-wrap">
          <table class="grid">
            <thead>
              <tr>
                <th>Plugin</th>
                <th>Version</th>
                <th>Branch</th>
                <th class="hide-sm">Type</th>
                <th class="hide-sm">Publisher</th>
                <th>Public</th>
                <th>Pre-release</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (p of filtered(); track p.objId) {
                <tr>
                  <td>
                    <strong>{{ p.name }}</strong>
                    <div class="sub mono">{{ p.componentName }}</div>
                  </td>
                  <td class="mono">
                    {{ p.version }}
                    <div class="sub">core ≥ {{ p.minCoreServerVersion }}</div>
                  </td>
                  <td><span class="pill">{{ p.branch }}</span></td>
                  <td class="hide-sm">{{ p.pluginType }}</td>
                  <td class="hide-sm">{{ p.publisher ?? '–' }}</td>
                  <td>
                    <label class="check"><input type="checkbox" [checked]="p.isPublic" (change)="toggle(p, 'isPublic')" [attr.aria-label]="'Public ' + p.name" /></label>
                  </td>
                  <td>
                    <label class="check"><input type="checkbox" [checked]="p.isPrerelease" (change)="toggle(p, 'isPrerelease')" [attr.aria-label]="'Pre-release ' + p.name" /></label>
                  </td>
                  <td class="actions">
                    <button type="button" class="btn ghost sm danger" (click)="remove(p)">Delete</button>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="8" class="muted">No plugins match.</td></tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `,
})
export class PluginsPage implements OnInit {
  private readonly api = inject(Api);
  private readonly toasts = inject(Toasts);
  private readonly confirm = inject(Confirm);
  private readonly uploader = viewChild(Upload);

  protected readonly plugins = signal<Plugin[]>([]);
  protected readonly loading = signal(true);
  protected readonly uploading = signal(false);
  protected readonly query = signal('');
  protected readonly branch = signal('');
  protected readonly latestOnly = signal(true);

  protected readonly branches = computed(() => [...new Set(this.plugins().map((p) => p.branch))].sort());

  protected readonly filtered = computed(() => {
    const q = this.query().trim().toLowerCase();
    const branch = this.branch();
    const seen = new Set<string>();
    // the api returns name asc, version desc, so the first per component+branch is the newest
    return this.plugins().filter((p) => {
      if (branch && p.branch !== branch) return false;
      if (q && ![p.name, p.componentName, p.publisher ?? ''].some((v) => v.toLowerCase().includes(q))) return false;
      if (this.latestOnly()) {
        const key = `${p.pluginGuid}|${p.branch}`;
        if (seen.has(key)) return false;
        seen.add(key);
      }
      return true;
    });
  });

  async ngOnInit(): Promise<void> {
    try {
      this.plugins.set(await firstValueFrom(this.api.plugins()));
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  async upload(req: UploadRequest): Promise<void> {
    this.uploading.set(true);
    try {
      const plugin = await firstValueFrom(this.api.uploadPlugin(req.file, req.branch));
      this.plugins.set(await firstValueFrom(this.api.plugins()));
      this.uploader()?.reset();
      this.toasts.ok(`${plugin.name} ${plugin.version} uploaded.`);
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    } finally {
      this.uploading.set(false);
    }
  }

  async toggle(p: Plugin, field: 'isPublic' | 'isPrerelease'): Promise<void> {
    const next = { isPublic: p.isPublic, isPrerelease: p.isPrerelease, [field]: !p[field] };
    try {
      const updated = await firstValueFrom(this.api.updatePlugin(p.objId, next));
      this.plugins.update((list) => list.map((x) => (x.objId === updated.objId ? updated : x)));
    } catch (err) {
      this.toasts.bad(errorMessage(err));
      // re-render the checkbox with the unchanged value
      this.plugins.update((list) => [...list]);
    }
  }

  async remove(p: Plugin): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Delete plugin version?',
      text: `${p.name} ${p.version} (${p.branch}) and its package file are removed. Servers that already installed it keep it.`,
      confirm: 'Delete',
      danger: true,
    });
    if (!ok) return;
    try {
      await firstValueFrom(this.api.deletePlugin(p.objId));
      this.plugins.update((list) => list.filter((x) => x.objId !== p.objId));
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    }
  }
}
