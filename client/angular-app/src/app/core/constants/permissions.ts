export const Permissions = {
  MedicinesView: 'Permissions.Medicines.View',
  MedicinesCreate: 'Permissions.Medicines.Create',
  MedicinesUpdate: 'Permissions.Medicines.Update',
  MedicinesDelete: 'Permissions.Medicines.Delete',
  InventoryView: 'Permissions.Inventory.View',
  InventoryAdjust: 'Permissions.Inventory.Adjust',
  PrescriptionsView: 'Permissions.Prescriptions.View',
  PrescriptionsCreate: 'Permissions.Prescriptions.Create',
  PrescriptionsManageOwn: 'Permissions.Prescriptions.ManageOwn',
  PrescriptionsManageAll: 'Permissions.Prescriptions.ManageAll',
  DispensingView: 'Permissions.Dispensing.View',
  DispensingCreate: 'Permissions.Dispensing.Create',
  UsersManage: 'Permissions.Users.Manage',
  AuditLogView: 'Permissions.AuditLog.View'
} as const;

export const Roles = {
  Admin: 'Admin',
  Pharmacist: 'Pharmacist',
  Doctor: 'Doctor'
} as const;