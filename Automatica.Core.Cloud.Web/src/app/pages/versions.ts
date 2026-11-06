import { Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { Api, DockerVersion, ServerVersion, errorMessage } from '../core/api';
import { Confirm, Modal, Toasts } from '../core/ui';
import { Upload, UploadRequest } from './upload';

const blankDocker = (): DockerVersion => ({
  version: '',
  imageName: 'automaticacore/automatica',
  imageTag: '',
  branch: 'develop',
  changeLog: null,
  isPreRelease: false,
  isPublic: false,
});

@Component({
  selector: 'app-versions',
  imports: [FormsModule, Modal, Upload],
  template: `
    <section class="page">
      <div class="page-head">
        <div>
          <span class="eyebrow">Publishing</span>
          <h1>Versions</h1>
          <p class="muted">Server updates offered to Automatica installations, as update packages or Docker images.</p>
        </div>
        @if (tab() === 'docker') {
          <button type="button" class="btn primary" (click)="editDocker(null)">Add Docker version</button>
        }
      </div>

      <div class="tabs" role="tablist">
        <button type="button" role="tab" [class.active]="tab() === 'server'" [attr.aria-selected]="tab() === 'server'" (click)="tab.set('server')">
          Update packages
        </button>
        <button type="button" role="tab" [class.active]="tab() === 'docker'" [attr.aria-selected]="tab() === 'docker'" (click)="tab.set('docker')">
          Docker images
        </button>
      </div>

      <div class="toolbar">
        <select class="input" style="max-width: 180px" [ngModel]="branch()" (ngModelChange)="branch.set($event)" aria-label="Branch">
          <option value="">All branches</option>
          @for (b of branches(); track b) {
            <option [value]="b">{{ b }}</option>
          }
        </select>
      </div>

      @if (tab() === 'server') {
        <app-upload hint="Drop an update package (.zip) here or click to choose" accept=".zip" [busy]="uploading()" (upload)="upload($event)" />

        <div class="card card-flush table-wrap">
          <table class="grid">
            <thead>
              <tr>
                <th>Version</th>
                <th>Runtime</th>
                <th>Branch</th>
                <th>Public</th>
                <th>Pre-release</th>
                <th>Change log</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (v of serverFiltered(); track v.objId) {
                <tr>
                  <td class="mono"><strong>{{ v.version }}</strong></td>
                  <td class="mono">{{ v.rid }}</td>
                  <td><span class="pill">{{ v.branch }}</span></td>
                  <td>
                    <label class="check"><input type="checkbox" [checked]="v.isPublic" (change)="toggleServer(v, 'isPublic')" aria-label="Public" /></label>
                  </td>
                  <td>
                    <label class="check"><input type="checkbox" [checked]="v.isPreRelease" (change)="toggleServer(v, 'isPreRelease')" aria-label="Pre-release" /></label>
                  </td>
                  <td class="sub changelog">{{ v.changeLog || '–' }}</td>
                  <td class="actions">
                    <button type="button" class="btn ghost sm" (click)="editChangeLog(v)">Change log</button>
                    <button type="button" class="btn ghost sm danger" (click)="removeServer(v)">Delete</button>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="7" class="muted">{{ loading() ? 'Loading…' : 'No update packages.' }}</td></tr>
              }
            </tbody>
          </table>
        </div>
      } @else {
        <div class="card card-flush table-wrap">
          <table class="grid">
            <thead>
              <tr>
                <th>Version</th>
                <th>Image</th>
                <th>Branch</th>
                <th>Public</th>
                <th>Pre-release</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (v of dockerFiltered(); track v.objId) {
                <tr class="clickable" (click)="editDocker(v)">
                  <td class="mono"><strong>{{ v.version }}</strong></td>
                  <td class="mono">{{ v.imageName }}:{{ v.imageTag }}</td>
                  <td><span class="pill">{{ v.branch }}</span></td>
                  <td>{{ v.isPublic ? 'yes' : 'no' }}</td>
                  <td>{{ v.isPreRelease ? 'yes' : 'no' }}</td>
                  <td class="actions">
                    <button type="button" class="btn ghost sm danger" (click)="$event.stopPropagation(); removeDocker(v)">Delete</button>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="6" class="muted">{{ loading() ? 'Loading…' : 'No Docker versions.' }}</td></tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>

    <dialog class="dlg" [appModal]="!!changeLogFor()" (closed)="changeLogFor.set(null)">
      <form (ngSubmit)="saveChangeLog()">
        <header>
          <h3>Change log {{ changeLogFor()?.version }}</h3>
        </header>
        <div class="body">
          <textarea class="input" name="changeLog" rows="8" [(ngModel)]="changeLog" aria-label="Change log"></textarea>
        </div>
        <footer>
          <button type="button" class="btn" (click)="changeLogFor.set(null)">Cancel</button>
          <button type="submit" class="btn primary">Save</button>
        </footer>
      </form>
    </dialog>

    <dialog class="dlg" [appModal]="dockerOpen()" (closed)="dockerOpen.set(false)">
      <form (ngSubmit)="saveDocker()" #df="ngForm">
        <header>
          <h3>{{ docker.objId ? 'Edit Docker version' : 'Add Docker version' }}</h3>
        </header>
        <div class="body">
          <div class="form-grid">
            <label class="field">
              <span>Version</span>
              <input class="input mono" name="version" [(ngModel)]="docker.version" required pattern="^\\d+(\\.\\d+){1,3}$" placeholder="10.0.0.1" />
            </label>
            <label class="field">
              <span>Branch</span>
              <input class="input" name="branch" [(ngModel)]="docker.branch" required pattern="^[A-Za-z0-9._\\-]+$" />
            </label>
            <label class="field">
              <span>Image</span>
              <input class="input mono" name="imageName" [(ngModel)]="docker.imageName" required />
            </label>
            <label class="field">
              <span>Tag</span>
              <input class="input mono" name="imageTag" [(ngModel)]="docker.imageTag" required />
            </label>
            <label class="field full">
              <span>Change log</span>
              <textarea class="input" name="changeLog" rows="4" [(ngModel)]="docker.changeLog"></textarea>
            </label>
            <div class="checks full">
              <label class="check"><input type="checkbox" name="public" [(ngModel)]="docker.isPublic" /> Public</label>
              <label class="check"><input type="checkbox" name="pre" [(ngModel)]="docker.isPreRelease" /> Pre-release</label>
            </div>
          </div>
        </div>
        <footer>
          <button type="button" class="btn" (click)="dockerOpen.set(false)">Cancel</button>
          <button type="submit" class="btn primary" [disabled]="!df.valid">Save</button>
        </footer>
      </form>
    </dialog>
  `,
  styles: `
    .changelog {
      max-width: 360px;
      white-space: pre-line;
      overflow: hidden;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
    }
  `,
})
export class VersionsPage implements OnInit {
  private readonly api = inject(Api);
  private readonly toasts = inject(Toasts);
  private readonly confirm = inject(Confirm);
  private readonly uploader = viewChild(Upload);

  protected readonly tab = signal<'server' | 'docker'>('server');
  protected readonly branch = signal('');
  protected readonly loading = signal(true);
  protected readonly uploading = signal(false);
  protected readonly server = signal<ServerVersion[]>([]);
  protected readonly dockerList = signal<DockerVersion[]>([]);

  protected readonly changeLogFor = signal<ServerVersion | null>(null);
  protected changeLog = '';
  protected readonly dockerOpen = signal(false);
  protected docker: DockerVersion = blankDocker();

  protected readonly branches = computed(() =>
    [...new Set([...this.server().map((v) => v.branch), ...this.dockerList().map((v) => v.branch)])].sort(),
  );
  protected readonly serverFiltered = computed(() => this.server().filter((v) => !this.branch() || v.branch === this.branch()));
  protected readonly dockerFiltered = computed(() => this.dockerList().filter((v) => !this.branch() || v.branch === this.branch()));

  async ngOnInit(): Promise<void> {
    try {
      const [server, docker] = await Promise.all([firstValueFrom(this.api.serverVersions()), firstValueFrom(this.api.dockerVersions())]);
      this.server.set(server);
      this.dockerList.set(docker);
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  async upload(req: UploadRequest): Promise<void> {
    this.uploading.set(true);
    try {
      const v = await firstValueFrom(this.api.uploadServerVersion(req.file, req.branch));
      this.server.set(await firstValueFrom(this.api.serverVersions()));
      this.uploader()?.reset();
      this.toasts.ok(`Version ${v.version} (${v.rid}) uploaded. It is not public yet.`);
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    } finally {
      this.uploading.set(false);
    }
  }

  private async updateServer(v: ServerVersion, data: { changeLog: string | null; isPreRelease: boolean; isPublic: boolean }): Promise<void> {
    try {
      const updated = await firstValueFrom(this.api.updateServerVersion(v.objId, data));
      this.server.update((list) => list.map((x) => (x.objId === updated.objId ? updated : x)));
    } catch (err) {
      this.toasts.bad(errorMessage(err));
      this.server.update((list) => [...list]);
    }
  }

  toggleServer(v: ServerVersion, field: 'isPublic' | 'isPreRelease'): Promise<void> {
    return this.updateServer(v, { changeLog: v.changeLog, isPreRelease: v.isPreRelease, isPublic: v.isPublic, [field]: !v[field] });
  }

  editChangeLog(v: ServerVersion): void {
    this.changeLog = v.changeLog ?? '';
    this.changeLogFor.set(v);
  }

  async saveChangeLog(): Promise<void> {
    const v = this.changeLogFor();
    if (!v) return;
    await this.updateServer(v, { changeLog: this.changeLog || null, isPreRelease: v.isPreRelease, isPublic: v.isPublic });
    this.changeLogFor.set(null);
  }

  async removeServer(v: ServerVersion): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Delete update package?',
      text: `Version ${v.version} (${v.rid}, ${v.branch}) and its file are removed.`,
      confirm: 'Delete',
      danger: true,
    });
    if (!ok) return;
    try {
      await firstValueFrom(this.api.deleteServerVersion(v.objId));
      this.server.update((list) => list.filter((x) => x.objId !== v.objId));
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    }
  }

  editDocker(v: DockerVersion | null): void {
    this.docker = v ? { ...v } : blankDocker();
    this.dockerOpen.set(true);
  }

  async saveDocker(): Promise<void> {
    try {
      const saved = await firstValueFrom(this.api.saveDockerVersion({ ...this.docker, changeLog: this.docker.changeLog || null }));
      this.dockerList.update((list) =>
        this.docker.objId ? list.map((x) => (x.objId === saved.objId ? saved : x)) : [saved, ...list],
      );
      this.dockerOpen.set(false);
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    }
  }

  async removeDocker(v: DockerVersion): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Delete Docker version?',
      text: `${v.imageName}:${v.imageTag} is no longer offered as an update. The image itself stays in the registry.`,
      confirm: 'Delete',
      danger: true,
    });
    if (!ok || !v.objId) return;
    try {
      await firstValueFrom(this.api.deleteDockerVersion(v.objId));
      this.dockerList.update((list) => list.filter((x) => x.objId !== v.objId));
    } catch (err) {
      this.toasts.bad(errorMessage(err));
    }
  }
}
