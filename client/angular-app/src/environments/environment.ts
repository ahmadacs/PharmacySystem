export interface AppEnvironment {
  production: boolean;
  apiUrl: string;
}

export const environment: AppEnvironment = {
  production: false,
  apiUrl: 'http://localhost:5066/api/v1'
};