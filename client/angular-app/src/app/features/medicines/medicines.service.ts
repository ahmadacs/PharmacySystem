import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateBatchRequest,
  CreateMedicineRequest,
  MedicineBatchDto,
  MedicineDetailsDto,
  UpdateMedicineRequest
} from '../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class MedicinesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/medicines`;

  get(id: string): Promise<MedicineDetailsDto> {
    return firstValueFrom(this.http.get<MedicineDetailsDto>(`${this.baseUrl}/${id}`));
  }

  create(request: CreateMedicineRequest): Promise<MedicineDetailsDto> {
    return firstValueFrom(this.http.post<MedicineDetailsDto>(this.baseUrl, request));
  }

  update(id: string, request: UpdateMedicineRequest): Promise<void> {
    return firstValueFrom(this.http.put<void>(`${this.baseUrl}/${id}`, request));
  }

  remove(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.baseUrl}/${id}`));
  }

  addBatch(medicineId: string, request: CreateBatchRequest): Promise<MedicineBatchDto> {
    return firstValueFrom(
      this.http.post<MedicineBatchDto>(`${this.baseUrl}/${medicineId}/batches`, request)
    );
  }

  removeBatch(batchId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.baseUrl}/batches/${batchId}`));
  }
}