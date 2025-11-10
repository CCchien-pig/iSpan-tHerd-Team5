export default [
  {
    path: '/prod/search',
    redirect: { name: 'product-main-search' },
  },
  {
    path: '/prod/products/:id',
    name: 'product-detail',
    component: () => import('@/pages/modules/prod/ProductDetail.vue'),
    meta: { title: '商品詳情' },
  },
  {
    path: '/prod/products/search',
    name: 'product-main-search',
    component: () => import('@/pages/modules/prod/ProductMainSearch.vue'),
    meta: { title: '商品搜尋' },
  },
  // 新增這一段：支援 SEO URL
  {
    path: '/products/:slug',
    name: 'product-type-search',
    component: () => import('@/pages/modules/prod/ProductMainSearch.vue'),
    props: true, // 讓 params 自動傳進組件
    meta: { title: '商品分類搜尋' },
  },
  {
    path: '/prod/products/hot',
    name: 'product-hot-rank',
    component: () => import('@/pages/modules/prod/ProductHotRank.vue'),
    meta: { title: '暢銷排名' },
  },
]
