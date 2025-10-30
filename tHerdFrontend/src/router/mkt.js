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
    meta: { title: '會員優惠券', requiresAuth: true }, // ✅ 必須登入
  },
]
