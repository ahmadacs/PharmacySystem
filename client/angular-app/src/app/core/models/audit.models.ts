export type AuditAction = 'Created' | 'Updated' | 'Deleted';

export interface AuditChangeDto {
  property: string;
  oldValue: string | null;
  newValue: string | null;
  oldValueDisplay: string | null;
  newValueDisplay: string | null;
}

export interface AuditEntryDto {
  id: string;
  entityName: string;
  entityId: string;
  entityDisplay: string | null;
  action: AuditAction;
  changedBy: string | null;
  changedByName: string | null;
  changedAt: string;
  changes: AuditChangeDto[];
}
