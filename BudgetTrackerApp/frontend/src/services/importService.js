import axios from 'axios';
import authService from './authService';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

const importService = {
  uploadTransactions: async (file, accountId) => {
    const token = authService.getToken();
    const formData = new FormData();
    formData.append('file', file);
    formData.append('accountId', accountId);

    const response = await axios.post(`${API_URL}/api/import/upload`, formData, {
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'multipart/form-data',
      }
    });
    return response.data;
  },
};

export default importService;
