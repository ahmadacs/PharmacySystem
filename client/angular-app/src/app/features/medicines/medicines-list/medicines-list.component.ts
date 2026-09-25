import { Component, computed, inject, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
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
import { environment } from '../../../../environments/environment';
import { AuthStore } from '../../../core/auth/auth.store';
import { Permissions } from '../../../core/constants/permissions';
import { CategoryEnum, MedicineForm, MedicineListItemDto } from '../../../core/models/api.models';
import { ExportService } from '../../../core/services/export.service';
import { ToastService } from '../../../core/services/toast.service';
import { confirmAndMutate, openForResult } from '../../../core/utils/dialog-helpers';
import { numericEnumValues } from '../../../core/utils/entity-helpers';
import { pickLocalizedGenericName, pickLocalizedName } from '../../../core/utils/localized-name.utils';
import { createPagedResource, createPagedTable, buildPagedParams, refreshPaged } from '../../../core/utils/paged-table.utils';
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
  protected readonly forms = numericEnumValues(MedicineForm);
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

  protected readonly table = createPagedTable({ defaultSortBy: 'name' });
  protected readonly page = this.table.page;
  protected readonly pageSize = this.table.pageSize;
  protected readonly search = this.table.search;
  protected readonly sortBy = this.table.sortBy;
  protected readonly sortDir = this.table.sortDir;
  protected readonly searchControl = this.table.searchControl;
  protected readonly categoryId = signal<number | null>(null);
  protected readonly form = signal<MedicineForm | null>(null);
  protected readonly isActive = signal<boolean | null>(null);

  protected readonly categories = numericEnumValues(CategoryEnum);

  protected readonly medicines = createPagedResource<MedicineListItemDto>(() => {
    const params = buildPagedParams(this.table, {
      categoryId: this.categoryId() || null,
      form: this.form() || null,
      isActive: this.isActive(),
    });
    return { url: `${environment.apiUrl}/medicines`, params };
  });

  protected readonly totalCount = this.medicines.totalCount;

  getStatusTranslation(isActive: boolean, isControlled: boolean): string {
    if (!isActive) return this.translate.instant('dictionary.status.inactive');
    if (isControlled) return this.translate.instant('dictionary.status.controlled');
    return this.translate.instant('dictionary.status.active');
  }

  displayName(row: MedicineListItemDto): string { return pickLocalizedName(row, this.translate); }
  displayGenericName(row: MedicineListItemDto): string { return pickLocalizedGenericName(row, this.translate); }

  onSortChange(sort: Sort): void {
    this.table.onSortChange(sort);
  }

  onPage(event: PageEvent): void {
    this.table.onPage(event);
  }

  onFilterChange(): void {
    this.table.resetToFirstPage();
  }

  private refreshMedicines(): void {
    refreshPaged(this.table, this.medicines);
  }

  openCreate(): void {
    openForResult(this.dialog, MedicineFormDialogComponent, { width: '640px', data: null }, () => {
      this.refreshMedicines();
    });
  }

  openEdit(medicine: MedicineListItemDto): void {
    openForResult(this.dialog, MedicineFormDialogComponent, { width: '640px', data: medicine }, () => {
      this.refreshMedicines();
    });
  }

  openDetails(medicine: MedicineListItemDto): void {
    this.dialog.open(MedicineDetailsDialogComponent, { width: '720px', data: medicine.id });
  }

  openAddBatch(medicine: MedicineListItemDto): void {
    openForResult(this.dialog, BatchFormDialogComponent, { width: '520px', data: medicine.id }, () => {
      this.refreshMedicines();
    });
  }

  async export(format: 'excel' | 'pdf'): Promise<void> {
    try { await this.exportService.export('medicines', format); this.toast.show(this.translate.instant('medicines.exported', { format }), 'success'); } catch {}
  }

  async deleteMedicine(medicine: MedicineListItemDto): Promise<void> {
    await confirmAndMutate(
      this.dialog,
      this.toast,
      {
        title: this.translate.instant('common.delete'),
        message: this.translate.instant('medicines.deleteConfirm', { name: medicine.name }),
        confirmLabel: this.translate.instant('common.delete'),
        danger: true
      },
      () => this.medicinesService.remove(medicine.id),
      this.translate.instant('medicines.deleted'),
      () => this.refreshMedicines()
    );
  }
}