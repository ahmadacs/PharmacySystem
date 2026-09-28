import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  DispensePrescriptionResponse,
  DispenseRequest,
  DispensingLookupResponse
} from '../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class DispensingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/dispensing`;

  dispense(request: DispenseRequest): Promise<DispensePrescriptionResponse> {
    return firstValueFrom(this.http.post<DispensePrescriptionResponse>(this.baseUrl, request));
  }

  lookup(shortCode: string, phoneNumber: string): Promise<DispensingLookupResponse> {
    const params = new HttpParams()
      .set('shortCode', shortCode.trim().toUpperCase())
      .set('phoneNumber', phoneNumber.trim());
    return firstValueFrom(this.http.get<DispensingLookupResponse>(`${this.baseUrl}/lookup`, { params }));
  }
}