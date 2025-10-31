import CouponPage from '@/pages/modules/mkt/CouponPage.vue'

export default [
  {
    path: '/mkt/marquee',
    name: 'marquee',
    component: () => import('@/components/modules/mkt/Marquee.vue'),
    meta: { title: '跑馬燈' },
  },
  {
    path: '/mkt/coupons',
    name: 'mkt-coupons',
    component: CouponPage,
    meta: { title: '會員優惠券', requiresAuth: true },
  },
  {
    path: '/mkt/game',
    name: 'mkt-game',
    component: () => import('@/pages/modules/mkt/Game.vue'), // ✅ 指向頁面層
    meta: { title: '翻牌記憶遊戲', requiresAuth: true },
  },
]
