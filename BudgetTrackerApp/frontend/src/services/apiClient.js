import axios from 'axios';

const trimTrailingSlash = (value) => value.replace(/\/+$/, '');

const normalizeConfiguredBaseUrl = (value) => {
  const trimmedValue = trimTrailingSlash(value.trim());

  if (!trimmedValue) {
    return '';
  }

  return trimmedValue.endsWith('/api') ? trimmedValue : `${trimmedValue}/api`;
};

const configuredBaseUrl =
  import.meta.env.VITE_API_BASE_URL ??
  import.meta.env.VITE_API_URL ??
  '';

// Default to the backend's real route prefix.
export const apiBaseUrl =
  normalizeConfiguredBaseUrl(configuredBaseUrl) || '/api';

const authPathsToSkipRefresh = [
  '/auth/login',
  '/auth/register',
  '/auth/refresh',
  '/auth/logout',
];

const apiClient = axios.create({
  baseURL: apiBaseUrl,
});

let refreshPromise = null;

const isRefreshEligibleRequest = (config = {}) => {
  const requestUrl = config.url || '';

  if (!config.headers?.Authorization) {
    return false;
  }

  return !authPathsToSkipRefresh.some((path) => requestUrl.includes(path));
};

const persistAuthData = (authData) => {
  localStorage.setItem('token', authData.token);
  localStorage.setItem('refreshToken', authData.refreshToken);
  localStorage.setItem('user', JSON.stringify({
    email: authData.email,
    firstName: authData.firstName,
    lastName: authData.lastName,
  }));
};

const clearAuthData = () => {
  localStorage.removeItem('token');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
};

const refreshAccessToken = async () => {
  const token = localStorage.getItem('token');
  const refreshToken = localStorage.getItem('refreshToken');

  if (!token || !refreshToken) {
    throw new Error('Refresh token flow requires stored auth tokens.');
  }

  const response = await axios.post(`${apiBaseUrl}/auth/refresh`, {
    token,
    refreshToken,
  });

  persistAuthData(response.data);
  return response.data.token;
};

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (
      error.response?.status !== 401 ||
      !originalRequest ||
      originalRequest._retry ||
      !isRefreshEligibleRequest(originalRequest)
    ) {
      return Promise.reject(error);
    }

    originalRequest._retry = true;

    try {
      refreshPromise ??= refreshAccessToken().finally(() => {
        refreshPromise = null;
      });

      const newToken = await refreshPromise;
      originalRequest.headers = {
        ...originalRequest.headers,
        Authorization: `Bearer ${newToken}`,
      };

      return apiClient(originalRequest);
    } catch (refreshError) {
      clearAuthData();
      return Promise.reject(refreshError);
    }
  }
);

export default apiClient;
