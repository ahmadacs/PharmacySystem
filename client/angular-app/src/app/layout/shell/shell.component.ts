import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatListItem, MatNavList } from '@angular/material/list';
import { MatMenu, MatMenuItem, MatMenuTrigger } from '@angular/material/menu';
import { MatSidenav, MatSidenavContainer, MatSidenavContent } from '@angular/material/sidenav';
import { MatToolbar } from '@angular/material/toolbar';
import { MatDialog } from '@angular/material/dialog';
import { RouterLink, RouterOutlet, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Permissions } from '../../core/constants/permissions';
import { AuthStore } from '../../core/auth/auth.store';
import { ThemeService } from '../../core/services/theme.service';
import { LocalizationService } from '../../core/services/localization.service';
import { ToastService } from '../../core/services/toast.service';
import { ChangePasswordDialogComponent } from './change-password-dialog/change-password-dialog.component';
import { NotificationBellComponent } from './notification-bell/notification-bell.component';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  permissions: string[];
}

const NAV_ITEMS: NavItem[] = [
  { label: 'shell.dashboard', icon: 'dashboard', route: '/dashboard', permissions: [Permissions.MedicinesView] },
  { label: 'shell.medicines', icon: 'medication', route: '/medicines', permissions: [Permissions.MedicinesView] },
  { label: 'shell.inventory', icon: 'inventory_2', route: '/inventory', permissions: [Permissions.InventoryView] },
  {
    label: 'shell.prescriptions',
    icon: 'description',
    route: '/prescriptions',
    permissions: [Permissions.PrescriptionsView, Permissions.PrescriptionsManageOwn]
  },
  { label: 'shell.dispensing', icon: 'local_pharmacy', route: '/dispensing', permissions: [Permissions.DispensingView] },
  { label: 'shell.users', icon: 'group', route: '/users', permissions: [Permissions.UsersManage] },
  { label: 'shell.auditLog', icon: 'receipt_long', route: '/audit-log', permissions: [Permissions.AuditLogView] }
];

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    MatSidenav,
    MatSidenavContainer,
    MatSidenavContent,
    MatToolbar,
    MatIconButton,
    MatIcon,
    MatNavList,
    MatListItem,
    MatMenu,
    MatMenuItem,
    MatMenuTrigger,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    TranslatePipe,
    NotificationBellComponent
  ],
  templateUrl: './shell.component.html',
  styleUrls: ['./shell.component.scss']
})
export class ShellComponent {
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly dialog = inject(MatDialog);

  protected readonly authStore = inject(AuthStore);
  protected readonly themeService = inject(ThemeService);
  protected readonly localizationService = inject(LocalizationService);
  protected readonly toast = inject(ToastService);

  private readonly isHandsetSignal = toSignal(
    this.breakpointObserver.observe([Breakpoints.Handset]),
    { initialValue: { matches: false, breakpoints: {} } }
  );
  protected readonly isHandset = computed(() => this.isHandsetSignal()?.matches ?? false);
  protected readonly sidenavOpened = signal(true);

  protected readonly navItems = computed(() =>
    NAV_ITEMS.filter((item) => item.permissions.some((p) => this.authStore.hasPermission(p)))
  );
  protected readonly displayName = computed(
    () => this.authStore.currentUser()?.fullName ?? this.authStore.currentUser()?.email ?? ''
  );

  constructor() {
    effect(() => {
      if (this.isHandset()) {
        this.sidenavOpened.set(false);
      } else {
        this.sidenavOpened.set(true);
      }
    });
  }

  toggleSidenav(): void {
    this.sidenavOpened.update((value) => !value);
  }

  onNavigate(): void {
    if (this.isHandset()) {
      this.sidenavOpened.set(false);
    }
  }

  async logout(): Promise<void> {
    await this.authStore.logout();
    this.toast.show('Signed out.', 'info');
  }

  openChangePassword(): void {
    this.dialog.open(ChangePasswordDialogComponent, {
      data: { email: this.authStore.currentUser()?.email }
    });
  }
}