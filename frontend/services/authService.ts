import { api } from "@/lib/axios";
import { AuthClientResponse, LoginPayload, SignupPayload, User } from "@/types/auth";

export const authService = {
  login: async (payload: LoginPayload): Promise<AuthClientResponse> =>
    (await api.post("/auth/login", payload)).data,

  signup: async (payload: SignupPayload): Promise<AuthClientResponse> =>
    (await api.post("/auth/signup", payload)).data,

  googleLogin: async (idToken: string): Promise<AuthClientResponse> =>
    (await api.post("/auth/google", { idToken })).data,

  verifyEmail: async (email: string, verificationToken: string) =>
    (await api.post("/auth/verify-email", { email, verificationToken })).data,

  logout: async () => (await api.post("/auth/logout")).data,

  getCurrentUser: async (): Promise<User> => (await api.get("/auth/me")).data,
};