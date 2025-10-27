// src/router/cs.js
export default [
  {
    path: '/cs/faq',
    name: 'FaqSearch',
    component: () => import('@/pages/modules/cs/FaqSearch.vue'),
    meta: { title: '常見問題搜尋' }
  },
];
