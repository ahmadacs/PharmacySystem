import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { Component, inject, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';
import {
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from '@angular/material/dialog';
import { MatProgressBar } from '@angular/material/progress-bar';
import { PrescriptionListItemDto } from '../../../core/models/api.models';
import { PrescriptionsService } from '../../prescriptions/prescriptions.service';

@Component({
  selector: 'app-dispense-picker-dialog',
  standalone: true,
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MatButton,
    MatProgressBar,
    DatePipe,
    TranslatePipe
  ],
  templateUrl: './dispense-picker-dialog.component.html',
  styleUrl: './dispense-picker-dialog.component.scss'
})
export class DispensePickerDialogComponent {
  private readonly prescriptionsService = inject(PrescriptionsService);
  private readonly dialogRef = inject(MatDialogRef<DispensePickerDialogComponent>);

  protected readonly candidates = signal<PrescriptionListItemDto[]>([]);
  protected readonly loading = signal(true);

  constructor() {
    void this.loadCandidates();
  }

  private async loadCandidates(): Promise<void> {
    try {
      const statuses = ['Pending', 'PartiallyDispensed'] as const;
      const pages = await Promise.all(
        statuses.map((status) =>
          this.prescriptionsService.list({ page: 1, pageSize: 50, status })
        )
      );
      const merged = pages.flatMap((page) => page.items);
      const seen = new Set<string>();
      this.candidates.set(
        merged.filter((item) => (seen.has(item.id) ? false : (seen.add(item.id), true)))
      );
    } finally {
      this.loading.set(false);
    }
  }

  select(candidate: PrescriptionListItemDto): void {
    this.dialogRef.close(candidate.id);
  }
}