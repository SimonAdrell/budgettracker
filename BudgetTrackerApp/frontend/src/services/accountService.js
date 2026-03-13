import apiClient from './apiClient';
import authService from './authService';

const accountService = {
  getAccounts: async () => {
    const token = authService.getToken();
    const response = await apiClient.get('/accounts', {
      headers: { Authorization: `Bearer ${token}` }
    });
    return response.data;
  },

  createAccount: async (name, accountNumber) => {
    const token = authService.getToken();
    const response = await apiClient.post('/accounts',
      { name, accountNumber },
      { headers: { Authorization: `Bearer ${token}` } }
    );
    return response.data;
  },
};

export default accountService;
