// /src/api/http.ts
import axios from 'axios';
import { useAuthStore } from '@/stores/auth';

export const http = axios.create({
  baseURL: '/api', // 建議配合 vite proxy（見下）
});

// 自動夾 Authorization
http.interceptors.request.use((config) => {
  const token = useAuthStore().accessToken;
  if (token) {
    (config.headers ??= {} as any);
    (config.headers as any).Authorization = `Bearer ${token}`;
  }
  return config;
});
