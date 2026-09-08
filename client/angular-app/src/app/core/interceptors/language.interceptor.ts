import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { LocalizationService } from '../services/localization.service';

/**
 * Sends the current UI language so the backend localizes its own messages
 * (dispense warnings/errors) per request via Accept-Language negotiation.
 * Accept-Language is CORS-safelisted: no preflight, no CORS config change.
 */
export const languageInterceptor: HttpInterceptorFn = (req, next) => {
  const localization = inject(LocalizationService);
  return next(req.clone({ setHeaders: { 'Accept-Language': localization.currentLang() } }));
};
