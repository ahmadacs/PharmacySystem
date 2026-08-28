import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { TokenStore } from '../auth/token.store';
import { NotificationDto } from '../models/api.models';

/**
 * Owns the SignalR connection to the notifications hub. The JWT access token is
 * passed as the "access_token" query string (WebSockets cannot send headers);
 * the backend only accepts it for /hubs paths.
 *
 * Started after login / silent refresh, stopped on logout. Restarting with a
 * fresh token (e.g. after a silent refresh) re-establishes the connection.
 */
@Injectable({ providedIn: 'root' })
export class SignalrService {
  private readonly tokenStore = inject(TokenStore);

  private connection: HubConnection | null = null;
  private connectedWithToken: string | null = null;

  private readonly latestNotificationSignal = signal<NotificationDto | null>(null);
  readonly latestNotification = this.latestNotificationSignal.asReadonly();

  async start(): Promise<void> {
    const token = this.tokenStore.accessToken();
    if (!token) {
      return;
    }

    if (
      this.connection &&
      this.connectedWithToken === token &&
      this.connection.state === HubConnectionState.Connected
    ) {
      return;
    }

    await this.stop();
    this.connectedWithToken = token;

    const hubUrl = new URL('/hubs/notifications', environment.apiUrl).toString();

    const connection = new HubConnectionBuilder()
      .withUrl(`${hubUrl}?access_token=${encodeURIComponent(token)}`)
      .withAutomaticReconnect()
      .build();

    connection.on('notification', (notification: NotificationDto) => {
      this.latestNotificationSignal.set(notification);
    });

    this.connection = connection;
    await connection.start();
  }

  async stop(): Promise<void> {
    const current = this.connection;
    this.connection = null;
    this.connectedWithToken = null;
    if (current) {
      await current.stop();
    }
  }
}