import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateUserRequest, UserDto } from '../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/users`;

  getRoles(): Promise<string[]> {
    return firstValueFrom(this.http.get<string[]>(`${this.baseUrl}/roles`));
  }

  create(request: CreateUserRequest): Promise<UserDto> {
    return firstValueFrom(this.http.post<UserDto>(this.baseUrl, request));
  }

  setActive(id: string, isActive: boolean): Promise<void> {
    return firstValueFrom(
      this.http.patch<void>(`${this.baseUrl}/${id}/active`, { isActive })
    );
  }
}