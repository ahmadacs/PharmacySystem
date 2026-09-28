import { Directive, HostListener, inject } from '@angular/core';
import { NgControl } from '@angular/forms';

@Directive({
  selector: 'input[appSaudiPhoneDigits], input[appEnglishDigits]',
  standalone: true
})
export class SaudiPhoneDigitsDirective {
  private readonly ngControl = inject(NgControl, { self: true, optional: true });

  @HostListener('keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    if (event.ctrlKey || event.metaKey || event.altKey) {
      return;
    }
    const allowed = [
      'Backspace',
      'Delete',
      'ArrowLeft',
      'ArrowRight',
      'ArrowUp',
      'ArrowDown',
      'Tab',
      'Home',
      'End'
    ];
    if (allowed.includes(event.key)) {
      return;
    }
    if (!/^\d$/.test(event.key)) {
      event.preventDefault();
    }
  }

  @HostListener('input', ['$event'])
  onInput(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    if (!input) {
      return;
    }
    const cleaned = input.value.replace(/\D/g, '');
    if (cleaned !== input.value) {
      this.ngControl?.control?.setValue(cleaned);
    }
  }
}
