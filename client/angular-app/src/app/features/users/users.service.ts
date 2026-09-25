import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ResourceApi } from '../../core/api/crud-api';
import { CreateUserRequest, UserDto } from '../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class UsersService extends ResourceApi<UserDto, CreateUserRequest> {
  constructor() {
    super(inject(HttpClient), `${environment.apiUrl}/users`);
  }

  getRoles(): Promise<string[]> {
    return firstValueFrom(this.http.get<string[]>(`${this.baseUrl}/roles`));
  }

  setActive(id: string, isActive: boolean): Promise<void> {
    return firstValueFrom(
      this.http.patch<void>(`${this.baseUrl}/${id}/active`, { isActive })
    );
  }
}
