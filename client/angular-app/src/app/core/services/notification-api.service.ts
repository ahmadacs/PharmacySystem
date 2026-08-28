import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NotificationDto, PagedResult } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class NotificationApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/notifications`;

  list(page = 1, pageSize = 50): Promise<PagedResult<NotificationDto>> {
    return firstValueFrom(
      this.http.get<PagedResult<NotificationDto>>(
        `${this.baseUrl}?page=${page}&pageSize=${pageSize}`,
        { withCredentials: true }
      )
    );
  }

  markRead(id: string): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${this.baseUrl}/${id}/read`, null, { withCredentials: true })
    );
  }

  markAllRead(): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${this.baseUrl}/read-all`, null, { withCredentials: true })
    );
  }
}