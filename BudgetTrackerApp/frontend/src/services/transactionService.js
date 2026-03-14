import apiClient from './apiClient';
import authService from './authService';

const transactionService = {
  getTransactionsForAccount: async (accountId) => {
    const token = authService.getToken();
    const response = await apiClient.get(`/transactions/${accountId}`, {
      headers: { Authorization: `Bearer ${token}` }
    });

    return response.data;
  },
};

export default transactionService;
