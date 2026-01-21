import axios from 'axios';
import authService from './authService';

const importService = {
  uploadTransactions: async (file, accountId) => {
    const token = authService.getToken();
    const formData = new FormData();
    formData.append('file', file);
    formData.append('accountId', accountId);

    const response = await axios.post(`/api/api/import/upload`, formData, {
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'multipart/form-data',
      }
    });
    return response.data;
  },
};

export default importService;
