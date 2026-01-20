# BudgetTracker

## Overview

BudgetTracker is an Aspire-based application with ASP.NET Core Identity integration for user authentication and management.

## Features

- **ASP.NET Core Identity** for user authentication
- **PostgreSQL** database with data volume persistence via Aspire
- **JWT-based authentication** compatible with React SPAs
- **Refresh token** support for secure token rotation
- **CORS configuration** for frontend integration

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Docker (for PostgreSQL via Aspire)

### Running the Application

1. Navigate to the AppHost project:
   ```bash
   cd BudgetTrackerApp/BudgetTrackerApp.AppHost
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Access the Aspire dashboard at the URL shown in the console output.

4. The API service will be available at the port assigned by Aspire.

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

## Security

- Passwords require: 6+ characters, uppercase, lowercase, and digit
- JWT tokens expire after 60 minutes (configurable)
- Refresh tokens expire after 7 days
- Account lockout after 5 failed login attempts

For more details, see [IDENTITY_SETUP.md](IDENTITY_SETUP.md).
