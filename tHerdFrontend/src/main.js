import './assets/main.css'
import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap'
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

const app = createApp(App)

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

app.mount('#app')
