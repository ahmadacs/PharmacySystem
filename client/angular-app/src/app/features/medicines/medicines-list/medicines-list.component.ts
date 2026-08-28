import { HttpParams, httpResource } from '@angular/common/http';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatSelect, MatOption } from '@angular/material/select';
import { MatSort, MatSortHeader, Sort } from '@angular/material/sort';
import {
  MatTable,
  MatColumnDef,
  MatHeaderCellDef,
  MatCellDef,
  MatHeaderRowDef,
  MatRowDef,
  MatHeaderCell,
  MatCell,
  MatHeaderRow,
  MatRow
} from '@angular/material/table';
import { MatTooltip } from '@angular/material/tooltip';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AuthStore } from '../../../core/auth/auth.store';
import { Permissions } from '../../../core/constants/permissions';
import { CategoryEnum, MedicineForm, MedicineListItemDto, PagedResult } from '../../../core/models/api.models';
import { ExportService } from '../../../core/services/export.service';
import { ToastService } from '../../../core/services/toast.service';
import { emptyPage } from '../../../core/utils/empty-page';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { MedicinesService } from '../medicines.service';
import { BatchFormDialogComponent } from '../batch-form-dialog/batch-form-dialog.component';
import { MedicineDetailsDialogComponent } from '../medicine-details-dialog/medicine-details-dialog.component';
import { MedicineFormDialogComponent } from '../medicine-form-dialog/medicine-form-dialog.component';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';

@Component({
  selector: 'app-medicines-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormField,
    MatInput,
    MatLabel,
    MatSelect,
    MatOption,
    MatButton,
    MatIconButton,
    MatIcon,
    MatTable,
    MatColumnDef,
    MatHeaderCellDef,
    MatCellDef,
    MatHeaderRowDef,
    MatRowDef,
    MatHeaderCell,
    MatCell,
    MatHeaderRow,
    MatRow,
    MatSort,
    MatSortHeader,
    MatPaginator,
    MatProgressBar,
    MatTooltip,
    PageHeaderComponent,
    EmptyStateComponent,
    HasPermissionDirective,
    TranslatePipe,
    EnumTranslatePipe
  ],
  templateUrl: './medicines-list.component.html',
  styleUrls: ['./medicines-list.component.scss']
})
export class MedicinesListComponent {
  private readonly dialog = inject(MatDialog);
  private readonly medicinesService = inject(MedicinesService);
  private readonly toast = inject(ToastService);
  private readonly authStore = inject(AuthStore);
  private readonly exportService = inject(ExportService);
  protected readonly translate = inject(TranslateService);

  protected readonly permissions = Permissions;
  protected readonly medicineForm = MedicineForm;
  protected readonly forms = Object.values(MedicineForm).filter(
    (form): form is MedicineForm => typeof form === 'number'
  );
  protected readonly displayedColumns = computed(() => {
    const columns = ['name', 'category', 'variants', 'stock', 'status'];
    if (
      this.authStore.hasPermission(Permissions.MedicinesUpdate) ||
      this.authStore.hasPermission(Permissions.MedicinesDelete)
    ) {
      columns.push('actions');
    }
    return columns;
  });

  protected readonly page = signal(1);
  protected readonly pageSize = signal(10);
  protected readonly search = signal('');
  protected readonly sortBy = signal('name');
  protected readonly sortDir = signal('asc');
  protected readonly categoryId = signal<number | null>(null);
  protected readonly form = signal<MedicineForm | null>(null);
  protected readonly isActive = signal<boolean | null>(null);

  protected readonly searchControl = new FormControl('');
  protected readonly searchValue = toSignal(this.searchControl.valueChanges, { initialValue: '' });

  protected readonly categories = Object.values(CategoryEnum).filter(v => typeof v === 'number');

  protected readonly medicines = httpResource<PagedResult<MedicineListItemDto>>(
    () => {
      let params = new HttpParams()
        .set('page', this.page())
        .set('pageSize', this.pageSize())
        .set('search', this.search())
        .set('sortBy', this.sortBy())
        .set('sortDir', this.sortDir());
      if (this.categoryId()) params = params.set('categoryId', this.categoryId()!);
      if (this.form()) params = params.set('form', this.form()!);
      if (this.isActive() !== null) params = params.set('isActive', this.isActive()!);
      return { url: `${environment.apiUrl}/medicines`, params };
    },
    { defaultValue: emptyPage<MedicineListItemDto>() }
  );

  protected readonly totalCount = computed(() => this.medicines.value()?.totalCount ?? 0);

  constructor() {
    // categories are now static enum values
    effect((onCleanup) => {
      const value = (this.searchValue() ?? '').trim();
      const handle = setTimeout(() => {
        this.search.set(value);
        this.page.set(1);
      }, 300);
      onCleanup(() => clearTimeout(handle));
    });
  }

  getStatusTranslation(isActive: boolean, isControlled: boolean): string {
    if (!isActive) return this.translate.instant('dictionary.status.inactive');
    if (isControlled) return this.translate.instant('dictionary.status.controlled');
    return this.translate.instant('dictionary.status.active');
  }

  onSortChange(sort: Sort): void {
    if (!sort.active) {
      return;
    }
    this.sortBy.set(sort.active);
    this.sortDir.set(sort.direction === 'asc' ? 'asc' : 'desc');
    this.page.set(1);
  }

  onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
  }

  onFilterChange(): void {
    this.page.set(1);
  }

  private refreshMedicines(): void {
    if (this.page() === 1) {
      void this.medicines.reload();
    } else {
      this.page.set(1);
    }
  }

  openCreate(): void {
    const ref = this.dialog.open(MedicineFormDialogComponent, { width: '640px', data: null });
    ref.afterClosed().subscribe((created: boolean) => {
      if (created) {
        this.refreshMedicines();
      }
    });
  }

  openEdit(medicine: MedicineListItemDto): void {
    const ref = this.dialog.open(MedicineFormDialogComponent, { width: '640px', data: medicine });
    ref.afterClosed().subscribe((updated: boolean) => {
      if (updated) {
        this.refreshMedicines();
      }
    });
  }

  openDetails(medicine: MedicineListItemDto): void {
    this.dialog.open(MedicineDetailsDialogComponent, { width: '720px', data: medicine.id });
  }

  openAddBatch(medicine: MedicineListItemDto): void {
    const ref = this.dialog.open(BatchFormDialogComponent, { width: '520px', data: medicine.id });
    ref.afterClosed().subscribe((added: boolean) => {
      if (added) {
        this.refreshMedicines();
      }
    });
  }

  async export(format: 'excel' | 'pdf'): Promise<void> {
    try { await this.exportService.export('medicines', format); this.toast.show(this.translate.instant('medicines.exported', { format }), 'success'); } catch {}
  }

  async deleteMedicine(medicine: MedicineListItemDto): Promise<void> {
    const confirmed = await firstValueFrom(
      this.dialog
        .open(ConfirmDialogComponent, {
          data: {
            title: this.translate.instant('common.delete'),
            message: this.translate.instant('medicines.deleteConfirm', { name: medicine.name }),
            confirmLabel: this.translate.instant('common.delete'),
            danger: true
          }
        })
        .afterClosed()
    );
    if (!confirmed) {
      return;
    }
    try {
      await this.medicinesService.remove(medicine.id);
      this.toast.show(this.translate.instant('medicines.deleted'), 'success');
      this.refreshMedicines();
    } catch {
      // error toast already shown by the error interceptor
    }
  }
}