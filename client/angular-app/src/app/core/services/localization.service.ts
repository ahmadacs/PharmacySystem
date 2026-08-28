import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type AppLanguage = 'en' | 'ar';

@Injectable({ providedIn: 'root' })
export class LocalizationService {
  private readonly translate = inject(TranslateService);
  private readonly storageKey = 'Abp.Localization.CultureName';

  readonly currentLang = signal<AppLanguage>((localStorage.getItem(this.storageKey) as AppLanguage) ?? 'en');
  readonly isRtl = computed(() => this.currentLang() === 'ar');
  readonly supportedCultures: AppLanguage[] = ['en', 'ar'];

  constructor() {
    this.translate.addLangs(this.supportedCultures);
    this.translate.setFallbackLang('en');
    void this.translate.use(this.currentLang());

    effect(() => {
      const lang = this.currentLang();
      document.documentElement.lang = lang;
      document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
      localStorage.setItem(this.storageKey, lang);
      void this.translate.use(lang);
    });
  }

  toggle(): void {
    this.currentLang.update(v => (v === 'en' ? 'ar' : 'en'));
  }

  instant(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }
}
