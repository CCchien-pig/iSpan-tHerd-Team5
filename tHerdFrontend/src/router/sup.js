// src/router/sup.js

// 頁框/首頁
import SupHome from '../pages/modules/sup/SupHome.vue'
import SupWelcome from '../pages/modules/sup/SupWelcome.vue'

// 物流相關
import LogisticsFee from '../components/modules/sup/logistics/LogisticsFee.vue'
import LogisticsList from '../components/modules/sup/logistics/LogisticsList.vue'
import StoreMap from '../components/modules/sup/logistics/StoreMap.vue'

// 品牌 A-Z 總覽頁
const BrandsAZ = () => import('../pages/modules/sup/BrandsAZ.vue')

// 品牌詳頁（延遲載入）
const BrandDetail = () => import('../pages/modules/sup/BrandDetail.vue')

export default [
  // SUP 模組入口（有子頁）
  {
    path: '/sup',
    component: SupHome,
    children: [
      // 可開啟歡迎頁
      // { path: '', name: 'sup-welcome', component: SupWelcome },

      // 物流首頁/運費查詢
      { path: 'logistics-fee', name: 'sup-logistics-fee', component: LogisticsFee },

      // 物流商列表/配送資訊
      { path: 'logistics-list', name: 'sup-logistics-list', component: LogisticsList },

      // 門市地圖
      { path: 'store-map', name: 'sup-store-map', component: StoreMap },

      // ...其它 /sup/* 子頁可續加
    ],
    meta: { title: '物流門市運費查詢' },
  },

  // 供應商配送資訊頁（獨立）
  {
    path: '/sup/logistics-info',
    name: 'SupLogisticsInfo',
    component: () => import('../pages/modules/sup/LogisticsInfo.vue'),
    meta: { title: '配送資訊查詢' },
  },

  // 品牌 A-Z 總覽
  {
    path: '/brands',
    name: 'SupBrandsAZ',
    component: BrandsAZ,
    meta: { title: '品牌 A-Z 總覽' },
  },

  // 品牌詳頁（數字 Id 版（純 /brands/:brandId））
  {
    path: '/brands/:brandId(\\d+)',
    name: 'SupBrandDetail',
    component: () => import('../pages/modules/sup/BrandDetail.vue'),
    props: true,
    meta: { title: '品牌' },
  },

  // 可選：SEO 友善別名（/brands/allmax-1001）
  {
    path: '/brands/:slug-:brandId(\\d+)',
    name: 'SupBrandDetailSlug',
    component: () => import('../pages/modules/sup/BrandDetail.vue'),
    props: (route) => ({ brandId: Number(route.params.brandId), slug: route.params.slug }),
    meta: { title: '品牌' },
  },
]
