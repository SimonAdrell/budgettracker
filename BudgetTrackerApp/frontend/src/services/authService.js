import apiClient from './apiClient';

const authService = {
  register: async ({ email, password, confirmPassword, firstName, lastName }) => {
    const response = await apiClient.post('/auth/register', {
      email,
      password,
      confirmPassword,
      firstName: firstName?.trim() || null,
      lastName: lastName?.trim() || null,
    });

    return response.data;
  },

  login: async (email, password) => {
    const response = await apiClient.post('/auth/login', {
      email,
      password,
    });

    const authData = response.data;

    // Store tokens and user info
    localStorage.setItem('token', authData.token);
    localStorage.setItem('refreshToken', authData.refreshToken);
    localStorage.setItem('user', JSON.stringify({
      email: authData.email,
      firstName: authData.firstName,
      lastName: authData.lastName,
    }));

    return authData;
  },

  logout: async () => {
    const token = localStorage.getItem('token');

    if (token) {
      try {
        await apiClient.post('/auth/logout', {}, {
          headers: { Authorization: `Bearer ${token}` }
        });
      } catch (error) {
        console.error('Logout error:', error);
      }
    }

    // Clear local storage
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
  },

  getCurrentUser: () => {
    const userStr = localStorage.getItem('user');
    if (userStr) {
      return JSON.parse(userStr);
    }
    return null;
  },

  getToken: () => {
    return localStorage.getItem('token');
  },

  isAuthenticated: () => {
    return !!localStorage.getItem('token');
  },
};

export default authService;
