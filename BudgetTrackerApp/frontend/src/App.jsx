import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login';
import Register from './pages/Register';
import UserInfo from './pages/UserInfo';
import Import from './pages/Import';
import Dashboard from './pages/Dashboard';
import Transactions from './pages/Transactions';
import authService from './services/authService';
import './App.css';

function App() {
  return (
    <Router>
      <Routes>
        <Route
          path="/login"
          element={
            authService.isAuthenticated() ? (
              <Navigate to="/dashboard" replace />
            ) : (
              <Login />
            )
          }
        />
        <Route path="/register" element={<Register />} />
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
          path="/dashboard"
          element={
            authService.isAuthenticated() ? (
              <Dashboard />
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
          path="/transactions"
          element={
            authService.isAuthenticated() ? (
              <Transactions />
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />
        <Route
          path="/"
          element={
            authService.isAuthenticated() ? (
              <Navigate to="/dashboard" replace />
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
