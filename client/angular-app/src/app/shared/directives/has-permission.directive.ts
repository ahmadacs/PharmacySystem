import { Directive, TemplateRef, ViewContainerRef, effect, inject, input, signal } from '@angular/core';
import { AuthStore } from '../../core/auth/auth.store';

/**
 * Structural directive for permission-based UI, e.g.
 * `*appHasPermission="'Permissions.Medicines.Create'"`.
 *
 * Note: this only affects the UX; authorization is always enforced server-side.
 */
@Directive({
  selector: '[appHasPermission]',
  standalone: true
})
export class HasPermissionDirective {
  private readonly authStore = inject(AuthStore);
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);

  readonly appHasPermission = input.required<string>();
  private readonly shown = signal(false);

  constructor() {
    effect(() => {
      const has = this.authStore.hasPermission(this.appHasPermission());
      if (has && !this.shown()) {
        this.viewContainer.createEmbeddedView(this.templateRef);
        this.shown.set(true);
      } else if (!has && this.shown()) {
        this.viewContainer.clear();
        this.shown.set(false);
      }
    });
  }
}