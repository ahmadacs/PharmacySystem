export type NotificationType = 'LowStock' | 'NearExpiry' | 'PrescriptionCreated' | 'PrescriptionDispensed';

export interface NotificationDto {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  data: string | null;
  localizationKey: string | null;
  localizationParamsJson: string | null;
  isRead: boolean;
  createdAt: string;
}
