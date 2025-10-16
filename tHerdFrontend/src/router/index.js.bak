import { createRouter, createWebHistory } from 'vue-router';
import HomeView from '../pages/home/Home.vue';

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
  ...moduleRoutes
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes
});

export default router;
