import apiClient from './apiClient';
import authService from './authService';

const dashboardService = {
  getAccountDashboard: async (accountId) => {
    const token = authService.getToken();
    const response = await apiClient.get(`/dashboard/${accountId}`, {
      headers: { Authorization: `Bearer ${token}` }
    });

    return response.data;
  },
};

export default dashboardService;
