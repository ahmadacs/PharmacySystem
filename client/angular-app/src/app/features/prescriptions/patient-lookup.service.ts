import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface FoundPatient {
  id: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  phoneNumber: string;
}

interface PatientPhoneCheckPayload {
  id?: string;
  firstName?: string | null;
  lastName?: string | null;
  dateOfBirth?: string | null;
  phoneNumber?: string | null;
  exists?: boolean;
}

interface PatientPhoneCheckResponse {
  isFailure?: boolean;
  value?: PatientPhoneCheckPayload | null;
  error?: unknown;
  statusCode?: number;
}

export const SAUDI_PHONE_PATTERN = /^(?:\+9665\d{8}|05\d{8}|5\d{8})$/;

/** Matches the inline hint logic previously buried in the dialog component. */
export function computePhoneHintKey(phone: string): string | null {
  if (!phone || SAUDI_PHONE_PATTERN.test(phone)) {
    return null;
  }
  const normalized = phone.replace(/[\s-]/g, '');
  const digits = normalized.replace(/\D/g, '');
  const fullLength = normalized.startsWith('+') ? 12 : 10;
  if (/^\+?\d+$/.test(normalized) && digits.length < fullLength) {
    return 'dialogs.prescriptionForm.phoneIncompleteHint';
  }
  return 'dialogs.prescriptionForm.phoneInvalidHint';
}

@Injectable({ providedIn: 'root' })
export class PatientLookupService {
  private readonly http = inject(HttpClient);

  isCompletePhone(phone: string): boolean {
    return SAUDI_PHONE_PATTERN.test((phone ?? '').trim());
  }

  /**
   * Returns the found patient, or null when the phone belongs to a new patient
   * (404 / empty payload also map to null — the caller treats both as "new").
   */
  async findByPhone(phone: string): Promise<FoundPatient | null> {
    try {
      const result = await firstValueFrom(
        this.http.get<PatientPhoneCheckResponse>(
          `${environment.apiUrl}/patients/by-phone/${encodeURIComponent(phone)}`,
        ),
      );
      const payload = result?.value ?? (result as PatientPhoneCheckPayload | undefined);
      const data: PatientPhoneCheckPayload | null =
        payload && typeof payload === 'object' ? payload : null;
      const isExisting =
        !result?.isFailure &&
        !!data &&
        (data.exists === true || !!data.firstName || !!data.lastName || !!data.dateOfBirth);
      if (!isExisting || !data) {
        return null;
      }
      return {
        id: data.id ?? '',
        firstName: data.firstName ?? '',
        lastName: data.lastName ?? '',
        dateOfBirth: data.dateOfBirth ?? '',
        phoneNumber: data.phoneNumber ?? phone,
      };
    } catch {
      return null;
    }
  }
}
