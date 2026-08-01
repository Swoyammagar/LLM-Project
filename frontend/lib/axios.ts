import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";

const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL;

export const api = axios.create({
  baseURL: BASE_URL,
  withCredentials: true,
  headers: { "Content-Type": "application/json" },
});

let isRefreshing = false;
let refreshSubscribers: Array<() => void> = [];

function subscribeTokenRefresh(cb: () => void) {
  refreshSubscribers.push(cb);
}
function onRefreshed() {
  refreshSubscribers.forEach((cb) => cb());
  refreshSubscribers = [];
}

interface RetriableConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

// A 401 here is an expected, normal outcome (checking "am I logged in?" and
// getting "no") — never force-redirect or attempt a refresh for these.
const SILENT_401_ENDPOINTS = ["/auth/me"];

function redirectToLogin() {
  if (typeof window === "undefined") return;
  // Guard against reloading when already on /login — assigning location.href
  // to the current URL still forces a full page reload in most browsers.
  if (window.location.pathname !== "/login") {
    window.location.href = "/login";
  }
}

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetriableConfig;

    if (!originalRequest || error.response?.status !== 401) {
      return Promise.reject(error);
    }

    if (SILENT_401_ENDPOINTS.some((p) => originalRequest.url?.includes(p))) {
      return Promise.reject(error);
    }

    const authEndpoints = ["/auth/refresh-token", "/auth/login", "/auth/signup", "/auth/google"];
    if (authEndpoints.some((p) => originalRequest.url?.includes(p))) {
      redirectToLogin();
      return Promise.reject(error);
    }

    if (originalRequest._retry) {
      redirectToLogin();
      return Promise.reject(error);
    }
    originalRequest._retry = true;

    if (isRefreshing) {
      return new Promise((resolve) => {
        subscribeTokenRefresh(() => resolve(api(originalRequest)));
      });
    }

    isRefreshing = true;

    try {
      await axios.post(`${BASE_URL}/auth/refresh-token`, {}, { withCredentials: true });
      isRefreshing = false;
      onRefreshed();
      return api(originalRequest);
    } catch (refreshError) {
      isRefreshing = false;
      refreshSubscribers = [];
      redirectToLogin();
      return Promise.reject(refreshError);
    }
  }
);