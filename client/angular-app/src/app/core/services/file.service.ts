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

export interface ImagePreview {
  id: string;
  fileName: string;
  contentType: string;
  url: string;
}

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

  /** Authenticated blob fetch (carries the JWT via the auth interceptor) for <img> previews and downloads. */
  downloadBlob(fileId: string): Promise<Blob> {
    return firstValueFrom(this.http.get(`${this.baseUrl}/${fileId}/download`, { responseType: 'blob' }));
  }

  /**
   * Loads image previews for an entity: lists attachments, then fetches each
   * image as an authenticated blob and exposes it as an object URL ready for
   * direct <img [src]> binding (plain <img src> sends no Authorization header).
   * Non-image files are returned separately for download links.
   */
  async loadImagePreviews(
    entityType: FileEntityType,
    entityId: string
  ): Promise<{ images: ImagePreview[]; documents: FileAttachmentDto[] }> {
    const list = await this.list(entityType, entityId).catch(() => [] as FileAttachmentDto[]);
    const images: ImagePreview[] = [];
    const documents = list.filter((f) => !f.contentType.toLowerCase().startsWith('image/'));
    await Promise.all(
      list
        .filter((f) => f.contentType.toLowerCase().startsWith('image/'))
        .map(async (f) => {
          try {
            const blob = await this.downloadBlob(f.id);
            images.push({
              id: f.id,
              fileName: f.fileName,
              contentType: f.contentType,
              url: URL.createObjectURL(new Blob([blob], { type: f.contentType }))
            });
          } catch {
            // per-file failure: skip thumbnail, global interceptor already toasted
          }
        })
    );
    return { images, documents };
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
