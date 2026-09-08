import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DispensePrescriptionResponse, DispenseRequest } from '../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class DispensingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/dispensing`;

  dispense(request: DispenseRequest): Promise<DispensePrescriptionResponse> {
    return firstValueFrom(this.http.post<DispensePrescriptionResponse>(this.baseUrl, request));
  }
}