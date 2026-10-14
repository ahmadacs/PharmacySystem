import { WritableSignal } from '@angular/core';
import { FileService } from '../services/file.service';
import { ToastService } from '../services/toast.service';

/** Accepted upload extensions (single source of truth for all file inputs). */
export const ACCEPTED_UPLOAD_TYPES = '.pdf,.jpg,.jpeg,.png';

/**
 * Shared file-pick handler. Replaces the 4x copy-pasted
 * `onFileSelected/removeFile` blocks in the form dialogs.
 */
export function handlePickedFile(
  event: Event,
  target: WritableSignal<File | null>,
  fileService: FileService,
  toast: ToastService,
): void {
  const input = event.target as HTMLInputElement;
  if (input.files && input.files.length > 0) {
    const selected = input.files[0];
    const error = fileService.validateUpload(selected);
    if (error) {
      toast.show(error, 'error');
      return;
    }
    target.set(selected);
  }
}

export function clearPickedFile(target: WritableSignal<File | null>): void {
  target.set(null);
}
