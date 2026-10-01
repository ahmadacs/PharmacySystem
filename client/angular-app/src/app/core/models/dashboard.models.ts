export interface DashboardSummaryDto {
  totalMedicines: number;
  totalVariants: number;
  prescriptionsCreatedToday: number;
  dispensedToday: number;
  adjustmentsToday: number;
  lowStock: number;
  expiredBatches: number;
  expiringSoon: number;
  generatedAt: string;
}
