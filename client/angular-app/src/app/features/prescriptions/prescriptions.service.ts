import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ResourceApi } from '../../core/api/crud-api';
import {
  CreatePrescriptionRequest,
  PagedResult,
  PatientPrescriptionHistoryDto,
  PrescriptionDetailsDto,
  PrescriptionListItemDto,
  PrescriptionStatus
} from '../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class PrescriptionsService extends ResourceApi<
  PrescriptionDetailsDto,
  CreatePrescriptionRequest
> {
  constructor() {
    super(inject(HttpClient), `${environment.apiUrl}/prescriptions`);
  }

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

  cancel(id: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/${id}/cancel`, null));
  }

  refillItem(prescriptionId: string, itemId: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/${prescriptionId}/items/${itemId}/refill`, null));
  }

  refillItems(prescriptionId: string, itemIds: string[]): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/${prescriptionId}/refill`, { itemIds }));
  }

  patientHistory(patientId: string, lookbackDays = 90): Promise<PatientPrescriptionHistoryDto[]> {
    const params = new HttpParams().set('lookbackDays', lookbackDays);
    return firstValueFrom(
      this.http.get<PatientPrescriptionHistoryDto[]>(
        `${environment.apiUrl}/patients/${patientId}/prescriptions`,
        { params }
      )
    );
  }
}
