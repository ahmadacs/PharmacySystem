import { DatePipe } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';

/**
 * Formats an ISO-8601 timestamp (the API always returns UTC) in the pharmacy's
 * display timezone: Saudi Arabia (Asia/Riyadh, fixed UTC+3). Every timestamp
 * shown anywhere in the UI goes through this pipe so the display is consistent
 * regardless of the reviewer's local timezone. Storage stays UTC.
 */
@Pipe({ name: 'riyadhDate', standalone: true })
export class RiyadhDatePipe implements PipeTransform {
  private readonly datePipe = new DatePipe('en-US');

  transform(
    value: string | Date | null | undefined,
    format: string = 'medium',
    timezone: string = '+0300'
  ): string | null {
    if (value == null || value === '') return null;
    return this.datePipe.transform(value, format, timezone);
  }
}