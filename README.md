# SecureHR

An HR management system for employees, departments, payroll, reports and audit logs, built with .NET 10.

- **SecureHR.Api**: ASP.NET Core Web API with JWT authentication and EF Core on SQLite
- **SecureHR.Web**: Blazor Server front end that calls the API

> **Status:** work in progress. See [Known limitations](#known-limitations).

> **Demo project:** all people and data are invented. No sensitive personal data, such as
> government IDs or bank details, is stored.

## Solution layout

```
src/
  SecureHR.Domain/          Entities and enums; repository interfaces that work with entities only
  SecureHR.Contracts/       DTOs shared by the API and the Web app
  SecureHR.Application/     Business services (employees, departments, payroll, reports) and the
                            interfaces they depend on (repositories, current user)
  SecureHR.Infrastructure/  EF Core (SQLite), migrations, audit trail, repositories,
                            password hashing and JWT authentication
  SecureHR.Api/             REST API: controllers, authorization policies, error handling
  SecureHR.Web/             Blazor Server UI; talks to the API over HTTP and references
                            only SecureHR.Contracts
tests/
  SecureHR.Tests/           Unit, service (in-memory SQLite) and API (in-memory server) tests
```

Package versions are managed centrally in `Directory.Packages.props`, and shared build
settings (including warnings as errors) live in `Directory.Build.props`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A trusted HTTPS development certificate (one-time setup):

  ```bash
  dotnet dev-certs https --trust
  ```

You don't need to install or create a database. The SQLite file is created and migrated automatically on first run.

## Running locally

The API and Web app must both be running.

### Visual Studio

1. Open `SecureHR.slnx`.
2. Choose the **API + Web** launch profile from the startup dropdown.
3. Press **F5**.

### Command line

From the repository root, in two terminals:

```bash
dotnet run --project src/SecureHR.Api      # https://localhost:7209
dotnet run --project src/SecureHR.Web      # https://localhost:7210
```

Then open https://localhost:7210.

### Development login

| Username  | Password            | Role     | Can do                                                        |
|-----------|---------------------|----------|---------------------------------------------------------------|
| `admin`   | `AdminPassword123!` | Admin    | Everything, including the audit log                           |
| `hrstaff` | `DemoPassword123!`  | HR       | Manage employees and departments, payroll, salary reports     |
| `viewer`  | `DemoPassword123!`  | ReadOnly | View employees (without salary), departments, headcount       |

These accounts are created on first run from `Seed:AdminPassword` and `Seed:DemoUserPassword` in
`src/SecureHR.Api/appsettings.Development.json`. The demo users are only created in Development.
Every new database gets 22 starting departments. Development also adds 32 demo employees,
each with a job title and an employee number such as `EMP-0001`. Both lists are defined in
`src/SecureHR.Infrastructure/Data/DemoData.cs`, so deleting `securehr.db` and restarting the API
always brings back the same demo data. Add new demo data to that file; records entered through
the app are not copied there. Other environments start with the departments and the `admin`
user only; employees are created in the app.
You stay signed in across tabs and browser restarts until you log out or the token expires
(`Jwt:ExpirationHours`, 24 hours by default).

## Configuration

The API has two settings that must never use development values outside your machine:

| Setting               | Purpose                                   | Development value comes from    |
|-----------------------|-------------------------------------------|---------------------------------|
| `Jwt:Secret`          | Key used to sign JWTs (32+ characters)    | `appsettings.Development.json`  |
| `Seed:AdminPassword`  | Password for the initial `admin` user     | `appsettings.Development.json`  |

In `appsettings.json` (used by every environment) both are empty. In any non-Development
environment, set them with environment variables or a secret store, for example:

```bash
Jwt__Secret=<long random value>
Seed__AdminPassword=<strong password>
```

The development values in `appsettings.Development.json` are public demo values. The API refuses
to start if `Jwt:Secret` is missing or too short, or if the database has no users and
`Seed:AdminPassword` is missing.

The Web app reads the API address from `ApiBaseUrl` in `src/SecureHR.Web/appsettings*.json`.

## Database

- SQLite, stored at `src/SecureHR.Api/securehr.db` (git-ignored).
- Migrations live in `src/SecureHR.Infrastructure/Data/Migrations` and are applied at startup.
- To reset your local data, stop the API and delete `securehr.db*`.

Adding a migration after changing the model:

```bash
dotnet tool install --global dotnet-ef   # once
dotnet ef migrations add <Name> -p src/SecureHR.Infrastructure -s src/SecureHR.Api -o Data/Migrations
```

## API

All endpoints except login require `Authorization: Bearer <token>`.
Access also depends on role (see [Development login](#development-login)); the login endpoint
is limited to 5 attempts per minute per IP address.
`src/SecureHR.Api/SecureHR.Api.http` has ready-to-run requests (Visual Studio / VS Code REST Client).

| Method | Route                                              | Description                          |
|--------|----------------------------------------------------|--------------------------------------|
| POST   | `/api/auth/login`                                  | Get a JWT                            |
| GET    | `/api/employees?page=&pageSize=&sortBy=&descending=` | List employees (`sortBy`: `name`, `department`, `hireDate`) |
| GET    | `/api/employees/{id}`                              | Get an employee                      |
| POST   | `/api/employees`                                   | Create an employee                   |
| PUT    | `/api/employees/{id}`                              | Update an employee                   |
| DELETE | `/api/employees/{id}`                              | Deactivate an employee               |
| GET    | `/api/departments?page=&pageSize=&sortBy=&descending=` | List active departments (`sortBy`: `name`, `employeeCount`) |
| GET    | `/api/departments/{id}`                            | Get a department                     |
| POST   | `/api/departments`                                  | Create a department                  |
| PUT    | `/api/departments/{id}`                            | Rename a department                  |
| DELETE | `/api/departments/{id}`                            | Deactivate a department (refused while it has active employees) |
| GET    | `/api/payroll/{id}`                                | Get a payroll record                 |
| GET    | `/api/payroll/employee/{employeeId}`               | Payroll history for an employee      |
| GET    | `/api/payroll/employee/{employeeId}/latest`        | Latest payroll for an employee       |
| GET    | `/api/payroll/period?periodStart=&periodEnd=`      | Payroll records in a period          |
| GET    | `/api/payroll/summary?periodStart=&periodEnd=`     | Payroll totals for a period          |
| POST   | `/api/payroll/process-batch`                       | Run payroll for one calendar month (409 if already run) |
| GET    | `/api/reports/payroll-summary`                     | Payroll summary report               |
| GET    | `/api/reports/department-payroll-summary`          | Payroll by department                |
| GET    | `/api/reports/headcount`                           | Headcount by department              |
| GET    | `/api/reports/department/{departmentId}/employees` | Active employees in a department     |
| GET    | `/api/reports/salary-distribution`                 | Salary distribution buckets          |
| GET    | `/api/auditlog?from=&to=&entityName=&entityId=&username=&action=` | Audit log, newest first (Admin) |

Date parameters use the `yyyy-MM-dd` format. `pageSize` is limited to 100.

Errors are returned as [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) JSON:
400 for invalid input (with per-field `errors`), 401/403 for authentication and authorization,
404 when something doesn't exist, and 409 for conflicts such as a duplicate email or department name.

## Payroll and audit trail

- **Payroll** runs one calendar month at a time for all active employees. Gross pay is the
  annual salary / 12, prorated by days for employees hired during the month, rounded to cents.
  Employees hired after the month or without a salary are listed as skipped. Each month can
  only be processed once.
- **Audit trail:** every create, update and deactivation of employees, departments, payroll
  records and users is recorded automatically with the user, time and changed fields, in the
  same transaction as the change. Password values are never written to the log, only that
  they changed. Admins can browse and filter the log on the Audit Logs page.

## Tests

```bash
dotnet test
```

- **Unit tests:** password hashing.
- **Service tests:** run against an in-memory SQLite database built from the real migrations,
  covering payroll rules, duplicate checks, deactivation rules and the audit trail
  (including rollback when an audit record can't be written).
- **API tests:** start the real API in memory (`WebApplicationFactory`) to check login, the
  role rules, validation and error responses.

GitHub Actions (`.github/workflows/ci.yml`) builds the solution, checks that no model
change is missing a migration, and runs the tests on every push and pull request to `main`.

## Known limitations

This project is under active development. Known gaps that are being worked on:

- Net pay uses a flat 20% placeholder deduction; there are no real tax or benefit rules yet.
