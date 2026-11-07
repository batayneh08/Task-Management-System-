# Task Management System

Full-stack task management system with an ASP.NET Core 10 REST API and an Angular 20 client. The backend uses C#, Clean Architecture, Entity Framework Core, PostgreSQL, JWT authentication, and Swagger UI. The Angular client uses standalone components, reactive forms, signals, and the Angular Router.

## Backend projects

- `Management System`: API controllers, JWT configuration, Swagger, and application startup.
- `ManagementSystem.Application`: use cases, services, interfaces, and validation.
- `ManagementSystem.Domain`: entities, enums, and request/response DTOs.
- `ManagementSystem.Infrastructure`: PostgreSQL, EF Core, repositories, migrations, password hashing, and development seed data.
- `ManagementSystem.Tests`: unit tests and API integration tests. Integration tests use an in-memory database; the application itself uses PostgreSQL.
- `management-system-frontend`: Angular login, registration, dashboard, and task management UI.

## Requirements

- .NET 10 SDK.
- PostgreSQL running locally or a reachable PostgreSQL server.

## Configure and run locally

Open PowerShell in the repository folder. Set the PostgreSQL connection string, a JWT signing key of at least 32 characters, and the first administrator credentials in the current shell session. These values are intentionally not stored in the repository.

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=task_management;Username=postgres;Password=<your-local-postgres-password>"
$jwtRng = [Security.Cryptography.RandomNumberGenerator]::Create()
$jwtBytes = New-Object byte[] 32
try {
    $jwtRng.GetBytes($jwtBytes)
    $env:Jwt__Key = [Convert]::ToBase64String($jwtBytes)
}
finally {
    $jwtRng.Dispose()
    [Array]::Clear($jwtBytes, 0, $jwtBytes.Length)
}
$env:BootstrapAdmin__FullName = "System Administrator"
$env:BootstrapAdmin__Email = "admin@taskmanagement.local"
$env:BootstrapAdmin__Password = "<choose-a-local-password-of-at-least-8-characters>"
```

An optional regular demo user can be created by setting all three `BootstrapDemoUser__FullName`, `BootstrapDemoUser__Email`, and `BootstrapDemoUser__Password` variables. The seeder never promotes an existing account to Admin. In Development, startup applies pending EF Core migrations. When the task table is empty and a bootstrap Admin is configured, it also creates sample tasks.

Start the API:

```powershell
dotnet run --project "Management System/Management System.csproj" --launch-profile https
```

Open Swagger UI at `https://localhost:7240/swagger`. Register or log in at `/api/auth`, copy the returned access token, select **Authorize**, and enter the token. Swagger sends it as a Bearer token.

## Main API routes

- `POST /api/auth/register` and `POST /api/auth/login`
- `GET /api/tasks` with optional `status`, `priority`, `assignedToId`, and `search` query parameters
- `GET /api/tasks/{id}`, `POST /api/tasks`, `PUT /api/tasks/{id}`, and `DELETE /api/tasks/{id}`
- `GET /api/tasks/{id}/activity`
- `GET /api/dashboard`
- `GET /api/users` and `GET /api/users/{id}` (authenticated account required)

Task, dashboard, and user-list routes require a valid JWT. Public registration creates a User account.

Task permissions: Admins create and delete tasks, assign users, and manage the task title, description, priority, and due date. A regular user can update the status of tasks assigned to them and include an optional update summary. The summary and all changed fields are saved together as one activity event; the original task description stays intact. The API enforces these rules even if a client bypasses the UI.

## Angular frontend

Run the backend first using the `https` launch profile. Then, in a separate terminal:

```powershell
cd management-system-frontend
npm install
npm start -- --host 127.0.0.1 --port 4200
```

Open `http://127.0.0.1:4200`. Run one Angular dev server at a time. During local development, the Angular proxy forwards `/api` calls to `https://localhost:7240`; the proxy accepts the local development certificate, and the UI uses the actual API, not mock data. The current frontend includes login, registration, a live dashboard, task search and filters, task create/edit/delete, assignee selection, and activity history.

## Build and tests

Build the full solution:

```powershell
dotnet build "Management System/Management System.slnx"
```

Run the backend unit and integration tests:

```powershell
dotnet run --project ManagementSystem.Tests/ManagementSystem.Tests.csproj
```

The tests cover registration, login, validation, role authorization, task CRUD, activity history, dashboard data, and Swagger documentation. The test host uses an in-memory database so tests do not modify the local PostgreSQL database.

For browser-based end-to-end tests, see [E2E testing](E2E-TESTING.md). The suite runs Angular against the real ASP.NET Core API and a dedicated PostgreSQL test database.

## AI-assisted development records

- [AI prompt log](AI-PROMPT-LOG.md): a concise record of meaningful project prompts, outcomes, and corrections.
- [AI experience report](AI-EXPERIENCE-REPORT.md): review notes, human decisions, verification evidence, and limitations.
