export type AuditAction = 'Created' | 'Updated' | 'Deleted';

export interface AuditChangeDto {
  property: string;
  oldValue: string | null;
  newValue: string | null;
}

export interface AuditEntryDto {
  id: string;
  entityName: string;
  entityId: string;
  action: AuditAction;
  changedBy: string | null;
  changedByName: string | null;
  changedAt: string;
  changes: AuditChangeDto[];
}
