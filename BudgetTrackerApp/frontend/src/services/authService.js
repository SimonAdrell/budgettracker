import axios from 'axios';

const authService = {
  login: async (email, password) => {
    const response = await axios.post(`/api/api/auth/login`, {
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
        await axios.post(`/api/api/auth/logout`, {}, {
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
