import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  ChangePasswordRequest,
  CurrentUser,
  ForgotPasswordRequest,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest
} from '../models/api.models';

/**
 * Auth API client. Endpoint paths are deliberately lowercase
 * (`/api/v1/auth/...`) so that the refresh-token cookie whose Path is
 * `/api/v1/auth` matches case-sensitively. All calls carry credentials so the
 * httpOnly refresh cookie is sent automatically.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  login(request: LoginRequest): Promise<AuthResponse> {
    return firstValueFrom(
      this.http.post<AuthResponse>(`${this.baseUrl}/login`, request, { withCredentials: true })
    );
  }

  register(request: RegisterRequest): Promise<AuthResponse> {
    return firstValueFrom(
      this.http.post<AuthResponse>(`${this.baseUrl}/register`, request, { withCredentials: true })
    );
  }

  refresh(): Promise<AuthResponse> {
    return firstValueFrom(
      this.http.post<AuthResponse>(`${this.baseUrl}/refresh`, null, { withCredentials: true })
    );
  }

  logout(): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${this.baseUrl}/logout`, null, { withCredentials: true })
    );
  }

  me(): Promise<CurrentUser> {
    return firstValueFrom(
      this.http.get<CurrentUser>(`${this.baseUrl}/me`, { withCredentials: true })
    );
  }

  changePassword(request: ChangePasswordRequest): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${this.baseUrl}/change-password`, request, { withCredentials: true })
    );
  }

  forgotPassword(request: ForgotPasswordRequest): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${this.baseUrl}/forgot-password`, request, { withCredentials: true })
    );
  }

  resetPassword(request: ResetPasswordRequest): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${this.baseUrl}/reset-password`, request, { withCredentials: true })
    );
  }
}