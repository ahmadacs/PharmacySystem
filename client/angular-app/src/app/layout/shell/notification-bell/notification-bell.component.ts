import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatBadge } from '@angular/material/badge';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatMenu, MatMenuItem, MatMenuTrigger } from '@angular/material/menu';
import { AuthStore } from '../../../core/auth/auth.store';
import { Permissions } from '../../../core/constants/permissions';
import { NotificationApiService } from '../../../core/services/notification-api.service';
import { SignalrService } from '../../../core/services/signalr.service';
import { ToastService } from '../../../core/services/toast.service';
import { NotificationDto, NotificationType } from '../../../core/models/api.models';
import { RiyadhDatePipe } from '../../../shared/pipes/riyadh-date.pipe';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [MatBadge, MatIconButton, MatIcon, MatMenu, MatMenuItem, MatMenuTrigger, MatButton, RiyadhDatePipe, TranslatePipe],
  templateUrl: './notification-bell.component.html',
  styleUrl: './notification-bell.component.scss'
})
export class NotificationBellComponent {
  private readonly api = inject(NotificationApiService);
  private readonly signalr = inject(SignalrService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly authStore = inject(AuthStore);
  private readonly translate = inject(TranslateService);

  private readonly notificationsSignal = signal<NotificationDto[]>([]);
  private readonly notificationsPanelOpened = signal(false);
  protected readonly notifications = this.notificationsSignal.asReadonly();
  protected readonly unreadCount = computed(() => this.notifications().filter((n) => !n.isRead).length);
  protected readonly showUnreadBadge = computed(() => this.unreadCount() > 0);

  constructor() {
    void this.load();

    effect(() => {
      const latest = this.signalr.latestNotification();
      if (latest) {
        this.notificationsSignal.update((list) => [latest, ...list.filter((n) => n.id !== latest.id)]);
        this.toast.show(`${this.translateTitle(latest)}: ${this.translateMessage(latest)}`, this.toastType(latest.type), 6000);
      }
    });
  }

  async load(): Promise<void> {
    this.notificationsPanelOpened.set(true);
    try {
      const result = await this.api.list(1, 50);
      this.notificationsSignal.set(result.items);
    } catch {
      // The error interceptor already surfaces a toast; keep the menu silent.
    }
  }

  async markRead(notification: NotificationDto): Promise<void> {
    await this.api.markRead(notification.id);
    this.notificationsSignal.update((list) =>
      list.map((n) => (n.id === notification.id ? { ...n, isRead: true } : n))
    );
  }

  async markAllRead(): Promise<void> {
    await this.api.markAllRead();
    this.notificationsSignal.update((list) => list.map((n) => ({ ...n, isRead: true })));
  }

  protected open(notification: NotificationDto): void {
    const route = this.routeFor(notification.type);
    if (route) {
      void this.router.navigate([route]);
    }
    void this.markRead(notification);
  }

  private routeFor(type: NotificationType): string | null {
    switch (type) {
      case 'LowStock':
      case 'NearExpiry':
        return '/inventory';
      case 'PrescriptionCreated':
        return '/prescriptions';
      case 'PrescriptionDispensed':
        return this.authStore.hasPermission(Permissions.DispensingView) ? '/dispensing' : '/prescriptions';
    }
  }

  protected iconFor(type: NotificationType): string {
    switch (type) {
      case 'LowStock':
        return 'warning';
      case 'NearExpiry':
        return 'event';
      case 'PrescriptionCreated':
        return 'description';
      case 'PrescriptionDispensed':
        return 'local_pharmacy';
    }
  }

  private toastType(type: NotificationType): 'success' | 'error' | 'info' {
    switch (type) {
      case 'LowStock':
      case 'NearExpiry':
        return 'error';
      case 'PrescriptionCreated':
        return 'info';
      case 'PrescriptionDispensed':
        return 'success';
    }
  }

  protected translateTitle(notification: NotificationDto): string {
    if (notification.localizationKey) {
      const titleKey = `notifications.${notification.localizationKey}Title`;
      const translated = this.translate.instant(titleKey);
      if (translated !== titleKey) return translated;
    }
    // fallback mapping by type
    const typeTitleMap: Record<string, string> = {
      LowStock: 'notifications.lowStockTitle',
      NearExpiry: 'notifications.nearExpiryTitle',
      PrescriptionCreated: 'notifications.newPrescriptionTitle',
      PrescriptionDispensed: 'notifications.dispensedTitle'
    };
    const fallbackKey = typeTitleMap[notification.type];
    if (fallbackKey) {
      const t = this.translate.instant(fallbackKey);
      if (t !== fallbackKey) return t;
    }
    return notification.title;
  }

  protected translateMessage(notification: NotificationDto): string {
    if (notification.localizationKey) {
      const params = notification.localizationParamsJson ? JSON.parse(notification.localizationParamsJson) : {};
      const key = `notifications.${notification.localizationKey}`;
      const translated = this.translate.instant(key, params);
      if (translated !== key) return translated;
    }
    // fallback: try type-specific message keys
    if (notification.localizationParamsJson) {
      try {
        const params = JSON.parse(notification.localizationParamsJson);
        const typeKeyMap: Record<string, string> = {
          LowStock: 'notifications.lowStock',
          NearExpiry: 'notifications.nearExpiry',
          PrescriptionCreated: 'notifications.newPrescription',
          PrescriptionDispensed: 'notifications.dispensed'
        };
        const k = typeKeyMap[notification.type];
        if (k) {
          const t = this.translate.instant(k, params);
          if (t !== k) return t;
        }
      } catch {}
    }
    return notification.message;
  }
}