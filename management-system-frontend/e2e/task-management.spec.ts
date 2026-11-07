import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

const adminEmail = process.env['E2E_ADMIN_EMAIL']!;
const adminPassword = process.env['E2E_ADMIN_PASSWORD']!;

async function logIn(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/login');
  await page.getByLabel('Email Address').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: /log in/i }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
}

async function logOut(page: Page): Promise<void> {
  await page.getByRole('button', { name: /log out/i }).click();
  await expect(page).toHaveURL(/\/login$/);
}

async function deleteTaskThroughApi(request: APIRequestContext, taskId: string): Promise<void> {
  const login = await request.post('/api/auth/login', {
    data: { email: adminEmail, password: adminPassword },
  });
  if (!login.ok()) return;

  const auth = (await login.json()) as { accessToken: string };
  await request.delete(`/api/tasks/${taskId}`, {
    headers: { Authorization: `Bearer ${auth.accessToken}` },
  });
}

test('user registers, admin assigns a task, assignee records progress, and admin deletes it', async ({ page, request }) => {
  test.setTimeout(120_000);

  const runId = `${Date.now()}-${Math.floor(Math.random() * 1_000_000)}`;
  const assigneeName = `E2E Assignee ${runId}`;
  const assigneeEmail = `e2e-${runId}@example.test`;
  const assigneePassword = `E2e-${runId}-Pass!`;
  const initialTitle = `E2E Task ${runId}`;
  const updatedTitle = `${initialTitle} Updated`;
  const progressSummary = `E2E progress summary ${runId}`;
  let taskId: string | undefined;

  try {
    await page.goto('/register');
    await page.getByLabel('Full Name').fill(assigneeName);
    await page.getByLabel('Email Address').fill(assigneeEmail);
    await page.getByLabel('Password', { exact: true }).fill(assigneePassword);
    await page.getByLabel('Confirm Password').fill(assigneePassword);
    await page.getByRole('button', { name: /create account/i }).click();
    await expect(page).toHaveURL(/\/dashboard$/);
    await expect(page.getByText(`Welcome, ${assigneeName}`)).toBeVisible();
    await logOut(page);

    await logIn(page, adminEmail, adminPassword);
    await page.goto('/tasks');
    await expect(page.getByRole('heading', { name: 'All Tasks' })).toBeVisible();
    await page.getByRole('button', { name: /new task/i }).click();

    const createDialog = page.getByRole('dialog');
    await createDialog.getByLabel('Task Title').fill(initialTitle);
    await createDialog.locator('#taskDescription').fill('Created by the browser end-to-end test.');
    await createDialog.getByLabel('Priority').selectOption({ label: 'High' });
    await createDialog.getByLabel('Assignee').selectOption({ label: assigneeName });

    const createResponsePromise = page.waitForResponse((response) =>
      response.url().includes('/api/tasks') && response.request().method() === 'POST',
    );
    await createDialog.getByRole('button', { name: 'Create Task' }).click();
    const createResponse = await createResponsePromise;
    expect(createResponse.status()).toBe(201);
    taskId = ((await createResponse.json()) as { id: string }).id;

    let taskRow = page.getByRole('row').filter({ hasText: initialTitle });
    await expect(taskRow).toBeVisible();
    await page.getByPlaceholder('Search by task title or description').fill(initialTitle);
    await page.getByRole('button', { name: 'Apply' }).click();
    await expect(taskRow).toBeVisible();

    await taskRow.getByRole('button', { name: 'Edit task' }).click();
    const editDialog = page.getByRole('dialog');
    await editDialog.getByLabel('Task Title').fill(updatedTitle);
    await editDialog.getByLabel('Priority').selectOption({ label: 'Low' });
    await editDialog.getByLabel('Status').selectOption({ label: 'In Progress' });
    await editDialog.getByRole('button', { name: 'Save Changes' }).click();

    taskRow = page.getByRole('row').filter({ hasText: updatedTitle });
    await expect(taskRow).toBeVisible();
    await expect(taskRow).toContainText('In Progress');
    await expect(taskRow).toContainText('Low');

    await logOut(page);
    await logIn(page, assigneeEmail, assigneePassword);
    await page.goto('/tasks');
    taskRow = page.getByRole('row').filter({ hasText: updatedTitle });
    await expect(taskRow).toBeVisible();
    await taskRow.getByRole('button', { name: 'Edit task' }).click();

    const assigneeDialog = page.getByRole('dialog');
    await assigneeDialog.getByLabel('Status').selectOption({ label: 'Completed' });
    await assigneeDialog.getByLabel('Update Summary').fill(progressSummary);
    await assigneeDialog.getByRole('button', { name: 'Save Changes' }).click();
    await expect(taskRow).toContainText('Completed');

    await taskRow.getByRole('button', { name: 'View task activity' }).click();
    const activityDialog = page.getByRole('dialog', { name: updatedTitle });
    await expect(activityDialog).toContainText('Updated Status');
    await expect(activityDialog).toContainText('In Progress');
    await expect(activityDialog).toContainText('Completed');
    await expect(activityDialog.locator('.activity-summary')).toContainText(progressSummary);
    await expect(activityDialog).toContainText(assigneeName);
    await activityDialog.locator('footer.modal-actions').getByRole('button', { name: 'Close' }).click();

    await logOut(page);
    await logIn(page, adminEmail, adminPassword);
    await page.goto('/tasks');
    taskRow = page.getByRole('row').filter({ hasText: updatedTitle });
    await expect(taskRow).toBeVisible();
    await taskRow.getByRole('button', { name: 'Delete task' }).click();
    const deleteDialog = page.getByRole('alertdialog');
    await deleteDialog.getByRole('button', { name: 'Delete Task' }).click();
    await expect(page.getByRole('row').filter({ hasText: updatedTitle })).toHaveCount(0);

    await logOut(page);
    await page.goto('/tasks');
    await expect(page).toHaveURL(/\/login$/);
  } finally {
    if (taskId) await deleteTaskThroughApi(request, taskId);
  }
});
