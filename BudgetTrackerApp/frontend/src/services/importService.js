import apiClient from './apiClient';
import authService from './authService';

const importService = {
  uploadTransactions: async (file, accountId) => {
    const token = authService.getToken();
    const formData = new FormData();
    formData.append('file', file);
    formData.append('accountId', accountId);

    const response = await apiClient.post('/import/upload', formData, {
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'multipart/form-data',
      }
    });
    return response.data;
  },
};

export default importService;
