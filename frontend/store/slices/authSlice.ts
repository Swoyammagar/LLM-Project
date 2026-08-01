import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { authService } from "@/services/authService";
import { AuthClientResponse, LoginPayload, SignupPayload, User } from "@/types/auth";

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  loading: boolean;
  initializing: boolean; // true while checking session on first load
  error: string | null;
}

const initialState: AuthState = {
  user: null,
  isAuthenticated: false,
  loading: false,
  initializing: true,
  error: null,
};

function extractErrorMessage(err: any): string {
  return err?.response?.data?.message || err?.response?.data?.error || "Something went wrong. Please try again.";
}

export const loginUser = createAsyncThunk<AuthClientResponse, LoginPayload, { rejectValue: string }>(
  "auth/login",
  async (payload, { rejectWithValue }) => {
    try {
      return await authService.login(payload);
    } catch (err) {
      return rejectWithValue(extractErrorMessage(err));
    }
  }
);

export const signupUser = createAsyncThunk<AuthClientResponse, SignupPayload, { rejectValue: string }>(
  "auth/signup",
  async (payload, { rejectWithValue }) => {
    try {
      return await authService.signup(payload);
    } catch (err) {
      return rejectWithValue(extractErrorMessage(err));
    }
  }
);

export const googleLogin = createAsyncThunk<AuthClientResponse, string, { rejectValue: string }>(
  "auth/googleLogin",
  async (idToken, { rejectWithValue }) => {
    try {
      return await authService.googleLogin(idToken);
    } catch (err) {
      return rejectWithValue(extractErrorMessage(err));
    }
  }
);

// Called once on app load to check if a valid session cookie exists
export const fetchCurrentUser = createAsyncThunk<User, void, { rejectValue: string }>(
  "auth/fetchCurrentUser",
  async (_, { rejectWithValue }) => {
    try {
      return await authService.getCurrentUser();
    } catch (err) {
      return rejectWithValue(extractErrorMessage(err));
    }
  }
);

export const logoutUser = createAsyncThunk("auth/logoutUser", async () => {
  await authService.logout();
});

const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    clearAuthError(state) {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    const pending = (state: AuthState) => {
      state.loading = true;
      state.error = null;
    };
    const rejected = (state: AuthState, action: any) => {
      state.loading = false;
      state.error = action.payload ?? "Authentication failed";
    };
    const fulfilled = (state: AuthState, action: { payload: AuthClientResponse }) => {
      state.loading = false;
      state.isAuthenticated = true;
      state.user = action.payload.user;
    };

    builder
      .addCase(loginUser.pending, pending)
      .addCase(loginUser.fulfilled, fulfilled)
      .addCase(loginUser.rejected, rejected)
      .addCase(signupUser.pending, pending)
      .addCase(signupUser.fulfilled, fulfilled)
      .addCase(signupUser.rejected, rejected)
      .addCase(googleLogin.pending, pending)
      .addCase(googleLogin.fulfilled, fulfilled)
      .addCase(googleLogin.rejected, rejected)

      .addCase(fetchCurrentUser.pending, (state) => {
        state.initializing = true;
      })
      .addCase(fetchCurrentUser.fulfilled, (state, action) => {
        state.initializing = false;
        state.isAuthenticated = true;
        state.user = action.payload;
      })
      .addCase(fetchCurrentUser.rejected, (state) => {
        state.initializing = false;
        state.isAuthenticated = false;
        state.user = null;
      })

      .addCase(logoutUser.fulfilled, (state) => {
        state.user = null;
        state.isAuthenticated = false;
      });
  },
});

export const { clearAuthError } = authSlice.actions;
export default authSlice.reducer;