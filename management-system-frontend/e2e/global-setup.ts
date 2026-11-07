import type { FullConfig } from '@playwright/test';

export default function globalSetup(_config: FullConfig): void {
  const required = [
    'E2E_DATABASE_CONNECTION',
    'E2E_DATABASE_NAME',
    'E2E_ADMIN_EMAIL',
    'E2E_ADMIN_PASSWORD',
  ] as const;
  const missing = required.filter((name) => !process.env[name]);
  if (missing.length > 0) {
    throw new Error(`Set the required local end-to-end environment variables: ${missing.join(', ')}.`);
  }

  const databaseName = process.env['E2E_DATABASE_NAME']!;
  const connection = process.env['E2E_DATABASE_CONNECTION']!;
  const databaseInConnection = /(?:Database|Initial Catalog)\s*=\s*([^;]+)/i.exec(connection)?.[1]?.trim();

  if (!/(e2e|test)/i.test(databaseName) || databaseName.toLowerCase() === 'task_management') {
    throw new Error('E2E_DATABASE_NAME must identify a dedicated test database (for example task_management_e2e).');
  }
  if (databaseInConnection?.toLowerCase() !== databaseName.toLowerCase()) {
    throw new Error('E2E_DATABASE_CONNECTION must point to the database named by E2E_DATABASE_NAME.');
  }
  if (process.env['E2E_ADMIN_PASSWORD']!.length < 8 || process.env['E2E_ADMIN_PASSWORD']!.length > 128) {
    throw new Error('E2E_ADMIN_PASSWORD must be between 8 and 128 characters.');
  }
}
