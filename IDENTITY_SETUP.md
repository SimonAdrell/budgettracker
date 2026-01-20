# ASP.NET Identity Integration with PostgreSQL

This document describes the ASP.NET Identity integration with PostgreSQL for the BudgetTracker application, configured to support React SPA authentication.

## Overview

The BudgetTracker application now includes:
- **ASP.NET Core Identity** for user authentication and management
- **PostgreSQL** as the identity data store (configured via Aspire)
- **JWT-based authentication** for React SPA compatibility
- **Refresh token** support for secure token rotation
- **CORS configuration** for React frontend integration

## Architecture

### Components

1. **ApplicationUser**: Custom Identity user model with additional fields (FirstName, LastName, CreatedAt)
2. **RefreshToken**: Model for storing and managing refresh tokens
3. **ApplicationDbContext**: EF Core DbContext for Identity and application data
4. **TokenService**: Service for generating and validating JWT tokens
5. **Authentication Endpoints**: Register, Login, Logout, and Refresh Token APIs

### Database

PostgreSQL is configured in Aspire with data volume persistence:
```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var identityDb = postgres.AddDatabase("identitydb");
```

This ensures that:
- Database data persists between container restarts
- Data is stored in a Docker volume
- Connection strings are automatically managed by Aspire

## Configuration

### JWT Settings (appsettings.json)

```json
{
  "Jwt": {
    "Key": "YourSecureKeyHereMinimum32CharactersLongForProduction!",
    "Issuer": "BudgetTrackerApp",
    "Audience": "BudgetTrackerApp",
    "ExpiryInMinutes": "60"
  }
}
```

**Important for Production**: 
- Store the JWT Key in a secure location (e.g., Azure Key Vault, User Secrets)
- Use a cryptographically secure random key of at least 32 characters
- Never commit production keys to source control

### CORS Configuration

The API is configured to accept requests from common React development ports:
- http://localhost:3000 (Create React App)
- http://localhost:5173 (Vite)
- HTTPS variants of the above

**For Production**: Update the CORS policy to include your production frontend URLs.

## API Endpoints

### 1. Register User
**POST** `/api/auth/register`

Request body:
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123",
  "confirmPassword": "SecurePassword123",
  "firstName": "John",
  "lastName": "Doe"
}
```

Response (200 OK):
```json
{
  "message": "User registered successfully"
}
```

### 2. Login
**POST** `/api/auth/login`

Request body:
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123"
}
```

Response (200 OK):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64-encoded-refresh-token",
  "expiration": "2026-01-20T12:37:09.137Z",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe"
}
```

### 3. Refresh Token
**POST** `/api/auth/refresh`

Request body:
```json
{
  "token": "expired-jwt-token",
  "refreshToken": "current-refresh-token"
}
```

Response (200 OK): Same as login response with new tokens

### 4. Logout
**POST** `/api/auth/logout`

Requires: Authorization header with valid JWT token

Response (200 OK):
```json
{
  "message": "Logged out successfully"
}
```

### 5. Protected Endpoint Example
**GET** `/weatherforecast`

Requires: Authorization header with valid JWT token

Response (200 OK): Weather forecast data

## React SPA Integration

### Install Required Packages
```bash
npm install axios
# or
yarn add axios
```

### Example Authentication Service

```typescript
// authService.ts
import axios from 'axios';

const API_URL = 'http://localhost:5000'; // Adjust to your API URL

interface LoginRequest {
  email: string;
  password: string;
}

interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName?: string;
  lastName?: string;
}

interface AuthResponse {
  token: string;
  refreshToken: string;
  expiration: string;
  email: string;
  firstName?: string;
  lastName?: string;
}

