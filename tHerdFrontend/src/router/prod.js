export default [
  {
    path: '/prod/search',
    name: 'search',
    component: () => import('@/pages/modules/prod/ProductSearchExample.vue'),
    meta: { title: '商品查詢' },
  },
  {
    path: '/prod/products/:id',
    name: 'product-detail',
    component: () => import('@/pages/modules/prod/ProductDetail.vue'),
  },
  {
  path: '/prod/products/search',
  name: 'product-main-search',
  component: () => import('@/pages/modules/prod/ProductMainSearch.vue')
  },
]
