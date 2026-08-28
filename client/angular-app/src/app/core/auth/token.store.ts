import { Injectable, signal } from '@angular/core';

/**
 * Holds the JWT access token in memory only.
 *
 * Security rationale (documented in README):
 *  - The access token is never persisted to localStorage/sessionStorage, so it
 *    cannot be exfiltrated by a XSS payload.
 *  - The refresh token is stored in an httpOnly cookie managed entirely by the
 *    backend, so JavaScript can never read it.
 *  - Consequence: a full page reload loses the access token; the AuthStore
 *    then calls GET /auth/me, whose 401 triggers a silent refresh via the
 *    httpOnly cookie, which restores the session transparently.
 */
@Injectable({ providedIn: 'root' })
export class TokenStore {
  private readonly accessTokenSignal = signal<string | null>(null);
  private readonly sessionExpiredHandledSignal = signal(false);

  readonly accessToken = this.accessTokenSignal.asReadonly();
  readonly sessionExpiredHandled = this.sessionExpiredHandledSignal.asReadonly();

  setAccessToken(token: string): void {
    this.accessTokenSignal.set(token);
  }

  /** Marks that the user was already told their session expired (avoids spamming redirects). */
  markSessionExpired(): void {
    this.sessionExpiredHandledSignal.set(true);
  }

  /** Cleared whenever a fresh session is established (login or silent refresh). */
  resetSessionExpired(): void {
    this.sessionExpiredHandledSignal.set(false);
  }

  clear(): void {
    this.accessTokenSignal.set(null);
  }
}