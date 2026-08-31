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
import { environment } from '../../../../environments/environment';
import { Permissions } from '../../../core/constants/permissions';
import { PagedResult, UserDto, UserRole } from '../../../core/models/api.models';
import { ToastService } from '../../../core/services/toast.service';
import { emptyPage } from '../../../core/utils/empty-page';
import { TranslatePipe } from '@ngx-translate/core';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { UsersService } from '../users.service';
import { UserFormDialogComponent } from '../user-form-dialog/user-form-dialog.component';
import { firstValueFrom } from 'rxjs';

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

  protected readonly page = signal(1);
  protected readonly pageSize = signal(10);
  protected readonly search = signal('');
  protected readonly role = signal<string | null>(null);
  protected readonly isActive = signal<boolean | null>(null);
  protected readonly sortBy = signal('email');
  protected readonly sortDir = signal('asc');

  protected readonly searchControl = new FormControl('');
  protected readonly searchValue = toSignal(this.searchControl.valueChanges, { initialValue: '' });

  protected readonly users = httpResource<PagedResult<UserDto>>(
    () => {
      let params = new HttpParams()
        .set('page', this.page())
        .set('pageSize', this.pageSize())
        .set('search', this.search())
        .set('sortBy', this.sortBy())
        .set('sortDir', this.sortDir());
      if (this.role()) params = params.set('role', this.role()!);
      if (this.isActive() !== null) params = params.set('isActive', this.isActive()!);
      return { url: `${environment.apiUrl}/users`, params };
    },
    { defaultValue: emptyPage<UserDto>() }
  );

  protected readonly totalCount = computed(() => this.users.value()?.totalCount ?? 0);

  constructor() {
    effect((onCleanup) => {
      const value = (this.searchValue() ?? '').trim();
      const handle = setTimeout(() => {
        this.search.set(value);
        this.page.set(1);
      }, 300);
      onCleanup(() => clearTimeout(handle));
    });
  }

  onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
  }

  onSort(sort: Sort): void {
    this.sortDir.set(sort.direction === 'asc' ? 'asc' : 'desc');
    this.sortBy.set(sort.active && sort.direction ? sort.active : 'email');
    this.page.set(1);
  }

  private refreshUsers(): void {
    if (this.page() === 1) {
      void this.users.reload();
    } else {
      this.page.set(1);
    }
  }

  openCreate(): void {
    const ref = this.dialog.open(UserFormDialogComponent, { width: '560px' });
    ref.afterClosed().subscribe((created: boolean) => {
      if (created) {
        this.refreshUsers();
      }
    });
  }

  async toggleActive(user: UserDto): Promise<void> {
    const confirmed = await firstValueFrom(
      this.dialog
        .open(ConfirmDialogComponent, {
          data: {
            title: user.isActive ? 'Deactivate user' : 'Activate user',
            message: `${user.isActive ? 'Deactivate' : 'Activate'} ${user.email}?`,
            confirmLabel: user.isActive ? 'Deactivate' : 'Activate',
            danger: user.isActive
          }
        })
        .afterClosed()
    );
    if (!confirmed) return;
    try {
      await this.usersService.setActive(user.id, !user.isActive);
      this.toast.show(user.isActive ? 'User deactivated.' : 'User activated.', 'success');
      this.refreshUsers();
    } catch {
      // error toast already shown by the error interceptor
    }
  }
}