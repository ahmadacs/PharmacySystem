import { ComponentType } from '@angular/cdk/portal';
import { WritableSignal } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { MatDialog, MatDialogConfig, MatDialogRef } from '@angular/material/dialog';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { AuthStore } from '../auth/auth.store';
import { ToastService } from '../services/toast.service';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../shared/components/confirm-dialog/confirm-dialog.component';

/**
 * Defense-in-depth for dialogs: trigger buttons are already hidden without
 * the permission (`*appHasPermission`), but `dialog.open` calls must not rely
 * on that alone — a hidden button can still be invoked from the console.
 * Pass to `openForResult`/`confirmAndMutate`, or check directly with
 * `requireDialogPermission` before a raw `dialog.open`.
 */
export interface DialogPermissionGuard {
  authStore: AuthStore;
  toast: ToastService;
  translate: TranslateService;
  /** At least one must be held (mirrors route guards accepting alternatives). */
  permissions: string[];
}

/** Shows an "access denied" toast and returns false when access is denied. */
export function requireDialogPermission(guard: DialogPermissionGuard): boolean {
  if (guard.permissions.some((permission) => guard.authStore.hasPermission(permission))) {
    return true;
  }
  guard.toast.show(guard.translate.instant('status.forbiddenTitle'), 'error');
  return false;
}

/**
 * Opens a dialog and invokes `onResult` only when it closes with a truthy
 * result. Replaces the 8x repeated
 * `dialog.open(...).afterClosed().subscribe((x) => { if (x) refresh(); })`
 * blocks across the list screens. Fire-and-forget opens (details dialogs)
 * keep using `MatDialog.open` directly.
 */
export function openForResult<TComponent, TData, TResult>(
  dialog: MatDialog,
  component: ComponentType<TComponent>,
  config: MatDialogConfig<TData>,
  onResult: (result: TResult) => void,
  guard?: DialogPermissionGuard,
): MatDialogRef<TComponent, TResult> | null {
  if (guard && !requireDialogPermission(guard)) {
    return null;
  }
  const ref = dialog.open<TComponent, TData, TResult>(component, config);
  ref.afterClosed().subscribe((result: TResult | undefined) => {
    if (result) {
      onResult(result);
    }
  });
  return ref;
}

/**
 * Confirm dialog + service call + success toast + follow-up, in one step.
 * Replaces the 3x repeated confirm blocks (medicines delete, users
 * activate/deactivate, prescription cancel). Errors are toasted by the
 * global error interceptor, hence the empty catch — same as before.
 */
export async function confirmAndMutate(
  dialog: MatDialog,
  toast: ToastService,
  data: ConfirmDialogData,
  action: () => Promise<unknown>,
  successMessage: string,
  onSuccess?: () => void,
  guard?: DialogPermissionGuard,
): Promise<void> {
  if (guard && !requireDialogPermission(guard)) {
    return;
  }
  const confirmed = await firstValueFrom(
    dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, { data })
      .afterClosed(),
  );
  if (!confirmed) {
    return;
  }
  try {
    await action();
    toast.show(successMessage, 'success');
    onSuccess?.();
  } catch {
    // error toast already shown by the error interceptor
  }
}

/**
 * Standard async-submit lifecycle: re-entrancy guard, `submitting` flag,
 * awaited work, awaited success continuation, interceptor-driven errors.
 * Callers keep their own validation preamble (it differs per form);
 * everything inside the old `try` moves into `action`, everything that ran
 * after it (toast / close / navigate) moves into `onSuccess`.
 */
export async function runSubmit<T>(
  submitting: WritableSignal<boolean>,
  action: () => Promise<T>,
  onSuccess: (result: T) => void | Promise<void>,
): Promise<void> {
  if (submitting()) {
    return;
  }
  submitting.set(true);
  try {
    const result = await action();
    await onSuccess(result);
  } catch {
    // error toast already shown by the error interceptor
  } finally {
    submitting.set(false);
  }
}

export async function runFormSubmit<T>(
  form: AbstractControl,
  submitting: WritableSignal<boolean>,
  action: () => Promise<T>,
  onSuccess: (result: T) => void | Promise<void>,
  onInvalid?: () => void,
): Promise<void> {
  if (form.invalid) {
    form.markAllAsTouched();
    onInvalid?.();
    return;
  }
  await runSubmit(submitting, action, onSuccess);
}
