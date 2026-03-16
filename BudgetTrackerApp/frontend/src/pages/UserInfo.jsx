import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import authService from '../services/authService';
import './UserInfo.css';

function UserInfo() {
  const navigate = useNavigate();
  const user = authService.getCurrentUser();

  useEffect(() => {
    if (!user) {
      navigate('/login');
    }
  }, [navigate, user]);

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
        <div style={{ marginTop: '20px' }}>
          <button 
            onClick={() => navigate('/import')} 
            className="logout-button"
            style={{ backgroundColor: '#007bff', marginRight: '10px' }}
          >
            Import Transactions
          </button>
          <button onClick={handleLogout} className="logout-button">
            Logout
          </button>
        </div>
      </div>
    </div>
  );
}

export default UserInfo;
