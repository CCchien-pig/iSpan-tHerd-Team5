// src/router/ord.js
export default [
  {
    path: '/cart',
    name: 'Cart',
    component: () => import('@/pages/modules/ord/Cart.vue'),  
    meta: { 
      title: '購物車',
      module: 'ORD'
    }
  },
//   {
//     path: '/checkout',
//     name: 'Checkout',
//     component: () => import('@/pages/modules/ord/Checkout.vue'), 
//     meta: { 
//       title: '結帳',
//       module: 'ORD',
//       requiresAuth: true
//     }
//   },
//   {
//     path: '/order/:orderNo',
//     name: 'OrderDetail',
//     component: () => import('@/pages/modules/ord/OrderDetail.vue'), 
//     meta: { 
//       title: '訂單詳情',
//       module: 'ORD',
//       requiresAuth: true
//     }
//   },
//   {
//     path: '/orders',
//     name: 'OrderList',
//     component: () => import('@/pages/modules/ord/OrderList.vue'), 
//     meta: { 
//       title: '訂單列表',
//       module: 'ORD',
//       requiresAuth: true
//     }
//   }
]