import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CurrentUser } from '../models/api.models';
import { AuthService } from './auth.service';
import { SignalrService } from '../services/signalr.service';
import { TokenStore } from './token.store';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly authService = inject(AuthService);
  private readonly tokenStore = inject(TokenStore);
  private readonly signalr = inject(SignalrService);
  private readonly router = inject(Router);

  private readonly currentUserSignal = signal<CurrentUser | null>(null);
  private readonly initializedSignal = signal(false);
  private initializePromise: Promise<void> | null = null;

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly initialized = this.initializedSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);
  readonly permissions = computed(() => this.currentUserSignal()?.permissions ?? []);
  readonly role = computed(() => this.currentUserSignal()?.role ?? null);

  hasPermission(permission: string): boolean {
    return this.permissions().includes(permission);
  }

  hasRole(role: string): boolean {
    return this.role() === role;
  }

  /**
   * Restores the session on application startup. The in-memory access token is
   * lost on a full page reload, so we call POST /auth/refresh directly (the
   * httpOnly cookie is sent automatically) which returns a fresh access token
   * AND the current user in one round-trip. No initial 401 is produced.
   */
  async initialize(): Promise<void> {
    if (this.initializedSignal()) {
      return;
    }
    // Share a single in-flight restore so the app bootstrap and the route
    // guards (which both call initialize()) never fire duplicate requests.
    this.initializePromise ??= this.restoreSession().finally(() => {
      this.initializedSignal.set(true);
      this.initializePromise = null;
    });
    return this.initializePromise;
  }

  private async restoreSession(): Promise<void> {
    try {
      const response = await this.authService.refresh();
      this.tokenStore.setAccessToken(response.accessToken);
      this.setSession(response.user);
      void this.signalr.start();
    } catch {
      this.clearSession();
    }
  }

  async login(email: string, password: string): Promise<void> {
    const response = await this.authService.login({ email, password });
    this.tokenStore.setAccessToken(response.accessToken);
    this.setSession(response.user);
    void this.signalr.start();
  }

  async logout(): Promise<void> {
    try {
      await this.authService.logout();
    } finally {
      this.clearSession();
      void this.router.navigate(['/login']);
    }
  }

  setSession(user: CurrentUser): void {
    this.tokenStore.resetSessionExpired();
    this.currentUserSignal.set(user);
  }

  /** Called by the auth interceptor when a refresh ultimately fails. */
  handleSessionExpired(): void {
    this.clearSession();
    void this.router.navigate(['/login']);
  }

  private clearSession(): void {
    this.tokenStore.clear();
    this.currentUserSignal.set(null);
    void this.signalr.stop();
  }
}