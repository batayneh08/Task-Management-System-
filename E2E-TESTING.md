# End-to-End Testing

The browser suite exercises the Angular UI, its `/api` proxy, the ASP.NET Core API, EF Core migrations, and PostgreSQL together. It uses Microsoft Edge installed on Windows and does not mock API responses.

## Safety and prerequisites

- .NET 10 SDK, Node.js/npm, Microsoft Edge, and PostgreSQL must be installed.
- The helper creates a **separate, temporary test database** with a unique name containing `task_management_e2e`. Never configure the suite to point at `task_management` or another database containing user data. The test registers an account, creates a task, and deletes that task at the end; the helper removes the temporary database afterward.
- Stop any API or Angular development server already using ports `7240` or `4200`. Playwright starts and stops its own servers.
- Supply the database connection and bootstrap administrator credentials as process environment variables. They are passed to the local API process and are not stored in this repository.

Run the isolated test helper from `management-system-frontend`:

```powershell
.\e2e\run-e2e.ps1
```

The helper asks for the PostgreSQL password without displaying it, creates a uniquely named temporary database, generates test-only administrator credentials in memory, runs the suite, then drops that database. The PostgreSQL account must be allowed to create and drop databases. The test API applies the checked-in EF Core migrations and seeds the administrator on startup.

## Covered browser journey

The automated scenario:

1. Registers a regular user in the UI and verifies the dashboard loads.
2. Logs in as the seeded administrator, creates a high-priority task, and assigns it to that user.
3. Searches for the task, changes its title, priority, and status, and checks the updated values.
4. Logs in as the assignee, moves the task to Completed, and submits an update summary.
5. Opens that task's activity history and verifies the actor, old/new statuses, and summary appear together.
6. Logs in as the administrator, deletes the task, and verifies the protected route redirects to login after logout.

The test also deletes its task in a `finally` cleanup path if an assertion fails. A deletion audit remains by design; recreate the dedicated test database when a clean run is needed.

## Results

Playwright prints the pass/fail summary and writes a detailed HTML report to `management-system-frontend/playwright-report`. On failure, screenshots and traces are saved under `management-system-frontend/test-results`.

**Current result:** **1 Playwright end-to-end test passed**. The full browser journey verified registration and login, dashboard loading, task creation and assignment, search, administrator edits, assignee status update with its summary, activity history, task deletion, logout, and the protected-route redirect. The helper removed its temporary PostgreSQL database after the run. Open the HTML report with `npm run e2e:report`.
