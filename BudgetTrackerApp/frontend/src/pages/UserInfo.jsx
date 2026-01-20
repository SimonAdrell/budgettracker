import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import authService from '../services/authService';
import './UserInfo.css';

function UserInfo() {
  const [user, setUser] = useState(null);
  const navigate = useNavigate();

  useEffect(() => {
    const currentUser = authService.getCurrentUser();
    if (!currentUser) {
      navigate('/login');
    } else {
      setUser(currentUser);
    }
  }, [navigate]);

  const handleLogout = async () => {
    await authService.logout();
    navigate('/login');
  };

  if (!user) {
    return <div>Loading...</div>;
  }

  return (
    <div className="user-info-container">
      <div className="user-info-box">
        <h2>User Information</h2>
        <div className="user-details">
          <div className="user-detail-row">
            <span className="label">Name:</span>
            <span className="value">
              {user.firstName} {user.lastName}
            </span>
          </div>
          <div className="user-detail-row">
            <span className="label">Email:</span>
            <span className="value">{user.email}</span>
          </div>
        </div>
        <button onClick={handleLogout} className="logout-button">
          Logout
        </button>
      </div>
    </div>
  );
}

export default UserInfo;
