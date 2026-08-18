import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

export type IconName =
  | 'dashboard'
  | 'projects'
  | 'tasks'
  | 'feasibility'
  | 'ai'
  | 'logout'
  | 'plus'
  | 'search'
  | 'chevron-down'
  | 'chevron-right'
  | 'edit'
  | 'trash'
  | 'check'
  | 'x'
  | 'user'
  | 'arrow-left'
  | 'arrow-right'
  | 'clock'
  | 'alert'
  | 'grip'
  | 'bot'
  | 'sparkles'
  | 'building'
  | 'calendar'
  | 'wallet'
  | 'users'
  | 'layers'
  | 'menu'
  | 'help'
  | 'mail'
  | 'bell'
  | 'expand'
  | 'share'
  | 'apps'
  | 'sliders'
  | 'file-text'
  | 'paperclip'
  | 'hash'
  | 'send'
  | 'download'
  | 'comment'
  | 'panel-left-close'
  | 'panel-right-close'
  | 'panel-left-open'
  | 'panel-right-open'
  | 'table'
  | 'type'
  | 'paragraph'
  | 'list'
  | 'form'
  | 'checkbox'
  | 'toggle'
  | 'image'
  | 'signature'
  | 'eye'
  | 'more'
  | 'upload';

/**
 * Lucide ikon setinden (MIT lisanslı) uyarlanmış, tek renkli (currentColor) SVG ikonlar.
 * Harici bir ikon paketine bağımlılık eklemeden tutarlı bir modern görünüm sağlar.
 */
