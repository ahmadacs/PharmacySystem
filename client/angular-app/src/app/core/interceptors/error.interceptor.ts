import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ErrorEnvelope } from '../models/api.models';
import { ToastService } from '../services/toast.service';

function extractMessage(error: HttpErrorResponse): string {
  const envelope = error.error as Partial<ErrorEnvelope> | null;
  if (envelope?.message) {
    return envelope.message;
  }
  if (envelope?.errors) {
    const first = Object.values(envelope.errors)[0];
    if (first?.length) {
      return first[0];
    }
  }
  return error.status === 0
    ? 'Cannot reach the server. Please check your connection.'
    : 'Something went wrong. Please try again.';
}

/**
 * Converts backend HTTP errors into user-facing toasts using the standard
 * error envelope. 401 responses are intentionally skipped here because they
 * are consumed by the auth interceptor (silent refresh / session expiry).
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        return throwError(() => error);
      }
      toast.show(extractMessage(error), 'error');
      return throwError(() => error);
    })
  );
};