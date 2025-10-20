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

app.mount('#app')
