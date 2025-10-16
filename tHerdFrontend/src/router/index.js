import { createRouter, createWebHistory } from 'vue-router'
import Layout from '@/components/layout/Layout.vue'
import HomeView from '../pages/home/Home.vue'

// 自動載入 router/*.js（排除 index.js）
const modules = import.meta.glob('./!(index).js', { eager: true });
let moduleRoutes = [];

Object.values(modules).forEach((mod) => {
  moduleRoutes.push(...(mod.default || []));
});

const routes = [
  { path: '/', name: 'home', component: HomeView },
    {
      path: '/about',
      name: 'about',
      component: () => import('../pages/home/About.vue'),
    },
      {
    path: '/',
    component: Layout,
    children: [
      { path: '', name: 'home', component: HomeView },
      { path: 'about', name: 'about', component: () => import('../pages/home/About.vue') },
      ...moduleRoutes // 👈 放進 children
    ]
  },
  ...moduleRoutes
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes
});

export default router;
