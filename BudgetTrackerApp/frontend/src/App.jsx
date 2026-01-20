import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login';
import UserInfo from './pages/UserInfo';
import Import from './pages/Import';
import authService from './services/authService';
import './App.css';

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route
          path="/user-info"
          element={
            authService.isAuthenticated() ? (
              <UserInfo />
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />
        <Route
          path="/import"
          element={
            authService.isAuthenticated() ? (
              <Import />
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />
        <Route
          path="/"
          element={
            authService.isAuthenticated() ? (
              <Navigate to="/import" replace />
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />
      </Routes>
    </Router>
  );
}

export default App;
