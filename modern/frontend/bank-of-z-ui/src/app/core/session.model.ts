export interface Session {
  isAuthenticated: boolean;
  userName: string | null;
  customerId: string | null;
  roles: string[];
}

export interface LoginRequest {
  userName: string;
  password: string;
  rememberMe: boolean;
}
