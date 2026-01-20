# React Frontend for Budget Tracker

This is the React frontend for the Budget Tracker application, featuring authentication integration with the ASP.NET Core backend.

## Features

- **Login Page**: Authenticate users with email and password
- **User Info Page**: Display logged-in user's name and email
- **Logout Functionality**: Securely logout and clear authentication tokens
- **Protected Routes**: Automatic redirect to login for unauthenticated users
- **JWT Token Management**: Secure token storage in localStorage

## Prerequisites

- Node.js 18+ and npm
- Backend API service running (via Aspire)

## Getting Started

### 1. Install Dependencies

```bash
cd BudgetTrackerApp/frontend
npm install
```

### 2. Configure API URL

The application uses environment variables to configure the API URL. By default, it points to `http://localhost:5000`.

To override this, create a `.env.local` file:

```bash
# .env.local
VITE_API_URL=http://localhost:<your-api-port>
```

**Note**: When running with Aspire, check the Aspire dashboard for the actual API service port.

### 3. Run the Development Server

```bash
npm run dev
```

The application will start at `http://localhost:5173`.

## Running with Aspire

### Option 1: Separate Terminals (Recommended for Development)

1. **Terminal 1 - Start Aspire AppHost** (this starts the API and PostgreSQL):
   ```bash
   cd BudgetTrackerApp/BudgetTrackerApp.AppHost
   dotnet run
   ```

2. **Check the API port** in the Aspire dashboard (usually shown in the console output)

3. **Terminal 2 - Start React Frontend**:
   ```bash
   cd BudgetTrackerApp/frontend
   npm run dev
   ```

4. **Access the application**:
   - React Frontend: http://localhost:5173
   - Aspire Dashboard: (URL shown in Terminal 1 output)

### Option 2: Production Build

1. Build the React application:
   ```bash
   npm run build
   ```

2. Serve the production build:
   ```bash
   npm run preview
   ```

## Project Structure

```
frontend/
├── src/
│   ├── pages/
│   │   ├── Login.jsx           # Login page component
│   │   ├── Login.css          # Login page styles
│   │   ├── UserInfo.jsx       # User info page component
│   │   └── UserInfo.css       # User info page styles
│   ├── services/
│   │   └── authService.js     # Authentication service
│   ├── App.jsx                # Main app component with routing
│   ├── App.css                # App styles
│   ├── index.css              # Global styles
│   └── main.jsx               # Entry point
├── .env                       # Default environment variables
├── package.json               # Dependencies and scripts
└── vite.config.js            # Vite configuration
```

## Authentication Flow

### Login

1. User enters email and password on the login page
2. Credentials are sent to `/api/auth/login` endpoint
3. On success:
   - JWT token and refresh token are stored in localStorage
   - User information (name, email) is stored in localStorage
   - User is redirected to the User Info page

### Logout

1. User clicks the logout button
2. Logout request is sent to `/api/auth/logout` endpoint (if token is valid)
3. All tokens and user data are cleared from localStorage
4. User is redirected to the login page

### Protected Routes

- The `/user-info` route is protected
- If a user tries to access it without being authenticated, they're redirected to `/login`
- The root route (`/`) redirects to `/user-info` if authenticated, otherwise to `/login`

## API Endpoints Used

- **POST** `/api/auth/login` - Login endpoint
- **POST** `/api/auth/logout` - Logout endpoint (requires authentication)

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `VITE_API_URL` | Backend API base URL | `http://localhost:5000` |

## Development

### Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint

### Testing the Application

1. **Start the backend** via Aspire (see above)

2. **Register a test user** using the API or a tool like curl:
   ```bash
   curl -X POST http://localhost:<api-port>/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test@example.com",
       "password": "Test123!",
       "confirmPassword": "Test123!",
       "firstName": "Test",
       "lastName": "User"
     }'
   ```

3. **Start the React frontend** (see above)

4. **Navigate to** http://localhost:5173

5. **Login** with the credentials you just registered

6. You should see the User Info page displaying the user's name

7. **Click Logout** to test the logout functionality

## Styling

The application uses custom CSS with:
- Modern gradient backgrounds
- Clean card-based layouts
- Responsive design
- Focus states for accessibility

## Security Considerations

- JWT tokens are stored in localStorage (consider using httpOnly cookies for production)
- CORS is configured on the backend to accept requests from localhost:5173
- All API calls to protected endpoints include the JWT token in the Authorization header
- Logout properly clears all authentication data

## Troubleshooting

### CORS Errors

If you see CORS errors in the browser console:
1. Verify the API URL is correct in your `.env.local`
2. Check that the backend CORS policy includes your frontend URL
3. Ensure the backend is running

### Login Fails

1. Verify the backend API is running via Aspire
2. Check the API port in the Aspire dashboard
3. Ensure your `.env.local` has the correct API URL
4. Verify the user exists in the database (register first if needed)

### Cannot Connect to API

1. Check that Aspire AppHost is running
2. Verify PostgreSQL container is running via Aspire dashboard
3. Check the browser console for the actual API URL being used
4. Ensure no firewall is blocking the connection

## Next Steps

Potential enhancements:
- Add user registration page
- Implement token refresh logic
- Add password reset functionality
- Implement proper error handling and validation
- Add loading states and better UX feedback
- Use a state management library (Redux, Zustand, etc.)
- Add unit and integration tests

## Technologies Used

- **React 19** - UI library
- **React Router Dom 7** - Client-side routing
- **Axios** - HTTP client
- **Vite** - Build tool and dev server

## License

This project is part of the BudgetTracker application.
