import SupHome from '../pages/modules/sup/SupHome.vue'
import SupWelcome from '../pages/modules/sup/SupWelcome.vue'
import LogisticsFee from '../components/modules/sup/logistics/LogisticsFee.vue'
import LogisticsList from '../components/modules/sup/logistics/LogisticsList.vue'
import StoreMap from '../components/modules/sup/logistics/StoreMap.vue'
// 品牌總覽頁
const BrandsAZ = () => import('../pages/modules/sup/BrandsAZ.vue')
// 其他...

export default [
  {
    path: '/sup',
    component: SupHome,
    children: [
      // 首頁
      // { path: '', name: 'sup-welcome', component: SupWelcome },
      // 物流首頁
      { path: 'logistics-fee', name: 'sup-logistics-fee', component: LogisticsFee },
      // 物流商列表/配送資訊
      { path: 'logistics-list', name: 'sup-logistics-list', component: LogisticsList },
      // 門市地圖
      { path: 'store-map', name: 'sup-store-map', component: StoreMap },
      // ...其它子頁
    ],
    meta: { title: '物流門市運費查詢' },
  },

  // 供應商配送資訊頁
  {
    path: '/sup/logistics-info',
    name: 'SupLogisticsInfo',
    component: () => import('../pages/modules/sup/LogisticsInfo.vue'),
    meta: { title: '配送資訊查詢' },
  },

  // 品牌 A-Z 總覽頁
  {
    path: '/brands',
    name: 'SupBrandsAZ',
    component: () => import('../pages/modules/sup/BrandsAZ.vue'),
    meta: { title: '品牌 A-Z 總覽' },
  },
]

// {
//   path: '/sup',
//   component: () => import('../pages/sup/SupplierHome.vue'),
//   meta: { title: '供應商管理' }
// }
