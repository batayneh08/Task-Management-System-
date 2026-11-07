import { defineConfig } from '@playwright/test';
import { randomBytes } from 'node:crypto';

const databaseConnection = process.env['E2E_DATABASE_CONNECTION'];
const databaseName = process.env['E2E_DATABASE_NAME'];
const adminEmail = process.env['E2E_ADMIN_EMAIL'];
const adminPassword = process.env['E2E_ADMIN_PASSWORD'];
const e2eConfigured = Boolean(databaseConnection && databaseName && adminEmail && adminPassword);

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 120_000,
  expect: { timeout: 10_000 },
  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
  ],
  outputDir: 'test-results',
  globalSetup: './e2e/global-setup.ts',
  use: {
    baseURL: 'http://127.0.0.1:4200',
    channel: 'msedge',
    actionTimeout: 10_000,
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  webServer: e2eConfigured
    ? [
        {
          command: 'dotnet run --project "../Management System/Management System.csproj" --launch-profile https',
          url: 'https://localhost:7240/swagger/index.html',
          ignoreHTTPSErrors: true,
          timeout: 120_000,
          reuseExistingServer: false,
          env: {
            ASPNETCORE_ENVIRONMENT: 'Development',
            ConnectionStrings__DefaultConnection: databaseConnection!,
            Database__MigrateOnStartup: 'true',
            Jwt__Key: randomBytes(48).toString('base64'),
            BootstrapAdmin__FullName: 'End-to-End Test Administrator',
            BootstrapAdmin__Email: adminEmail!,
            BootstrapAdmin__Password: adminPassword!,
            BootstrapDemoUser__FullName: '',
            BootstrapDemoUser__Email: '',
            BootstrapDemoUser__Password: '',
          },
        },
        {
          command: 'npm start -- --host 127.0.0.1 --port 4200',
          url: 'http://127.0.0.1:4200/login',
          timeout: 120_000,
          reuseExistingServer: false,
        },
      ]
    : undefined,
});
