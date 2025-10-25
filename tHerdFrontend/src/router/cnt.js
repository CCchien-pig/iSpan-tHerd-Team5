// export default [
// {
//   path: '/cnt',
//   component: () => import('../pages/cnt/CntHome.vue'),
//   meta: { title: '內容管理' }
// }
// ];

// 📌 CNT 模組（健康中心 / 內容管理）路由設定
// 放置位置：src/router/cnt.js

// ✅ src/router/cnt.js

export default [
  {
    path: '/cnt',
    component: () => import('@/components/modules/cnt/CntLayout.vue'), // 新增的 Layout 元件
    meta: { title: '健康中心' },
    children: [
      {
        path: '',
        name: 'cnt-home',
        component: () => import('@/pages/modules/cnt/CntHome.vue'),
        meta: { title: '健康平台首頁' }
      },
      {
        path: 'articles',
        name: 'cnt-articles',
        component: () => import('../pages/modules/cnt/ArticleList.vue'),
        meta: { title: '健康文章' }
      },
      {
        path: 'article/:id',
        name: 'cnt-article-detail',
        component: () => import('../pages/modules/cnt/ArticleDetail.vue'),
        meta: { title: '文章內容' },
        props: true
      },
      {
        path: 'nutrition',
        name: 'cnt-nutrition',
        component: () => import('../pages/modules/cnt/NutritionList.vue'),
        meta: { title: '營養資料庫' }
      },
      {
        path: 'nutrition/:id',
        name: 'cnt-nutrition-detail',
        component: () => import('../pages/modules/cnt/NutritionDetail.vue'),
        meta: { title: '營養詳情' },
        props: true
      },
      {
        path: 'nutrition/compare',
        name: 'cnt-nutrition-compare',
        component: () => import('../pages/modules/cnt/NutritionCompare.vue'),
        meta: { title: '營養比較' }
      }
    ]
  }
];

