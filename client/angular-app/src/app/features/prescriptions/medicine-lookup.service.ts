import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  MedicineDetailsDto,
  MedicineListItemDto,
  MedicineVariantDto,
  PagedResult,
} from '../../core/models/api.models';

/**
 * Caches the medicine list + per-medicine variants so the prescription dialog
 * (and any future caller) does not duplicate the fetch/filter logic.
 * Extracted from PrescriptionFormDialogComponent.
 */
@Injectable({ providedIn: 'root' })
export class MedicineLookupService {
  private readonly http = inject(HttpClient);

  readonly medicines = signal<MedicineListItemDto[]>([]);
  private readonly variantsByMedicine = signal<Record<string, MedicineVariantDto[]>>({});
  private loaded = false;

  async ensureMedicines(): Promise<void> {
    if (this.loaded) {
      return;
    }
    this.loaded = true;
    const params = new HttpParams().set('page', 1).set('pageSize', 200).set('sortBy', 'name').set('sortDir', 'asc');
    try {
      const result = await firstValueFrom(
        this.http.get<PagedResult<MedicineListItemDto>>(`${environment.apiUrl}/medicines`, { params }),
      );
      this.medicines.set(result.items.filter((m) => m.isActive));
    } catch {
      this.loaded = false;
    }
  }

  variantsFor(medicineId: string | null): MedicineVariantDto[] {
    if (!medicineId) {
      return [];
    }
    return this.variantsByMedicine()[medicineId] ?? [];
  }

  hasVariants(medicineId: string): boolean {
    return !!this.variantsByMedicine()[medicineId];
  }

  async ensureVariants(medicineId: string): Promise<MedicineVariantDto[]> {
    const cached = this.variantsByMedicine()[medicineId];
    if (cached) {
      return cached;
    }
    const details = await firstValueFrom(
      this.http.get<MedicineDetailsDto>(`${environment.apiUrl}/medicines/${medicineId}`),
    );
    const variants = details.variants.filter((v) => v.isActive);
    this.variantsByMedicine.update((map) => ({ ...map, [medicineId]: variants }));
    return variants;
  }

  findMedicine(medicineId: string): MedicineListItemDto | undefined {
    return this.medicines().find((med) => med.id === medicineId);
  }

  filterMedicines(search: string): MedicineListItemDto[] {
    const term = search.trim().toLowerCase();
    if (!term) {
      return this.medicines();
    }
    return this.medicines().filter(
      (m) =>
        m.name.toLowerCase().includes(term) ||
        m.nameAr?.toLowerCase().includes(term) ||
        m.genericName.toLowerCase().includes(term) ||
        m.genericNameAr?.toLowerCase().includes(term),
    );
  }
}
