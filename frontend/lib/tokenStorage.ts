import Cookies from "js-cookie";

const ACCESS_TOKEN_KEY = "accessToken";
const REFRESH_TOKEN_KEY = "refreshToken";

// Short-lived cookie for access token, longer for refresh token
export const tokenStorage = {
  getAccessToken: () => Cookies.get(ACCESS_TOKEN_KEY) ?? null,
  getRefreshToken: () => Cookies.get(REFRESH_TOKEN_KEY) ?? null,

  setTokens: (accessToken: string, refreshToken: string) => {
    Cookies.set(ACCESS_TOKEN_KEY, accessToken, {
      expires: 1, // 1 day, adjust to your access token TTL
      secure: process.env.NODE_ENV === "production",
      sameSite: "strict",
      path: "/",
    });
    Cookies.set(REFRESH_TOKEN_KEY, refreshToken, {
      expires: 7, // matches backend's 30-day refresh token expiry
      secure: process.env.NODE_ENV === "production",
      sameSite: "strict",
      path: "/",
    });
  },

  clearTokens: () => {
    Cookies.remove(ACCESS_TOKEN_KEY, { path: "/" });
    Cookies.remove(REFRESH_TOKEN_KEY, { path: "/" });
  },
};