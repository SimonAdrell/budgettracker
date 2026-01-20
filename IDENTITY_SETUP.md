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

### Apply Migrations

Migrations are automatically applied on application startup via:
```csharp
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}
```

### Manual Migration Commands

To create a new migration:
```bash
cd BudgetTrackerApp/BudgetTrackerApp.ApiService
dotnet ef migrations add MigrationName --output-dir Data/Migrations
```

To apply migrations manually:
```bash
dotnet ef database update
```

To remove the last migration:
```bash
dotnet ef migrations remove
```

## Running the Application

### Using Aspire (Recommended)

```bash
cd BudgetTrackerApp/BudgetTrackerApp.AppHost
dotnet run
```

This will:
1. Start PostgreSQL container with data volume
2. Create the identitydb database
3. Start the API service
4. Start the web frontend
5. Apply database migrations automatically

Access the Aspire dashboard at the URL shown in the console output.

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
