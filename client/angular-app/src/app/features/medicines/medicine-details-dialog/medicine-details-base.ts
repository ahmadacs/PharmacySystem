import { inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { MedicineDetailsDto } from '../../../core/models/api.models';
import { AuthStore } from '../../../core/auth/auth.store';
import { Permissions } from '../../../core/constants/permissions';
import { AttachmentViewerService } from '../../../core/services/attachment-viewer.service';
import { FileEntityType } from '../../../core/services/file.service';
import { pickLocalizedGenericName, pickLocalizedName } from '../../../core/utils/localized-name.utils';

/** Shared logic for both medicine-details dialogs (medicines + inventory). */
export abstract class MedicineDetailsBase {
  protected readonly viewer = inject(AttachmentViewerService);
  protected readonly translate = inject(TranslateService);
  protected readonly authStore = inject(AuthStore);

  displayName(details: MedicineDetailsDto): string {
    return pickLocalizedName(details, this.translate);
  }

  genericDisplay(details: MedicineDetailsDto): string {
    return pickLocalizedGenericName(details, this.translate);
  }

  protected canViewMedicineAttachments(): boolean {
    return this.authStore.hasPermission(Permissions.MedicinesView);
  }

  protected canViewBatchAttachments(): boolean {
    return (
      this.authStore.hasPermission(Permissions.InventoryView) ||
      this.authStore.hasPermission(Permissions.InventoryAdjust)
    );
  }

  protected openImages(entityType: FileEntityType, entityId: string): void {
    this.viewer.open(entityType, entityId);
  }
}
