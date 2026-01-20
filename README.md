# BudgetTracker

## Overview

BudgetTracker is an Aspire-based application with ASP.NET Core Identity integration for user authentication and management.

## Features

- **ASP.NET Core Identity** for user authentication
- **PostgreSQL** database with data volume persistence via Aspire
- **JWT-based authentication** compatible with React SPAs
- **Refresh token** support for secure token rotation
- **CORS configuration** for frontend integration
- **Budget tracking** with Excel import support for bank transactions

## Documentation

- [Database ER Diagram](docs/database/ER_DIAGRAM.md) - Complete database schema design
- [Schema Overview](docs/database/SCHEMA_OVERVIEW.md) - Quick reference guide
- [Identity Setup](IDENTITY_SETUP.md) - Authentication and authorization details
- [API Reference](API_REFERENCE.md) - API endpoint documentation

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Docker Desktop (running) - Required for PostgreSQL container
- Git

### Quick Start

1. **Clone the repository** (if you haven't already):
   ```bash
   git clone https://github.com/SimonAdrell/budgettracker.git
   cd budgettracker
   ```

2. **Ensure Docker is running**:
   - Start Docker Desktop on your machine
   - Verify Docker is running: `docker ps`

3. **Navigate to the AppHost project**:
   ```bash
   cd BudgetTrackerApp/BudgetTrackerApp.AppHost
   ```

4. **Run the application**:
   ```bash
   dotnet run
   ```

5. **Access the services**:
   - The console will display URLs for:
     - **Aspire Dashboard**: `http://localhost:15xxx` (port varies)
     - **API Service**: Check dashboard for the assigned port
     - **Web Frontend**: Check dashboard for the assigned port
   
   Example output:
   ```
   info: Aspire.Hosting.DistributedApplication[0]
         Aspire version: 13.1.0
   info: Aspire.Hosting.DistributedApplication[0]
         Distributed application starting.
   info: Aspire.Hosting.DistributedApplication[0]
         Application host directory is: /path/to/BudgetTrackerApp/BudgetTrackerApp.AppHost
   info: Aspire.Hosting.DistributedApplication[0]
         Now listening on: http://localhost:15252
   ```

6. **What happens on startup**:
   - PostgreSQL container starts with data volume for persistence
   - Database `identitydb` is created automatically
   - Entity Framework migrations run automatically
   - API service starts and connects to PostgreSQL
   - Web frontend starts

### Verifying the Setup

1. Open the Aspire Dashboard (URL from console output)
2. Check that all resources show as "Running" (green status):
   - `postgres` - PostgreSQL container
   - `identitydb` - Database
   - `apiservice` - API service
   - `webfrontend` - Web frontend

3. Test the API:
   ```bash
   # Find the API service port from the Aspire dashboard, then:
   curl http://localhost:<api-port>/
   # Expected: "API service is running. Navigate to /weatherforecast to see sample data."
   ```

### Stopping the Application

Press `Ctrl+C` in the terminal where the application is running. This will:
- Stop all services
- Stop the PostgreSQL container
- **Data persists** in the Docker volume for next startup

## API Documentation

For detailed information about the Identity integration, authentication endpoints, and React SPA integration, see [IDENTITY_SETUP.md](IDENTITY_SETUP.md).

### Quick API Reference

- **POST** `/api/auth/register` - Register a new user
- **POST** `/api/auth/login` - Login and get JWT token
- **POST** `/api/auth/refresh` - Refresh access token
- **POST** `/api/auth/logout` - Logout and revoke refresh tokens
- **GET** `/weatherforecast` - Protected endpoint example

## Project Structure

- **BudgetTrackerApp.AppHost** - Aspire orchestration
- **BudgetTrackerApp.ApiService** - Backend API with Identity
- **BudgetTrackerApp.Web** - Blazor frontend
- **BudgetTrackerApp.ServiceDefaults** - Shared Aspire configuration
- **BudgetTrackerApp.Tests** - Integration tests

## Database

PostgreSQL is configured in Aspire with data volume persistence. The database is automatically migrated on startup.

### Database Migrations

#### Automatic Migration (Default)

By default, migrations run automatically when the API service starts:

```csharp
// In Program.cs
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}
```

#### Manual Migration Execution

If you need to run migrations manually (e.g., for troubleshooting or in production):

1. **Install EF Core tools** (if not already installed):
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. **Navigate to the API service project**:
   ```bash
   cd BudgetTrackerApp/BudgetTrackerApp.ApiService
   ```

3. **Ensure the application is running** (in another terminal):
   ```bash
   # In another terminal window
   cd BudgetTrackerApp/BudgetTrackerApp.AppHost
   dotnet run
   ```
   This ensures PostgreSQL is running and connection strings are available.

4. **Apply migrations**:
   ```bash
   dotnet ef database update
   ```
   
   Expected output:
   ```
   Build started...
   Build succeeded.
   Applying migration '20260120114305_InitialIdentitySetup'.
   Done.
   ```

5. **Verify migration status**:
   ```bash
   dotnet ef migrations list
   ```
   
   Expected output:
   ```
   20260120114305_InitialIdentitySetup (Applied)
   ```

#### Creating New Migrations

When you modify the database models:

```bash
cd BudgetTrackerApp/BudgetTrackerApp.ApiService
dotnet ef migrations add YourMigrationName --output-dir Data/Migrations
```

#### Rolling Back Migrations

To revert the last migration:
```bash
dotnet ef migrations remove
```

To revert to a specific migration:
```bash
dotnet ef database update PreviousMigrationName
```

#### Troubleshooting Migrations

**Error: "Unable to create an object of type 'ApplicationDbContext'"**
- Ensure the AppHost is running (PostgreSQL needs to be available)
- Check that connection strings are properly configured

**Error: "A network-related or instance-specific error"**
- Verify PostgreSQL container is running via Aspire dashboard
- Check Docker Desktop is running

**Error: "Login failed for user"**
- Connection string issue; restart the AppHost
- Check Aspire dashboard for the correct connection string

## Security

- Passwords require: 6+ characters, uppercase, lowercase, and digit
- JWT tokens expire after 60 minutes (configurable)
- Refresh tokens expire after 7 days
- Account lockout after 5 failed login attempts

For more details, see [IDENTITY_SETUP.md](IDENTITY_SETUP.md).
