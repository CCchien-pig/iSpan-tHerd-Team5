import './assets/main.css'
import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap/dist/js/bootstrap.bundle.min.js'
import 'bootstrap-icons/font/bootstrap-icons.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'

// Google Maps
import VueGoogleMaps from '@fawmi/vue-google-maps'

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

// 初始化 auth
import { useAuthStore } from '@/stores/auth';
const auth = useAuthStore();
auth.loadFromStorage();

// （選擇）開發模式 & 沒 token 時自動拿一次 dev-token
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
