import axios from 'axios';
import authService from './authService';

const accountService = {
  getAccounts: async () => {
    const token = authService.getToken();
    const response = await axios.get(`/api/api/accounts`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    return response.data;
  },

  createAccount: async (name, accountNumber) => {
    const token = authService.getToken();
    const response = await axios.post(`/api/api/accounts`,
      { name, accountNumber },
      { headers: { Authorization: `Bearer ${token}` } }
    );
    return response.data;
  },
};

export default accountService;
