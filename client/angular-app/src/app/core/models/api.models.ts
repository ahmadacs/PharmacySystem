/**
 * Backwards-compatible barrel. New code should import from the domain files
 * directly (e.g. `core/models/medicines.models`); this file only re-exports
 * so the existing 40+ import sites keep working during the migration.
 */
export * from './common.models';
export * from './auth.models';
export * from './medicines.models';
export * from './prescriptions.models';
export * from './dispensing.models';
export * from './inventory.models';
export * from './users.models';
export * from './audit.models';
export * from './dashboard.models';
export * from './notifications.models';
