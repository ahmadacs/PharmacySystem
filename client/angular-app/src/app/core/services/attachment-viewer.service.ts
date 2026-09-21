import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { FileEntityType } from './file.service';
import { AttachmentViewerDialogComponent } from '../../shared/components/attachment-viewer/attachment-viewer-dialog.component';

/**
 * Single entry point for viewing entity attachments (Medicine / Prescription /
 * Batch / InventoryAdjustment). Opens a lazy dialog that shows the images
 * themselves — nothing is fetched until the user actually asks to see them.
 */
@Injectable({ providedIn: 'root' })
export class AttachmentViewerService {
  private readonly dialog = inject(MatDialog);

  open(entityType: FileEntityType, entityId: string): void {
    this.dialog.open(AttachmentViewerDialogComponent, {
      width: '640px',
      maxWidth: '95vw',
      data: { entityType, entityId }
    });
  }
}
