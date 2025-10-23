// /src/stores/auth.ts
import { defineStore } from 'pinia';
import { http } from '@/api/http';

export const useAuthStore = defineStore('auth', {
  state: () => ({
    accessToken: '' as string,
    accessExpiresAt: '' as string,
  }),
  actions: {
    loadFromStorage() {
      this.accessToken = localStorage.getItem('accessToken') ?? '';
      this.accessExpiresAt = localStorage.getItem('accessExpiresAt') ?? '';
    },
    setToken(token: string, expiresAt: string) {
      this.accessToken = token;
      this.accessExpiresAt = expiresAt;
      localStorage.setItem('accessToken', token);
      localStorage.setItem('accessExpiresAt', expiresAt);
    },
    clear() {
      this.accessToken = '';
      this.accessExpiresAt = '';
      localStorage.removeItem('accessToken');
      localStorage.removeItem('accessExpiresAt');
    },
    // ★ 開發專用：向後端拿固定測試 token
    async devLogin() {
      const { data } = await http.post('/auth/dev-token');
      this.setToken(data.accessToken, data.accessExpiresAt);
      return data;
    },
  },
});
