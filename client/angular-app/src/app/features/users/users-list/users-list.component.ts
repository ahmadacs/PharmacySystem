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
import { environment } from '../../../../environments/environment';
import { Permissions } from '../../../core/constants/permissions';
import { UserDto, UserRole } from '../../../core/models/api.models';
import { ToastService } from '../../../core/services/toast.service';
import { createPagedResource, createPagedTable, buildPagedParams, refreshPaged } from '../../../core/utils/paged-table.utils';
import { confirmAndMutate, openForResult } from '../../../core/utils/dialog-helpers';
import { TranslatePipe } from '@ngx-translate/core';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { UsersService } from '../users.service';
import { UserFormDialogComponent } from '../user-form-dialog/user-form-dialog.component';

const USER_ROLES: UserRole[] = ['Admin', 'Pharmacist', 'Doctor'];

@Component({
  selector: 'app-users-list',
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
    MatPaginator,
    MatProgressBar,
    MatTooltip,
    MatSort,
    MatSortHeader,
    TranslatePipe,
    PageHeaderComponent,
    EmptyStateComponent,
    HasPermissionDirective
  ],
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.scss']
})
export class UsersListComponent {
  private readonly dialog = inject(MatDialog);
  private readonly usersService = inject(UsersService);
  private readonly toast = inject(ToastService);

  protected readonly permissions = Permissions;
  protected readonly roles = USER_ROLES;
  protected readonly displayedColumns = ['email', 'fullName', 'role', 'isActive', 'actions'];

  protected readonly table = createPagedTable({ defaultSortBy: 'email' });
  protected readonly page = this.table.page;
  protected readonly pageSize = this.table.pageSize;
  protected readonly search = this.table.search;
  protected readonly sortBy = this.table.sortBy;
  protected readonly sortDir = this.table.sortDir;
  protected readonly searchControl = this.table.searchControl;
  protected readonly role = signal<string | null>(null);
  protected readonly isActive = signal<boolean | null>(null);

  protected readonly users = createPagedResource<UserDto>(() => {
    const params = buildPagedParams(this.table, {
      role: this.role() || null,
      isActive: this.isActive(),
    });
    return { url: `${environment.apiUrl}/users`, params };
  });

  protected readonly totalCount = this.users.totalCount;

  onPage(event: PageEvent): void {
    this.table.onPage(event);
  }

  onSort(sort: Sort): void {
    this.sortDir.set(sort.direction === 'asc' ? 'asc' : 'desc');
    this.sortBy.set(sort.active && sort.direction ? sort.active : 'email');
    this.table.resetToFirstPage();
  }

  private refreshUsers(): void {
    refreshPaged(this.table, this.users);
  }

  openCreate(): void {
    openForResult(this.dialog, UserFormDialogComponent, { width: '560px' }, () => {
      this.refreshUsers();
    });
  }

  async toggleActive(user: UserDto): Promise<void> {
    await confirmAndMutate(
      this.dialog,
      this.toast,
      {
        title: user.isActive ? 'Deactivate user' : 'Activate user',
        message: `${user.isActive ? 'Deactivate' : 'Activate'} ${user.email}?`,
        confirmLabel: user.isActive ? 'Deactivate' : 'Activate',
        danger: user.isActive
      },
      () => this.usersService.setActive(user.id, !user.isActive),
      user.isActive ? 'User deactivated.' : 'User activated.',
      () => this.refreshUsers()
    );
  }
}