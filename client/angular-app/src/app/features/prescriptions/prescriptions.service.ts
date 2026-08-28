import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreatePrescriptionRequest,
  PagedResult,
  PrescriptionDetailsDto,
  PrescriptionListItemDto,
  PrescriptionStatus
} from '../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class PrescriptionsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/prescriptions`;

  list(params?: {
    page?: number;
    pageSize?: number;
    search?: string;
    status?: PrescriptionStatus;
  }): Promise<PagedResult<PrescriptionListItemDto>> {
    let httpParams = new HttpParams()
      .set('page', params?.page ?? 1)
      .set('pageSize', params?.pageSize ?? 10);
    if (params?.search) httpParams = httpParams.set('search', params.search);
    if (params?.status) httpParams = httpParams.set('status', params.status);
    return firstValueFrom(
      this.http.get<PagedResult<PrescriptionListItemDto>>(this.baseUrl, { params: httpParams })
    );
  }

  get(id: string): Promise<PrescriptionDetailsDto> {
    return firstValueFrom(this.http.get<PrescriptionDetailsDto>(`${this.baseUrl}/${id}`));
  }

  create(request: CreatePrescriptionRequest): Promise<PrescriptionDetailsDto> {
    return firstValueFrom(
      this.http.post<PrescriptionDetailsDto>(this.baseUrl, request)
    );
  }

  cancel(id: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/${id}/cancel`, null));
  }

  refill(id: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/${id}/refill`, null));
  }
}