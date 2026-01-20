import axios from 'axios';
import authService from './authService';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

const accountService = {
  getAccounts: async () => {
    const token = authService.getToken();
    const response = await axios.get(`${API_URL}/api/accounts`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    return response.data;
  },

  createAccount: async (name, accountNumber) => {
    const token = authService.getToken();
    const response = await axios.post(`${API_URL}/api/accounts`, 
      { name, accountNumber },
      { headers: { Authorization: `Bearer ${token}` }}
    );
    return response.data;
  },
};

export default accountService;
