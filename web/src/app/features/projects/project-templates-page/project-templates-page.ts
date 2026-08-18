import { CommonModule, Location } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Icon, IconName } from '../../../shared/icon/icon';
import { ToastService } from '../../../shared/toast/toast.service';
import { TemplateApiService } from '../data/template-api.service';
import { TemplateDto } from '../data/template-api.models';
import { BackendProjectType } from '../data/project-api.models';
import { AuthService } from '../../../shared/auth/auth.service';

const TYPE_LABELS: Record<BackendProjectType, string> = {
  Simple: 'Basit',
  MultiUnit: 'Çoklu Birimli',
  FeasibilityBased: 'Fizibiliteye Bağlı'
};

const TYPE_BADGE_CLASSES: Record<BackendProjectType, string> = {
  Simple: 'tpl-badge--simple',
  MultiUnit: 'tpl-badge--multi',
  FeasibilityBased: 'tpl-badge--feasibility'
};

const TYPE_CARD_CLASSES: Record<BackendProjectType, string> = {
  Simple: 'tpl-card--simple',
  MultiUnit: 'tpl-card--multi',
  FeasibilityBased: 'tpl-card--feasibility'
};

const TYPE_DESCRIPTIONS: Record<BackendProjectType, string> = {
  Simple: 'Tek ekipli ve doğrusal ilerleyen projeler için temel yapı.',
  MultiUnit: 'Birden fazla departmanın birlikte çalıştığı projeler için.',
  FeasibilityBased: 'Karar ve fizibilite adımları içeren projeler için.'
};

type TemplateTypeFilter = 'All' | BackendProjectType;

interface RailItem {
  icon: IconName;
  label: string;
  active?: boolean;
}

