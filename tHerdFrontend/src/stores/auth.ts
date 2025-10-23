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
    setToken(token?: string, expiresAt?: string) {
  if (typeof token !== 'string' || typeof expiresAt !== 'string' || !token || !expiresAt) {
    console.warn('[setToken] invalid token payload', { token, expiresAt });
    return; // 直接拒寫
  }
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
  const res = await http.post('/auth/dev-token');
  console.log('[DEV-LOGIN payload]', res.data, Object.keys(res.data));
  const { accessToken, accessExpiresAt } = res.data; // 先假設是這兩個鍵
  if (!accessToken || !accessExpiresAt) {
    throw new Error('Bad payload: ' + JSON.stringify(res.data));
  }
  this.setToken(accessToken, accessExpiresAt);
  return res.data;
}
  },
});
