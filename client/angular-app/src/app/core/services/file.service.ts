import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface FileAttachmentDto {
  id: string;
  entityType: string;
  entityId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  blobPath: string;
  createdAt: string;
  url: string | null;
}

@Injectable({ providedIn: 'root' })
export class FileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/files`;

  upload(entityType: 'Medicine' | 'Prescription', entityId: string, file: File): Promise<FileAttachmentDto> {
    const form = new FormData();
    form.append('file', file, file.name);
    return firstValueFrom(this.http.post<FileAttachmentDto>(`${this.baseUrl}/${entityType}/${entityId}`, form));
  }

  list(entityType: string, entityId: string): Promise<FileAttachmentDto[]> {
    return firstValueFrom(this.http.get<FileAttachmentDto[]>(`${this.baseUrl}/${entityType}/${entityId}/list`));
  }

  downloadUrl(fileId: string): string {
    return `${this.baseUrl}/${fileId}/download`;
  }
}