@Component({
  selector: 'app-icon',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg
      [attr.width]="size"
      [attr.height]="size"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
      class="cwa-icon"
    >
      @switch (name) {
        @case ('dashboard') {
          <rect x="3" y="3" width="7" height="9" rx="1.5" /><rect x="14" y="3" width="7" height="5" rx="1.5" />
          <rect x="14" y="12" width="7" height="9" rx="1.5" /><rect x="3" y="16" width="7" height="5" rx="1.5" />
        }
        @case ('projects') {
          <path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V7Z" />
        }
        @case ('tasks') {
          <rect x="3" y="4" width="7" height="7" rx="1.5" /><rect x="14" y="4" width="7" height="4" rx="1.5" />
          <rect x="14" y="12" width="7" height="4" rx="1.5" /><rect x="3" y="14" width="7" height="7" rx="1.5" />
        }
        @case ('feasibility') {
          <rect x="4" y="2" width="16" height="20" rx="2" />
          <path d="M8 6h8M8 10h3M13 10h3M8 14h3M13 14h3M8 18h3" />
        }
        @case ('ai') {
          <path d="M12 3v3M12 18v3M4.2 4.2l2.1 2.1M17.7 17.7l2.1 2.1M3 12h3M18 12h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1" />
          <circle cx="12" cy="12" r="3.2" />
        }
        @case ('sparkles') {
          <path d="M12 3l1.6 4.4L18 9l-4.4 1.6L12 15l-1.6-4.4L6 9l4.4-1.6L12 3Z" />
          <path d="M19 15l.8 2.2L22 18l-2.2.8L19 21l-.8-2.2L16 18l2.2-.8L19 15Z" />
        }
        @case ('logout') {
          <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
          <path d="M16 17l5-5-5-5M21 12H9" />
        }
        @case ('plus') {
          <path d="M12 5v14M5 12h14" />
        }
        @case ('search') {
          <circle cx="11" cy="11" r="7" /><path d="M21 21l-4.3-4.3" />
        }
        @case ('chevron-down') {
          <path d="M6 9l6 6 6-6" />
        }
        @case ('chevron-right') {
          <path d="M9 6l6 6-6 6" />
        }
        @case ('edit') {
          <path d="M12 20h9" /><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5Z" />
        }
        @case ('trash') {
          <path d="M3 6h18M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2m3 0-1 14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2L4 6h16Z" />
        }
        @case ('check') {
          <path d="M20 6L9 17l-5-5" />
        }
        @case ('x') {
          <path d="M18 6L6 18M6 6l12 12" />
        }
        @case ('user') {
          <circle cx="12" cy="8" r="4" /><path d="M4 21c0-4 4-6 8-6s8 2 8 6" />
        }
        @case ('arrow-left') {
          <path d="M19 12H5M12 19l-7-7 7-7" />
        }
        @case ('arrow-right') {
          <path d="M5 12h14M12 5l7 7-7 7" />
        }
        @case ('clock') {
          <circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 3" />
        }
        @case ('alert') {
          <path d="M12 9v4M12 17h.01" />
          <path d="M10.3 3.6 1.8 18a1.6 1.6 0 0 0 1.4 2.4h17.6a1.6 1.6 0 0 0 1.4-2.4L13.7 3.6a1.6 1.6 0 0 0-2.8 0Z" />
        }
        @case ('grip') {
          <circle cx="9" cy="6" r="1" /><circle cx="9" cy="12" r="1" /><circle cx="9" cy="18" r="1" />
          <circle cx="15" cy="6" r="1" /><circle cx="15" cy="12" r="1" /><circle cx="15" cy="18" r="1" />
        }
        @case ('bot') {
          <rect x="4" y="9" width="16" height="10" rx="2" /><path d="M12 5v4M9 3.5h6" />
          <path d="M9 13.5h.01M15 13.5h.01" />
        }
        @case ('building') {
          <rect x="4" y="3" width="16" height="18" rx="1.5" />
          <path d="M9 8h1M14 8h1M9 12h1M14 12h1M9 16h1M14 16h1" />
        }
        @case ('calendar') {
          <rect x="3" y="4" width="18" height="18" rx="2" /><path d="M16 2v4M8 2v4M3 10h18" />
        }
        @case ('wallet') {
          <path d="M3 7a2 2 0 0 1 2-2h13a1 1 0 0 1 1 1v3" />
          <rect x="3" y="7" width="18" height="13" rx="2" /><path d="M17 13h.01" />
        }
        @case ('users') {
          <circle cx="9" cy="7" r="3.2" /><path d="M2.5 20c0-3.3 3-5 6.5-5s6.5 1.7 6.5 5" />
          <path d="M16.5 5a3.2 3.2 0 0 1 0 6.2M21.5 20c0-2.8-2-4.3-4.3-4.8" />
        }
        @case ('layers') {
          <path d="M12 2l9 5-9 5-9-5 9-5Z" /><path d="M3 12l9 5 9-5M3 16.5l9 5 9-5" />
        }
        @case ('menu') {
          <path d="M4 6h16M4 12h16M4 18h16" />
        }
        @case ('help') {
          <circle cx="12" cy="12" r="9" />
          <path d="M9.5 9.3a2.5 2.5 0 0 1 4.9.7c0 1.6-2.4 2-2.4 3.6" /><path d="M12 17.2h.01" />
        }
        @case ('mail') {
          <rect x="3" y="5" width="18" height="14" rx="2" /><path d="M3 7l9 6 9-6" />
        }
        @case ('bell') {
          <path d="M6 9a6 6 0 0 1 12 0c0 3.2 1 5 2 6H4c1-1 2-2.8 2-6Z" /><path d="M10 19a2 2 0 0 0 4 0" />
        }
        @case ('expand') {
          <path d="M9 3H5a2 2 0 0 0-2 2v4M15 3h4a2 2 0 0 1 2 2v4M9 21H5a2 2 0 0 1-2-2v-4M15 21h4a2 2 0 0 0 2-2v-4" />
        }
        @case ('share') {
          <circle cx="18" cy="5" r="2.5" /><circle cx="6" cy="12" r="2.5" /><circle cx="18" cy="19" r="2.5" />
          <path d="m8.2 10.8 7.6-4.5M8.2 13.2l7.6 4.5" />
        }
        @case ('apps') {
          <rect x="4" y="4" width="5" height="5" rx="1" /><rect x="15" y="4" width="5" height="5" rx="1" />
          <rect x="4" y="15" width="5" height="5" rx="1" /><rect x="15" y="15" width="5" height="5" rx="1" />
        }
        @case ('sliders') {
          <path d="M4 7h10M18 7h2M4 17h2M10 17h10" /><circle cx="16" cy="7" r="2" /><circle cx="8" cy="17" r="2" />
        }
        @case ('file-text') {
          <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z" />
          <path d="M14 2v6h6M8 13h8M8 17h6" />
        }
        @case ('paperclip') {
          <path d="m21.4 11.6-8.9 8.9a6 6 0 0 1-8.5-8.5l9.2-9.2a4 4 0 0 1 5.7 5.7l-9.2 9.2a2 2 0 0 1-2.8-2.8l8.5-8.5" />
        }
        @case ('hash') {
          <path d="M5 9h14M4 15h14M10 3 8 21M16 3l-2 18" />
        }
        @case ('send') {
          <path d="m22 2-7 20-4-9-9-4Z" /><path d="M22 2 11 13" />
        }
        @case ('download') {
          <path d="M12 3v12M7 10l5 5 5-5M5 21h14" />
        }
        @case ('upload') {
          <path d="M12 15V3M7 8l5-5 5 5M5 21h14" />
        }
        @case ('comment') {
          <path d="M21 15a4 4 0 0 1-4 4H8l-5 3V7a4 4 0 0 1 4-4h10a4 4 0 0 1 4 4Z" />
        }
        @case ('panel-left-close') {
          <rect x="3" y="3" width="18" height="18" rx="2" /><path d="M9 3v18M15 8l-4 4 4 4" />
        }
        @case ('panel-right-close') {
          <rect x="3" y="3" width="18" height="18" rx="2" /><path d="M15 3v18M9 8l4 4-4 4" />
        }
        @case ('panel-left-open') {
          <rect x="3" y="3" width="18" height="18" rx="2" /><path d="M9 3v18M12 8l4 4-4 4" />
        }
        @case ('panel-right-open') {
          <rect x="3" y="3" width="18" height="18" rx="2" /><path d="M15 3v18M12 8l-4 4 4 4" />
        }
        @case ('table') {
          <rect x="3" y="4" width="18" height="16" rx="1.5" /><path d="M3 9h18M8 4v16M15 4v16" />
        }
        @case ('type') {
          <path d="M5 5h14M12 5v14M9 19h6" />
        }
        @case ('paragraph') {
          <path d="M15 5H9a4 4 0 0 0 0 8h3M12 5v14M16 5v14" />
        }
        @case ('list') {
          <path d="M9 6h11M9 12h11M9 18h11M4 6h.01M4 12h.01M4 18h.01" />
        }
        @case ('form') {
          <rect x="3" y="4" width="18" height="16" rx="2" /><path d="M7 9h4M7 14h2M14 9h3M12 14h5" />
        }
        @case ('checkbox') {
          <rect x="3" y="3" width="18" height="18" rx="2" /><path d="m7 12 3 3 7-7" />
        }
        @case ('toggle') {
          <rect x="2" y="7" width="20" height="10" rx="5" /><circle cx="16" cy="12" r="3" />
        }
        @case ('image') {
          <rect x="3" y="4" width="18" height="16" rx="2" /><circle cx="9" cy="9" r="2" /><path d="m4 17 5-5 4 4 2-2 5 5" />
        }
        @case ('signature') {
          <path d="M4 17c2-5 3-8 5-8 3 0-2 8 1 8 2 0 3-5 5-5 2 0-1 5 1 5 1 0 2-2 4-2" /><path d="M4 21h16" />
        }
        @case ('eye') {
          <path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6S2 12 2 12Z" /><circle cx="12" cy="12" r="3" />
        }
        @case ('more') {
          <circle cx="5" cy="12" r="1" fill="currentColor" stroke="none" /><circle cx="12" cy="12" r="1" fill="currentColor" stroke="none" /><circle cx="19" cy="12" r="1" fill="currentColor" stroke="none" />
        }
      }
    </svg>
  `,
  styles: [
    `
      :host {
        display: inline-flex;
        line-height: 0;
      }
      .cwa-icon {
        display: block;
        flex-shrink: 0;
      }
    `
  ]
})
export class Icon {
  @Input({ required: true }) name!: IconName;
  @Input() size = 18;
}
