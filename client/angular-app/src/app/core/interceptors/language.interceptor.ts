import { HttpInterceptorFn } from '@angular/common/http';
import { readStoredCulture } from '../constants/storage-keys';

/**
 * Sends the current UI language so the backend localizes its own messages
 * (dispense warnings/errors) per request via Accept-Language negotiation.
 * Accept-Language is CORS-safelisted: no preflight, no CORS config change.
 *
 * NOTE: this deliberately does NOT inject LocalizationService. That service
 * wraps TranslateService, whose HTTP loader runs through this interceptor —
 * injecting it here creates a LocalizationService -> TranslateService ->
 * HttpClient -> languageInterceptor cycle that breaks translation loading.
 * The language is read from the same localStorage key instead.
 * Static assets (e.g. /assets/i18n/*.json) are skipped: they need no culture.
 */
export const languageInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.includes('/api/')) {
    return next(req);
  }
  return next(req.clone({ setHeaders: { 'Accept-Language': readStoredCulture() ?? 'en' } }));
};
