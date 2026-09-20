import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdjustInventoryRequest, InventoryAdjustmentDto, ReceiveInventoryRequest } from '../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/inventory`;

  adjust(request: AdjustInventoryRequest): Promise<InventoryAdjustmentDto> {
    return firstValueFrom(
      this.http.post<InventoryAdjustmentDto>(`${this.baseUrl}/adjustments`, request)
    );
  }

  receive(request: ReceiveInventoryRequest): Promise<InventoryAdjustmentDto> {
    return firstValueFrom(
      this.http.post<InventoryAdjustmentDto>(`${this.baseUrl}/receive`, request)
    );
  }
}