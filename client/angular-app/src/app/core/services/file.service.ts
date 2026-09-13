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

export type FileEntityType = 'Medicine' | 'Prescription' | 'Batch' | 'InventoryAdjustment';

@Injectable({ providedIn: 'root' })
export class FileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/files`;

  readonly maxUploadBytes = 5 * 1024 * 1024;
  readonly allowedUploadTypes: readonly string[] = ['application/pdf', 'image/jpeg', 'image/png'];

  upload(entityType: FileEntityType, entityId: string, file: File): Promise<FileAttachmentDto> {
    const form = new FormData();
    form.append('file', file, file.name);
    return firstValueFrom(this.http.post<FileAttachmentDto>(`${this.baseUrl}/${entityType}/${entityId}`, form));
  }

  list(entityType: FileEntityType, entityId: string): Promise<FileAttachmentDto[]> {
    return firstValueFrom(this.http.get<FileAttachmentDto[]>(`${this.baseUrl}/${entityType}/${entityId}/list`));
  }

  downloadUrl(fileId: string): string {
    return `${this.baseUrl}/${fileId}/download`;
  }

  /** Shared client-side validation (single source of truth for all dialogs). */
  validateUpload(file: File): string | null {
    if (!this.allowedUploadTypes.includes(file.type)) {
      return 'Only PDF, JPG, and PNG files are allowed.';
    }
    if (file.size > this.maxUploadBytes) {
      return 'File size must be less than 5MB.';
    }
    return null;
  }

  fileToBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => {
        const result = reader.result as string;
        resolve(result.split(',')[1] ?? '');
      };
      reader.onerror = reject;
    });
  }
}
