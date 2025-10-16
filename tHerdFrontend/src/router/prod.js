export default [
  {
    path: '/prod/search',
    name: 'search',
    component: () => import('../pages/modules/prod/ProductSearch.vue'),
    meta: { title: '商品查詢' }
  },
];
