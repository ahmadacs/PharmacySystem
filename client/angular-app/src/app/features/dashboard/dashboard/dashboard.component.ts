import { HttpParams, httpResource } from '@angular/common/http';
import { Component, computed, inject } from '@angular/core';
import { MatCard, MatCardContent, MatCardHeader, MatCardSubtitle, MatCardTitle } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { MatProgressBar } from '@angular/material/progress-bar';
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
import { environment } from '../../../../environments/environment';
import { AuthStore } from '../../../core/auth/auth.store';
import { Permissions } from '../../../core/constants/permissions';
import {
  LowStockDto,
  MedicineListItemDto,
  PagedResult,
  PrescriptionListItemDto,
  PrescriptionStatus
} from '../../../core/models/api.models';
import { emptyPage } from '../../../core/utils/empty-page';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { TranslatePipe } from '@ngx-translate/core';
import { EnumTranslatePipe } from '../../../shared/pipes/enum-translate.pipe';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    MatCard,
    MatCardHeader,
    MatCardTitle,
    MatCardSubtitle,
    MatCardContent,
    MatIcon,
    MatProgressBar,
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
    PageHeaderComponent,
    EmptyStateComponent,
    TranslatePipe,
    EnumTranslatePipe
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent {
  private readonly authStore = inject(AuthStore);

  protected readonly canViewInventory = computed(() =>
    this.authStore.hasPermission(Permissions.InventoryView)
  );

  protected readonly lowStock = httpResource<LowStockDto[]>(
    () =>
      this.authStore.hasPermission(Permissions.InventoryView)
        ? { url: `${environment.apiUrl}/inventory/low-stock` }
        : undefined,
    { defaultValue: [] }
  );

  protected readonly medicines = httpResource<PagedResult<MedicineListItemDto>>(
    () =>
      this.authStore.hasPermission(Permissions.MedicinesView)
        ? {
            url: `${environment.apiUrl}/medicines`,
            params: new HttpParams().set('page', 1).set('pageSize', 1)
          }
        : undefined,
    { defaultValue: emptyPage<MedicineListItemDto>() }
  );

  protected readonly expiring = httpResource<PagedResult<MedicineListItemDto>>(
    () =>
      this.authStore.hasPermission(Permissions.InventoryView)
        ? {
            url: `${environment.apiUrl}/inventory/batches`,
            params: new HttpParams()
              .set('page', 1)
              .set('pageSize', 1)
              .set('expiryStatus', 'ExpiringSoon')
              .set('withinDays', 30)
          }
        : undefined,
    { defaultValue: emptyPage<MedicineListItemDto>() }
  );

  protected readonly pendingPrescriptions = httpResource<PagedResult<PrescriptionListItemDto>>(
    () =>
      this.authStore.hasPermission(Permissions.PrescriptionsView) ||
      this.authStore.hasPermission(Permissions.PrescriptionsManageOwn)
        ? {
            url: `${environment.apiUrl}/prescriptions`,
            params: new HttpParams()
              .set('page', 1)
              .set('pageSize', 5)
              .set('status', 'Pending' satisfies PrescriptionStatus)
              .set('sortBy', 'issuedDate')
              .set('sortDir', 'desc')
          }
        : undefined,
    { defaultValue: emptyPage<PrescriptionListItemDto>() }
  );

  protected readonly medicineCount = computed(() => this.medicines.value()?.totalCount ?? 0);
  protected readonly lowStockCount = computed(() => this.lowStock.value()?.length ?? 0);
  protected readonly expiringCount = computed(() => this.expiring.value()?.totalCount ?? 0);
  protected readonly pendingCount = computed(() => this.pendingPrescriptions.value()?.totalCount ?? 0);

  protected readonly lowStockColumns = ['medicine', 'variant', 'availableQuantity', 'reorderLevel'];
  protected readonly pendingColumns = ['patientName', 'issuedDate', 'status', 'itemCount'];

}