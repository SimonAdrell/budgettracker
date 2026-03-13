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

// Default to the backend's real route prefix. The Vite dev proxy now keeps
// `/api` requests intact and includes a temporary compatibility rule for the
// legacy `/api/api/*` service calls until those services migrate here.
export const apiBaseUrl =
  normalizeConfiguredBaseUrl(configuredBaseUrl) || '/api';

const apiClient = axios.create({
  baseURL: apiBaseUrl,
});

export default apiClient;
