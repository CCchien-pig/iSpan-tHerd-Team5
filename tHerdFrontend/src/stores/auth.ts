// /src/stores/auth.ts
import { defineStore } from 'pinia';
import { http } from '@/api/http';

export interface MeDto {
  id: string
  email: string
  name: string
  userNumberId: number
  roles: string[]
}

export const useAuthStore = defineStore('auth', {
  state: () => ({
    accessToken: '' as string,
    accessExpiresAt: '' as string,
    refreshToken: '' as string,
    user: null as MeDto | null,
    _loadedFromStorage: false,   // 已載入 localStorage？
    isReady: false,              // 前端可用的「初始化完成」旗標
  }),

  getters: {
    isAuthenticated: (s) => !!s.accessToken, //（必要時可加入 expiresAt 檢查）
  },

  actions: {
    loadFromStorage() {
      if (this._loadedFromStorage) return
      this.accessToken = localStorage.getItem('accessToken') ?? ''
      this.accessExpiresAt = localStorage.getItem('accessExpiresAt') ?? ''
      this.refreshToken = localStorage.getItem('refreshToken') ?? ''
      this._loadedFromStorage = true
    },

    setTokenPair(accessToken: string, accessExpiresAt: string, refreshToken?: string) {
      if (!accessToken || !accessExpiresAt) return
      this.accessToken = accessToken
      this.accessExpiresAt = accessExpiresAt
      if (refreshToken) {
        this.refreshToken = refreshToken
        localStorage.setItem('refreshToken', refreshToken)
      }
      localStorage.setItem('accessToken', accessToken)
      localStorage.setItem('accessExpiresAt', accessExpiresAt)
    },

    clear() {
      this.accessToken = ''
      this.accessExpiresAt = ''
      this.refreshToken = ''
      this.user = null
      localStorage.removeItem('accessToken')
      localStorage.removeItem('accessExpiresAt')
      localStorage.removeItem('refreshToken')
    },

    // ★ 取得/快取目前登入者
     async ensureUser(force = false) {
      if (!this.isAuthenticated) {
        this.user = null
        return null
      }
      if (this.user && !force) return this.user
      const { data } = await http.get<MeDto>('/auth/me')  // 攔截器會自動帶 Authorization
      this.user = data
      return data
    },

   // 供攔截器呼叫的刷新：只管 token，不變動 user
    async refresh() {
      if (!this.refreshToken) throw new Error('No refresh token')
      const { data } = await http.post<{ accessToken: string; accessExpiresAt: string; refreshToken: string }>(
        '/auth/refresh',
        { refreshToken: this.refreshToken }
      )
      this.setTokenPair(data.accessToken, data.accessExpiresAt, data.refreshToken)
      return data.accessToken
    },

    // ★ 新增：正式登入
    async login(
  email: string,
  password: string,
  opts?: { recaptchaToken?: string; rememberMe?: boolean }
) {
  const payload: any = { email, password };
  if (opts?.recaptchaToken) payload.recaptchaToken = opts.recaptchaToken;
  if (typeof opts?.rememberMe === 'boolean') payload.rememberMe = opts.rememberMe;

  const { data } = await http.post('/auth/login', payload);

  if (data.refreshToken) this.setTokenPair(data.accessToken, data.accessExpiresAt, data.refreshToken);
  else this.setTokenPair(data.accessToken, data.accessExpiresAt);

  if (data.user) this.user = data.user;
  else await this.ensureUser(true);
  return data;
},

    async logout() {
      try {
        if (this.refreshToken) {
          await http.post('/auth/logout', { refreshToken: this.refreshToken })
        }
      } catch {
        // 後端已過期/撤銷也無妨
      } finally {
        this.clear()
      }
    },

    // ★ 新增：一次性初始化（頁面啟動或守門員等到它完成）
    async init() {
      if (this.isReady) return
      this.loadFromStorage()
      try {
        if (this.accessToken) {
          // 有 token 時嘗試拉一次 /auth/me（失敗就交給攔截器處理 refresh/清空）
          await this.ensureUser()
        } else {
          this.user = null
        }
      } catch {
        // 不拋錯，交給路由守門員處理導向
      } finally {
        this.isReady = true
      }
    },

    // ★ 開發專用：向後端拿固定測試 token
  async devLogin() {
  // const res = await http.post('/auth/dev-token');
  // console.log('[DEV-LOGIN payload]', res.data, Object.keys(res.data));
  // const { accessToken, accessExpiresAt } = res.data; // 先假設是這兩個鍵
  // if (!accessToken || !accessExpiresAt) {
  //   throw new Error('Bad payload: ' + JSON.stringify(res.data));
  // }
  // this.setToken(accessToken, accessExpiresAt);
  // return res.data;
  const { data } = await http.post<{
        accessToken: string
        accessExpiresAt: string
        refreshToken?: string
        user?: MeDto
      }>('/auth/dev-token')
      this.setTokenPair(data.accessToken, data.accessExpiresAt, data.refreshToken)
      if (data.user) this.user = data.user
      return data
    }, 
  },
});