export const authService = {
  register: async (data: RegisterRequest): Promise<void> => {
    const response = await axios.post(`${API_URL}/api/auth/register`, data);
    return response.data;
  },

  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await axios.post(`${API_URL}/api/auth/login`, data);
    const authData = response.data;
    
    // Store tokens
    localStorage.setItem('token', authData.token);
    localStorage.setItem('refreshToken', authData.refreshToken);
    
    return authData;
  },

  logout: async (): Promise<void> => {
    try {
      await axios.post(`${API_URL}/api/auth/logout`, {}, {
        headers: { Authorization: `Bearer ${localStorage.getItem('token')}` }
      });
    } finally {
      localStorage.removeItem('token');
      localStorage.removeItem('refreshToken');
    }
  },

  refreshToken: async (): Promise<AuthResponse> => {
    const token = localStorage.getItem('token');
    const refreshToken = localStorage.getItem('refreshToken');
    
    const response = await axios.post(`${API_URL}/api/auth/refresh`, {
      token,
      refreshToken
    });
    
    const authData = response.data;
    localStorage.setItem('token', authData.token);
    localStorage.setItem('refreshToken', authData.refreshToken);
    
    return authData;
  },

  getToken: (): string | null => {
    return localStorage.getItem('token');
  }
};
```

### Axios Interceptor for Automatic Token Refresh

```typescript
// axiosConfig.ts
import axios from 'axios';
import { authService } from './authService';

const apiClient = axios.create({
  baseURL: 'http://localhost:5000',
});

