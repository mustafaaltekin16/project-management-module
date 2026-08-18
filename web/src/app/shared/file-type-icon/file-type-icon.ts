import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

export type FileVisualKind = 'word' | 'powerpoint' | 'excel' | 'pdf' | 'file' | 'image' | 'video';

@Component({
  selector: 'app-file-type-icon',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg
      [attr.width]="size"
      [attr.height]="size"
      viewBox="0 0 72 72"
      role="img"
      [attr.aria-label]="label"
      class="file-visual"
    >
      @switch (kind) {
        @case ('word') {
          <path fill="#fff" stroke="#dbe3ec" d="M19 5h35l11 11v50H19z" />
          <path fill="#e8f2fc" d="M54 5v12h11z" />
          <path fill="#2b79c8" d="M29 23h28v4H29zm0 9h28v4H29zm0 9h28v4H29zm0 9h20v4H29z" />
          <rect x="3" y="17" width="34" height="43" rx="3" fill="#1767b2" />
          <text x="20" y="46" text-anchor="middle" fill="#fff" font-size="25" font-weight="800" font-family="Arial, sans-serif">W</text>
        }
        @case ('powerpoint') {
          <path fill="#fff" stroke="#e5dfdc" d="M19 5h35l11 11v50H19z" />
          <path fill="#fae7df" d="M54 5v12h11z" />
          <circle cx="46" cy="29" r="11" fill="#f8b39c" />
          <path fill="#ef6338" d="M46 18a11 11 0 0 1 11 11H46z" />
          <path stroke="#ef6338" stroke-width="4" d="M30 48h27M30 55h20" />
          <rect x="3" y="17" width="34" height="43" rx="3" fill="#e95428" />
          <text x="20" y="46" text-anchor="middle" fill="#fff" font-size="25" font-weight="800" font-family="Arial, sans-serif">P</text>
        }
        @case ('excel') {
          <path fill="#fff" stroke="#dce6e1" d="M19 5h35l11 11v50H19z" />
          <path fill="#dff2e8" d="M54 5v12h11z" />
          <path stroke="#2b9b59" stroke-width="2" d="M29 23h29v31H29zm0 8h29m-29 8h29m-29 8h29M39 23v31m10-31v31" />
          <rect x="3" y="17" width="34" height="43" rx="3" fill="#238c4e" />
          <text x="20" y="46" text-anchor="middle" fill="#fff" font-size="25" font-weight="800" font-family="Arial, sans-serif">X</text>
        }
        @case ('pdf') {
          <path fill="#edf0f3" stroke="#d9dee4" d="M12 4h39l12 12v52H12z" />
          <path fill="#d7dde2" d="M51 4v13h12z" />
          <rect x="4" y="38" width="58" height="22" rx="3" fill="#ef4438" />
          <text x="33" y="54" text-anchor="middle" fill="#fff" font-size="16" font-weight="800" font-family="Arial, sans-serif">PDF</text>
        }
        @case ('image') {
          <rect x="5" y="7" width="62" height="58" rx="7" fill="#365b78" />
          <path fill="#7fc4dc" d="M5 7h62v34H5z" />
          <circle cx="53" cy="20" r="7" fill="#ffd568" />
          <path fill="#30546e" d="m5 55 18-23 13 13 9-9 22 24v5H5z" />
          <path fill="#e67562" d="m5 61 25-18 13 10 9-6 15 12v6H5z" />
        }
        @case ('video') {
          <rect x="5" y="7" width="62" height="58" rx="7" fill="#202640" />
          <path fill="#ec466a" d="M5 7h62v13H5zm0 45h62v13H5z" />
          <circle cx="36" cy="36" r="16" fill="#ffffff2e" stroke="#fff" stroke-width="2" />
          <path fill="#fff" d="m32 27 13 9-13 9z" />
        }
        @default {
          <path fill="#74a9ee" stroke="#5f94dc" d="M12 4h39l12 12v52H12z" />
          <path fill="#a8caf6" d="M51 4v13h12z" />
          <path stroke="#fff" stroke-width="3" d="M23 30h29M23 39h29M23 48h21" />
        }
      }
    </svg>
  `,
  styles: [`
    :host { display: inline-grid; place-items: center; line-height: 0; }
    .file-visual { display: block; max-width: 100%; height: auto; filter: drop-shadow(0 3px 4px rgb(53 61 79 / 12%)); }
  `]
})
export class FileTypeIcon {
  @Input({ required: true }) kind: FileVisualKind = 'file';
  @Input() size = 72;
  @Input() label = 'Dosya';
}
