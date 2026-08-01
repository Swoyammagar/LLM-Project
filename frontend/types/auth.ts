export interface User {
  id: string;
  email: string;
  name: string;
  profilePicture?: string | null;
  createdAt: string;
}

export interface AuthClientResponse {
  user: User;
  isNewUser: boolean;
}

export interface LoginPayload {
  email: string;
  password: string;
}

export interface SignupPayload {
  email: string;
  password: string;
  name: string;
}