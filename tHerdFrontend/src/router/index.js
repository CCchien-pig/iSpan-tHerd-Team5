import { createRouter, createWebHistory } from 'vue-router'
import Layout from '@/components/layout/Layout.vue'
import HomeView from '../pages/home/Home.vue'

// 自動載入 router/*.js（排除 index.js）
const modules = import.meta.glob('./!(index).js', { eager: true })
let moduleRoutes = []

Object.values(modules).forEach((mod) => {
  moduleRoutes.push(...(mod.default || []));
})

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
      ...moduleRoutes
    ]
  },
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