import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FileService } from './file.service';

@Injectable({ providedIn: 'root' })
export class ExportService {
  private readonly http = inject(HttpClient);
  private readonly files = inject(FileService);

  async export(entityType: 'medicines' | 'inventory' | 'prescriptions' | 'dispensing', format: 'excel' | 'pdf', id?: string): Promise<void> {
    let url = `${environment.apiUrl}/exports/${entityType}?format=${format}`;
    if (id) url += `&id=${encodeURIComponent(id)}`;
    const blob = await firstValueFrom(this.http.get(url, { responseType: 'blob' }));
    const ext = format === 'pdf' ? 'pdf' : 'xlsx';
    const contentType = format === 'pdf' ? 'application/pdf' : 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
    this.files.downloadBlobAsFile(blob, `${entityType}.${ext}`, contentType);
  }
}