@Component({
  selector: 'app-project-templates-page',
  standalone: true,
  imports: [CommonModule, FormsModule, Icon],
  templateUrl: './project-templates-page.html',
  styleUrl: './project-templates-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectTemplatesPage implements OnInit {
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly templateApi = inject(TemplateApiService);
  private readonly toastService = inject(ToastService);
  private readonly authService = inject(AuthService);

  readonly currentUserName = computed(() => (this.authService.currentUser()?.displayName ?? ''));
  readonly canManageProjects = computed(() => this.authService.hasAnyRole(['Admin', 'ProjectManager']));

  readonly railCompact = signal(false);
  readonly profileOpen = signal(false);
  readonly searchQuery = signal('');
  readonly selectedType = signal<TemplateTypeFilter>('All');

  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly templates = signal<TemplateDto[]>([]);
  readonly deletingId = signal<string | null>(null);
  readonly pendingDeleteId = signal<string | null>(null);

  readonly filteredTemplates = computed(() => {
    const query = this.searchQuery().trim().toLocaleLowerCase('tr-TR');
    const selectedType = this.selectedType();

    return this.templates().filter((template) => {
      const matchesType = selectedType === 'All' || template.applicableProjectType === selectedType;
      const matchesQuery =
        !query ||
        template.name.toLocaleLowerCase('tr-TR').includes(query) ||
        template.fields.some((field) => field.label.toLocaleLowerCase('tr-TR').includes(query));

      return matchesType && matchesQuery;
    });
  });

  readonly customFieldTotal = computed(() =>
    this.templates().reduce((total, template) => total + this.customFieldCount(template), 0)
  );

  readonly requiredFieldTotal = computed(() =>
    this.templates().reduce((total, template) => total + this.requiredFieldCount(template), 0)
  );

  readonly typeCoverageCount = computed(
    () => new Set(this.templates().map((template) => template.applicableProjectType)).size
  );

  readonly hasActiveFilters = computed(
    () => Boolean(this.searchQuery().trim()) || this.selectedType() !== 'All'
  );

  readonly pendingDeleteTemplate = computed(() =>
    this.templates().find((template) => template.id === this.pendingDeleteId()) ?? null
  );

  readonly typeFilters: ReadonlyArray<{ value: TemplateTypeFilter; label: string }> = [
    { value: 'All', label: 'Tümü' },
    { value: 'Simple', label: 'Basit' },
    { value: 'MultiUnit', label: 'Çoklu Birimli' },
    { value: 'FeasibilityBased', label: 'Fizibiliteye Bağlı' }
  ];

  readonly primaryRail: RailItem[] = [
    { icon: 'dashboard', label: 'Uygulamalar' },
    { icon: 'user', label: 'Profil' },
    { icon: 'ai', label: 'Destek' }
  ];

  readonly moduleRail: RailItem[] = [
    { icon: 'projects', label: 'Portföy' },
    { icon: 'calendar', label: 'Takvim' },
    { icon: 'tasks', label: 'Görevler' },
    { icon: 'share', label: 'İş Akışları' },
    { icon: 'feasibility', label: 'Dokümanlar' },
    { icon: 'check', label: 'Onaylar' },
    { icon: 'users', label: 'Ekipler' },
    { icon: 'layers', label: 'Varlıklar' },
    { icon: 'building', label: 'Organizasyon' },
    { icon: 'wallet', label: 'Bütçe' },
    { icon: 'apps', label: 'Projeler', active: true },
    { icon: 'dashboard', label: 'Raporlar' }
  ];

  readonly utilityRail: RailItem[] = [
    { icon: 'projects', label: 'Arşiv' },
    { icon: 'feasibility', label: 'Şablonlar' },
    { icon: 'check', label: 'Kontroller' },
    { icon: 'sliders', label: 'Ayarlar' }
  ];

  async ngOnInit(): Promise<void> {
    await this.loadTemplates();
  }

  retryLoad(): void {
    this.loadTemplates();
  }

  typeLabel(type: BackendProjectType): string {
    return TYPE_LABELS[type];
  }

  typeBadgeClass(type: BackendProjectType): string {
    return TYPE_BADGE_CLASSES[type];
  }

  typeCardClass(type: BackendProjectType): string {
    return TYPE_CARD_CLASSES[type];
  }

  typeDescription(type: BackendProjectType): string {
    return TYPE_DESCRIPTIONS[type];
  }

  customFieldCount(template: TemplateDto): number {
    return template.fields.filter((field) => field.kind === 'Custom').length;
  }

  requiredFieldCount(template: TemplateDto): number {
    return template.fields.filter((field) => field.kind !== 'Section' && field.isRequired && field.isActive).length;
  }

  previewFields(template: TemplateDto): TemplateDto['fields'] {
    return template.fields
      .filter((field) => field.kind !== 'Section' && field.isActive)
      .sort((left, right) => left.sortOrder - right.sortOrder)
      .slice(0, 3);
  }

  remainingFieldCount(template: TemplateDto): number {
    const activeFieldCount = template.fields.filter((field) => field.kind !== 'Section' && field.isActive).length;
    return Math.max(activeFieldCount - 3, 0);
  }

  selectType(type: TemplateTypeFilter): void {
    this.selectedType.set(type);
  }

  clearFilters(): void {
    this.searchQuery.set('');
    this.selectedType.set('All');
  }

  createTemplate(): void {
    this.router.navigate(['/projects/templates/new']);
  }

  editTemplate(id: string): void {
    this.router.navigate(['/projects/templates', id]);
  }

  requestDelete(id: string): void {
    this.pendingDeleteId.set(id);
  }

  cancelDelete(): void {
    this.pendingDeleteId.set(null);
  }

  async confirmDelete(): Promise<void> {
    const id = this.pendingDeleteId();
    if (!id || this.deletingId()) {
      return;
    }

    this.deletingId.set(id);
    try {
      await this.templateApi.remove(id);
      this.templates.update((list) => list.filter((template) => template.id !== id));
      this.toastService.success('Şablon silindi.');
      this.pendingDeleteId.set(null);
    } catch (error) {
      this.toastService.error(error instanceof Error ? error.message : 'Şablon silinemedi.');
    } finally {
      this.deletingId.set(null);
    }
  }

  goToProjects(): void {
    this.router.navigate(['/projects']);
  }

  goBack(): void {
    this.location.back();
  }

  goToDepartments(): void {
    this.router.navigate(['/organization']);
  }

  goToTemplates(): void {
    this.router.navigate(['/projects/templates']);
  }

  logout(): void {
    this.authService.logout();
  }

  private async loadTemplates(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      this.templates.set(await this.templateApi.list());
    } catch (error) {
      this.loadError.set(error instanceof Error ? error.message : 'Şablonlar yüklenemedi.');
    } finally {
      this.loading.set(false);
    }
  }
}