// Request interceptor to add token
apiClient.interceptors.request.use(
  (config) => {
    const token = authService.getToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor to handle token refresh
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        await authService.refreshToken();
        const token = authService.getToken();
        originalRequest.headers.Authorization = `Bearer ${token}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        // Refresh failed, redirect to login
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);

export default apiClient;
```

### Usage Example

```typescript
// LoginComponent.tsx
import React, { useState } from 'react';
import { authService } from './authService';

const LoginComponent: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await authService.login({ email, password });
      // Redirect to dashboard or home page
      window.location.href = '/dashboard';
    } catch (err) {
      setError('Invalid credentials');
    }
  };

  return (
    <form onSubmit={handleLogin}>
      <input
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        placeholder="Email"
        required
      />
      <input
        type="password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        placeholder="Password"
        required
      />
      {error && <p style={{ color: 'red' }}>{error}</p>}
      <button type="submit">Login</button>
    </form>
  );
};

export default LoginComponent;
```

## Database Migrations

### Automatic Migration on Startup

Migrations are automatically applied on application startup via:
```csharp
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}
```

This means:
- ✅ No manual migration needed for first run
- ✅ Database schema always matches code
- ✅ Safe for development and production

### Manual Migration Commands

#### Prerequisites

1. **Install EF Core Tools** (one-time setup):
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. **Ensure Aspire is running** (for connection strings):
   ```bash
   # In a separate terminal
   cd BudgetTrackerApp/BudgetTrackerApp.AppHost
   dotnet run
   ```
   Keep this running while executing EF commands.

#### View Migration Status

Check which migrations exist and which have been applied:

```bash
cd BudgetTrackerApp/BudgetTrackerApp.ApiService
dotnet ef migrations list
```

Expected output:
```
Build started...
Build succeeded.
20260120114305_InitialIdentitySetup (Applied)
```

#### Apply Migrations Manually

To apply all pending migrations:

```bash
cd BudgetTrackerApp/BudgetTrackerApp.ApiService
dotnet ef database update
```

Expected output when migrations are already applied:
```
Build started...
Build succeeded.
No migrations were applied. The database is already up to date.
Done.
```

Expected output when applying new migrations:
```
Build started...
Build succeeded.
Applying migration '20260120114305_InitialIdentitySetup'.
Done.
```

#### Create a New Migration

When you add or modify database models:

```bash
cd BudgetTrackerApp/BudgetTrackerApp.ApiService
dotnet ef migrations add YourMigrationName --output-dir Data/Migrations
```

Example:
```bash
dotnet ef migrations add AddUserProfileFields --output-dir Data/Migrations
```

This creates three files:
- `YYYYMMDDHHMMSS_YourMigrationName.cs` - Migration operations
- `YYYYMMDDHHMMSS_YourMigrationName.Designer.cs` - Model snapshot
- `ApplicationDbContextModelSnapshot.cs` - Updated (full model)

#### Apply a Specific Migration

To migrate to a specific migration:

```bash
dotnet ef database update MigrationName
```

To migrate to the initial state (remove all migrations):
```bash
dotnet ef database update 0
```

#### Remove the Last Migration

To undo the last migration (only if not applied to database):

```bash
cd BudgetTrackerApp/BudgetTrackerApp.ApiService
dotnet ef migrations remove
```

⚠️ **Warning**: This only works if the migration hasn't been applied to the database yet.

#### Generate SQL Script

To generate a SQL script instead of applying directly:

```bash
# Generate SQL for all migrations
dotnet ef migrations script --output migrations.sql

# Generate SQL for specific range
dotnet ef migrations script FromMigration ToMigration --output migrations.sql

# Generate SQL from a migration to latest
dotnet ef migrations script InitialIdentitySetup --output migrations.sql
```

### Troubleshooting Migrations

#### Error: "Build failed"
**Problem**: Project doesn't compile.
**Solution**: Fix compilation errors first, then retry.

```bash
cd BudgetTrackerApp/BudgetTrackerApp.ApiService
dotnet build
# Fix any errors, then retry migration command
```

#### Error: "Unable to create an object of type 'ApplicationDbContext'"
**Problem**: Connection string not available or invalid.
**Solution**: 
1. Ensure AppHost is running (provides connection string via Aspire)
2. Check `appsettings.json` for any connection string issues

```bash
# Terminal 1: Start Aspire
cd BudgetTrackerApp/BudgetTrackerApp.AppHost
dotnet run

# Terminal 2: Run migrations
cd BudgetTrackerApp/BudgetTrackerApp.ApiService
dotnet ef database update
```

#### Error: "A network-related or instance-specific error occurred"
**Problem**: Cannot connect to PostgreSQL.
**Solution**:
1. Verify Docker Desktop is running
2. Check PostgreSQL container is running in Aspire dashboard
3. Verify connection string is correct

```bash
# Check Docker
docker ps | grep postgres

# If not running, start Aspire
cd BudgetTrackerApp/BudgetTrackerApp.AppHost
dotnet run
```

#### Error: "The table 'AspNetUsers' already exists"
**Problem**: Trying to apply migrations when database already has tables.
**Solution**:
1. Either drop the database and recreate:
   ```bash
   # Connect to postgres container via Aspire dashboard
   # Or use pgAdmin to drop the database
   ```
2. Or mark migrations as applied:
   ```bash
   # Mark all migrations as applied without executing them
   dotnet ef database update --no-build
   ```

#### Migration Applied but Changes Not Reflected
**Problem**: Database not updating despite migration success.
**Solution**:
1. Verify migration contains expected operations:
   ```bash
   # Check the migration file content
   cat Data/Migrations/*_YourMigration.cs
   ```
2. Check the database directly:
   ```bash
   # Use Aspire dashboard to connect to PostgreSQL
   # Or use a database client like pgAdmin or DBeaver
   ```
3. Restart the API service to clear any cached data

### Migration Best Practices

1. **Always create migrations for model changes**: Don't modify the database manually
2. **Test migrations**: Apply to a test database before production
3. **Review migration code**: Check the generated migration file before applying
4. **Use descriptive names**: `AddUserProfileFields` not `Update1`
5. **One migration per feature**: Don't combine unrelated changes
6. **Commit migrations with code**: Version control both together
7. **Never modify applied migrations**: Create a new migration to fix issues

## Running the Application

### Using Aspire (Recommended)

#### Step-by-Step Guide

1. **Prerequisites Check**:
   ```bash
   # Verify .NET SDK
   dotnet --version
   # Should show: 10.0.x
   
   # Verify Docker is running
   docker ps
   # Should show running containers or empty list (not an error)
   ```

2. **Start the Application**:
   ```bash
   cd BudgetTrackerApp/BudgetTrackerApp.AppHost
   dotnet run
   ```

3. **Wait for Startup** (takes 10-30 seconds):
   
   You'll see console output like:
   ```
   info: Aspire.Hosting.DistributedApplication[0]
         Aspire version: 13.1.0
   info: Aspire.Hosting.DistributedApplication[0]
         Now listening on: http://localhost:15252
   info: Aspire.Hosting.DistributedApplication[0]
         Login to the dashboard at http://localhost:15252/login?t=xxx
   ```

4. **What Happens Automatically**:
   - ✅ PostgreSQL container starts with data volume (`postgres-data`)
   - ✅ Database `identitydb` is created
   - ✅ EF Core migrations execute (creates Identity tables)
   - ✅ API service starts on assigned port
   - ✅ Web frontend starts on assigned port
   - ✅ Health checks verify all services are running

5. **Access the Services**:
   - **Aspire Dashboard**: Open the URL from console (e.g., `http://localhost:15252`)
   - **API Service**: Check dashboard for the endpoint (e.g., `http://localhost:5001`)
   - **Web Frontend**: Check dashboard for the endpoint (e.g., `http://localhost:5002`)

6. **Test the API**:
   ```bash
   # Replace <port> with the actual port from the dashboard
   curl http://localhost:<port>/
   
   # Expected response:
   # "API service is running. Navigate to /weatherforecast to see sample data."
   ```

### Alternative: Running Services Individually

If you need to run services separately for debugging:

1. **Start PostgreSQL via Aspire** (required for connection strings):
   ```bash
   cd BudgetTrackerApp/BudgetTrackerApp.AppHost
   dotnet run
   ```

2. **In another terminal, run API service**:
   ```bash
   cd BudgetTrackerApp/BudgetTrackerApp.ApiService
   dotnet run
   ```

3. **In another terminal, run Web frontend** (optional):
   ```bash
   cd BudgetTrackerApp/BudgetTrackerApp.Web
   dotnet run
   ```

### Direct API Access

The API service will be available at a port assigned by Aspire (check the dashboard).

Example API calls:
```bash
# Get API port from Aspire dashboard, then:
export API_URL=http://localhost:5001

# Test endpoint
curl $API_URL/

# Register a user
curl -X POST $API_URL/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!",
    "confirmPassword": "Test123!",
    "firstName": "Test",
    "lastName": "User"
  }'

# Login
curl -X POST $API_URL/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!"
  }'
```

### Direct API Access

The API service will be available at a port assigned by Aspire (check the dashboard).

## Security Considerations

### Password Requirements

The default password policy requires:
- Minimum 6 characters
- At least one digit
- At least one lowercase letter
- At least one uppercase letter
- Non-alphanumeric characters are optional

To customize, modify the Identity configuration in `Program.cs`:
```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    // ... other settings
})
```

### Token Expiration

- **Access Token**: 60 minutes (configurable via `Jwt:ExpiryInMinutes`)
- **Refresh Token**: 7 days (hardcoded in `Program.cs`)

### Lockout Policy

After 5 failed login attempts, accounts are locked for 5 minutes.

## Troubleshooting

### Connection String Issues

If you see database connection errors:
1. Ensure PostgreSQL container is running
2. Check Aspire dashboard for connection string
3. Verify the database name matches "identitydb"

### JWT Token Validation Errors

- Ensure the JWT Key in appsettings.json is at least 32 characters
- Verify the token hasn't expired
- Check that Issuer and Audience match between token generation and validation

### CORS Errors

- Ensure your React app's URL is listed in the CORS policy
- Check that credentials are being sent with requests if using cookies
- Verify the API is running on the expected port

## Database Schema

The Identity setup creates the following tables:

- **AspNetUsers**: User accounts (includes custom fields: FirstName, LastName, CreatedAt)
- **AspNetRoles**: User roles
- **AspNetUserRoles**: User-role assignments
- **AspNetUserClaims**: User claims
- **AspNetRoleClaims**: Role claims
- **AspNetUserLogins**: External login providers
- **AspNetUserTokens**: Authentication tokens
- **RefreshTokens**: Custom table for refresh token management

## Next Steps

1. **Add Role Management**: Create endpoints to manage user roles
2. **Email Confirmation**: Implement email verification for new users
3. **Password Reset**: Add forgot password functionality
4. **Two-Factor Authentication**: Enhance security with 2FA
5. **User Profile Management**: Create endpoints to update user information
6. **Audit Logging**: Track authentication events and user actions

## References

- [ASP.NET Core Identity Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [JWT Authentication in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Entity Framework Core Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
