import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

export interface UploadRequest {
  file: File;
  branch: string;
}

/** File + branch picker used for plugin and server-version uploads. */
@Component({
  selector: 'app-upload',
  imports: [FormsModule],
  template: `
    <form class="card upload" (ngSubmit)="submit()">
      <label class="drop" [class.over]="over()" (dragover)="$event.preventDefault(); over.set(true)" (dragleave)="over.set(false)"
        (drop)="drop($event)">
        <input type="file" [accept]="accept()" (change)="pick($event)" />
        <strong>{{ file()?.name ?? 'Choose a package' }}</strong>
        <span class="muted">{{ file() ? size(file()!.size) : hint() }}</span>
      </label>
      <label class="field branch">
        <span>Branch</span>
        <input class="input" name="branch" [(ngModel)]="branch" required pattern="^[A-Za-z0-9._\\-]+$" />
      </label>
      <button type="submit" class="btn primary" [disabled]="busy() || !file() || !branch">{{ busy() ? 'Uploading…' : 'Upload' }}</button>
    </form>
  `,
  styles: `
    .upload {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-end;
      gap: var(--space-3);
    }
    .drop {
      flex: 1 1 260px;
      display: flex;
      flex-direction: column;
      gap: 2px;
      padding: var(--space-3) var(--space-4);
      border: 1.5px dashed var(--line-strong);
      border-radius: var(--radius-md);
      cursor: pointer;
      position: relative;
    }
    .drop.over,
    .drop:hover {
      border-color: var(--accent);
      background: var(--accent-soft);
    }
    .drop input {
      position: absolute;
      inset: 0;
      opacity: 0;
      cursor: pointer;
    }
    .branch {
      flex: 0 1 180px;
    }
  `,
})
export class Upload {
  readonly accept = input('.acpkg,.zip');
  readonly hint = input('Drop a file here or click to choose');
  readonly busy = input(false);
  readonly upload = output<UploadRequest>();

  protected readonly file = signal<File | null>(null);
  protected readonly over = signal(false);
  protected branch = 'develop';

  pick(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file.set(input.files?.[0] ?? null);
  }

  drop(event: DragEvent): void {
    event.preventDefault();
    this.over.set(false);
    this.file.set(event.dataTransfer?.files?.[0] ?? null);
  }

  submit(): void {
    const file = this.file();
    if (file && this.branch) {
      this.upload.emit({ file, branch: this.branch.trim() });
    }
  }

  /** called by the page once the upload went through */
  reset(): void {
    this.file.set(null);
  }

  size(bytes: number): string {
    return bytes > 1 << 20 ? `${(bytes / (1 << 20)).toFixed(1)} MB` : `${Math.max(1, Math.round(bytes / 1024))} kB`;
  }
}
