import { HttpInterceptorFn } from '@angular/common/http';
import { readStoredCulture } from '../constants/storage-keys';


export const languageInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.includes('/api/')) {
    return next(req);
  }
  return next(req.clone({ setHeaders: { 'Accept-Language': readStoredCulture() ?? 'en' } }));
};
