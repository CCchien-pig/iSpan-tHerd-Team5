import './assets/main.css'
import 'bootstrap/dist/css/bootstrap.min.css'
// import 'bootstrap'
import 'bootstrap/dist/js/bootstrap.bundle.min.js'
import 'bootstrap-icons/font/bootstrap-icons.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'

// 初始化 Mock 系統（如果啟用）
import { initializeMocks } from './services/mocks'
initializeMocks()

// Google Maps
import VueGoogleMaps from '@fawmi/vue-google-maps'

//啟用 ElementPlus
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'

const app = createApp(App);

app.use(createPinia())
app.use(router)

// Google Maps
app.use(VueGoogleMaps, {
  load: {
    key: import.meta.env.VITE_GOOGLE_MAPS_KEY,
    language: 'zh-TW',
    v: 'weekly',
    loading: 'async',
    libraries: 'places',
  },
})
//啟用 ElementPlus
app.use(ElementPlus)

// 初始化 auth
import { http } from '@/api/http'
import { useAuthStore } from '@/stores/auth';
if (import.meta.env.DEV) {
  window.__http = http
  window.__auth = useAuthStore()
}
const auth = useAuthStore()
// 初始化：讀 localStorage 並（有 token 時）嘗試拉 /auth/me
await auth.init() 
// ★ 這裡註冊守門員（此時 Pinia 已就緒）
router.beforeEach(async (to) => {
  // 可選：避免對 login 頁自我攔截
  if (to.name === 'login') {
    // 若已登入，導回 redirect / 或首頁
    if (auth.isAuthenticated) {
      const back = (to.query.redirect && String(to.query.redirect)) || '/'
      return { path: back }
    }
    return
  }

  // 先等初始化完成（避免一進站就誤判未登入）
  if (!auth.isReady) {
    await auth.init()
  }


  // 檢查整條 matched 以支援巢狀路由 ⬇⬇⬇
  const needAuth = to.matched.some(r => r.meta?.requiresAuth)

  if (needAuth && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }
})

// 開發模式 & 沒 token 時自動拿一次 dev-token
if (import.meta.env.DEV && !auth.accessToken) {
  console.log('[boot] calling devLogin()')
  try {
    await auth.devLogin()  // ★ 先等 token 回來
    console.log('[boot] devLogin done')
  } catch (e) {
    console.error('[boot] devLogin failed', e)
  }
}


app.mount('#app')
