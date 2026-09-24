import { Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogTitle
} from '@angular/material/dialog';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatTooltip } from '@angular/material/tooltip';
import {
  FileAttachmentDto,
  FileEntityType,
  FileService,
  ImagePreview
} from '../../../core/services/file.service';

export interface AttachmentViewerData {
  entityType: FileEntityType;
  entityId: string;
}

/** Lazy image viewer: opened via AttachmentViewerService, fetches nothing until opened. */
@Component({
  selector: 'app-attachment-viewer-dialog',
  standalone: true,
  imports: [
    TranslatePipe,
    MatButton,
    MatIconButton,
    MatIcon,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MatProgressBar,
    MatTooltip
  ],
  templateUrl: './attachment-viewer-dialog.component.html',
  styleUrl: './attachment-viewer-dialog.component.scss'
})
export class AttachmentViewerDialogComponent implements OnDestroy {
  private readonly fileService = inject(FileService);
  protected readonly data = inject<AttachmentViewerData>(MAT_DIALOG_DATA);

  protected readonly loading = signal(true);
  protected readonly images = signal<ImagePreview[]>([]);
  protected readonly documents = signal<FileAttachmentDto[]>([]);
  protected readonly totalCount = computed(() => this.images().length + this.documents().length);

  constructor() {
    void this.load();
  }

  ngOnDestroy(): void {
    for (const image of this.images()) {
      URL.revokeObjectURL(image.url);
    }
  }

  protected async download(file: FileAttachmentDto): Promise<void> {
    try {
      const blob = await this.fileService.downloadBlob(file.id);
      this.fileService.downloadBlobAsFile(blob, file.fileName, file.contentType);
    } catch {
      // error toast already shown by the error interceptor
    }
  }

  private async load(): Promise<void> {
    try {
      const media = await this.fileService.loadImagePreviews(this.data.entityType, this.data.entityId);
      this.images.set(media.images);
      this.documents.set(media.documents);
    } finally {
      this.loading.set(false);
    }
  }
}
