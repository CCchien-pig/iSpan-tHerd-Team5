import { createRouter, createWebHistory } from 'vue-router'
import Layout from '@/components/layout/Layout.vue'
import HomeView from '../pages/home/Home.vue'

// 自動載入 router/*.js（排除 index.js）
const modules = import.meta.glob('./!(index).js', { eager: true })
let moduleRoutes = []

Object.values(modules).forEach((mod) => {
  moduleRoutes.push(...(mod.default || []));
})

// ❗把 userlogin / userregister / userme 從自動載入的 children 中排除
const AUTH_ROUTE_NAMES = ['userlogin', 'userregister']
const childRoutes = moduleRoutes.filter(r => !AUTH_ROUTE_NAMES.includes(r?.name))

// 不走 Layout 的「獨立頁面」
const standaloneAuthRoutes = [
  {
    path: '/user/login',
    name: 'userlogin',
    component: () => import('@/pages/modules/user/UserLogin.vue'),
    meta: { title: '登入', blankLayout: true } // 可加 blankLayout 供 Layout 判斷
  },
  {
    path: '/user/register',
    name: 'userregister',
    component: () => import('@/pages/modules/user/UserRegister.vue'),
    meta: { title: '註冊', blankLayout: true }
  },
]

// 統一放在 Layout 之下的 children
const routes = [
  {
    path: '/',
    component: Layout,
    children: [
      {
        path: '', // 預設首頁
        name: 'home',
        component: HomeView,
        meta: { title: '首頁' }
      },
      {
        path: 'about',
        name: 'about',
        component: () => import('@/pages/home/About.vue'),
        meta: { title: '關於我們' }
      },
      // 自動載入的模組路由
      ...childRoutes
    ]
  },

  // 這些頁面不會經過 Layout（變成獨立頁面）
  ...standaloneAuthRoutes,
  // 捕捉 404
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/pages/common/NotFound.vue'),
  },
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior() {
    return { left: 0, top: 0 }
  },
})

export default router