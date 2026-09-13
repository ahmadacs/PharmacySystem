import { PrescriptionListItemDto } from './prescriptions.models';

export interface DashboardSummaryDto {
  dispensedToday: number;
  pending: number;
  createdToday: number;
  lowStock: number;
  expiringSoon: number;
  fragmented: number;
  generatedAt: string;
  latestPending: PrescriptionListItemDto[];
  latestFragmented: PrescriptionListItemDto[];
}
