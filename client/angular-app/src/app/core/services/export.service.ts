import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ExportService {
  private readonly http = inject(HttpClient);

  async export(entityType: 'medicines' | 'inventory' | 'prescriptions' | 'dispensing', format: 'excel' | 'pdf', id?: string): Promise<void> {
    let url = `${environment.apiUrl}/exports/${entityType}?format=${format}`;
    if (id) url += `&id=${encodeURIComponent(id)}`;
    const blob = await firstValueFrom(this.http.get(url, { responseType: 'blob' }));
    const ext = format === 'pdf' ? 'pdf' : 'xlsx';
    const contentType = format === 'pdf' ? 'application/pdf' : 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
    this.download(blob, `${entityType}.${ext}`, contentType);
  }

  private download(blob: Blob, fileName: string, contentType: string) {
    const url = URL.createObjectURL(new Blob([blob], { type: contentType }));
    const a = document.createElement('a');
    a.href = url; a.download = fileName; a.click();
    URL.revokeObjectURL(url);
  }
}
