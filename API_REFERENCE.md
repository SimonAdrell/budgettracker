# API Quick Reference

## Authentication Endpoints

### Register User
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123",
  "confirmPassword": "SecurePassword123",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response (200 OK):**
```json
{
  "message": "User registered successfully"
}
```

### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123"
}
```

**Response (200 OK):**
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

### Refresh Token
```http
POST /api/auth/refresh
Content-Type: application/json

{
  "token": "expired-jwt-token",
  "refreshToken": "current-refresh-token"
}
```

**Response (200 OK):** Same as login response with new tokens

### Logout
```http
POST /api/auth/logout
Authorization: Bearer {access-token}
```

**Response (200 OK):**
```json
{
  "message": "Logged out successfully"
}
```

### Access Protected Endpoint (Example)
```http
GET /weatherforecast
Authorization: Bearer {access-token}
```

**Response (200 OK):**
```json
[
  {
    "date": "2026-01-21",
    "temperatureC": 25,
    "temperatureF": 76,
    "summary": "Warm"
  }
]
```

## Using cURL

### Register
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!",
    "confirmPassword": "Test123!",
    "firstName": "Test",
    "lastName": "User"
  }'
```

### Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!"
  }'
```

### Access Protected Endpoint
```bash
curl -X GET http://localhost:5000/weatherforecast \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

### Refresh Token
```bash
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "token": "YOUR_EXPIRED_TOKEN",
    "refreshToken": "YOUR_REFRESH_TOKEN"
  }'
```

### Logout
```bash
curl -X POST http://localhost:5000/api/auth/logout \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

## Password Requirements

- Minimum 6 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- Non-alphanumeric characters optional

## Token Expiration

- **Access Token**: 60 minutes (default, configurable)
- **Refresh Token**: 7 days

## Error Responses

### 400 Bad Request
Invalid input or validation errors
```json
{
  "errors": [
    {
      "code": "PasswordTooShort",
      "description": "Passwords must be at least 6 characters."
    }
  ]
}
```

### 401 Unauthorized
Invalid credentials or expired/invalid token
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

## Analytics Endpoints (`/api/v1/analytics`)

All analytics endpoints require `Authorization: Bearer {access-token}`.

### Query Parameters

- `fromUtc` (optional, ISO-8601 UTC): start of range
- `toUtc` (optional, ISO-8601 UTC): end of range
- `bucket` (optional): `day`, `week`, `month` (default: `month`)
- `accountId` (optional): limit to one account the user can access

Validation/defaults:
- If both dates are omitted, range defaults to previous UTC calendar month.
- `fromUtc` must be <= `toUtc`.
- Boundaries are normalized to UTC and aligned to bucket starts.
- If `accountId` is omitted, analytics aggregate all accounts accessible by the authenticated user.
- If `accountId` is provided but not accessible, response is `403 Forbidden`.

### Shared Response Metadata

Each response includes:
- `metadata.fromUtc`: normalized bucket-aligned start (UTC)
- `metadata.toUtc`: normalized bucket-aligned exclusive end (UTC)
- `metadata.bucket`: effective bucket (`day|week|month`)
- `metadata.currencyCode`: configured reporting currency code (`USD` by default)

### Endpoints

#### `GET /api/v1/analytics/balance-over-time`

Returns:
- `points[]`
- `points[].periodStartUtc`
- `points[].balance` (signed)

#### `GET /api/v1/analytics/income-vs-expenses`

Returns:
- `points[]`
- `points[].periodStartUtc`
- `points[].income` (positive magnitude)
- `points[].expenses` (positive magnitude)
- `points[].net` (signed: `income - expenses`)

#### `GET /api/v1/analytics/spending-by-category`

Returns:
- `rows[]`
- `rows[].categoryId` (nullable)
- `rows[].categoryName` (`"Uncategorized"` for missing category)
- `rows[].amount` (positive magnitude)

#### `GET /api/v1/analytics/category-spending-over-time`

Returns:
- `points[]`
- `points[].periodStartUtc`
- `points[].categories[]`
- `points[].categories[].categoryId` (nullable)
- `points[].categories[].categoryName` (`"Uncategorized"` for missing category)
- `points[].categories[].amount` (positive magnitude)

#### `GET /api/v1/analytics/net-worth-over-time`

Returns:
- `points[]`
- `points[].periodStartUtc`
- `points[].netWorth` (signed)

### Currency Behavior

- Analytics are currently single-currency output (`metadata.currencyCode`).
- Default currency is `USD` and can be changed via `Analytics:CurrencyCode` in API settings.

### Known Limitation

- Transfers are currently included in analytics aggregates.
- Follow-up issue: https://github.com/SimonAdrell/budgettracker/issues/18
