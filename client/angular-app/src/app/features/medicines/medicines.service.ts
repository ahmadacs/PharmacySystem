import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CrudApi } from '../../core/api/crud-api';
import {
  CreateBatchRequest,
  CreateMedicineRequest,
  MedicineBatchDto,
  MedicineDetailsDto,
  UpdateMedicineRequest
} from '../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class MedicinesService extends CrudApi<
  MedicineDetailsDto,
  CreateMedicineRequest,
  UpdateMedicineRequest
> {
  constructor() {
    super(inject(HttpClient), `${environment.apiUrl}/medicines`);
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
