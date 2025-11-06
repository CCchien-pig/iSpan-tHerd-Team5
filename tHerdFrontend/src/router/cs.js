// src/router/cs.js
export default [
  {
    path: '/cs/faq',
    name: 'FaqSearch',
    component: () => import('@/pages/modules/cs/FaqSearch.vue'),
    meta: { title: '常見問題搜尋' }
  },
    {
    path: '/cs/ticket',               
    name: 'TicketCreate',
    component: () => import('@/pages/modules/cs/TicketCreate.vue'),
    meta: { title: '聯絡我們' }      // 可在標題或麵包屑顯示
  },
    {
    path: '/cs/ticket/success',
    name: 'CsTicketSuccess',
    component: () => import('@/pages/modules/cs/TicketSuccess.vue'),
    meta: { title: '送出成功' }
  },
  {
    path: '/cs/tickets',
    name: 'TicketList',
    component: () => import('@/pages/modules/cs/TicketList.vue'),
    meta: { title: '我的工單' ,requiresAuth: true}
  },
  {
    path: '/cs/tickets/:id',
    name: 'TicketDetail',
    component: () => import('@/pages/modules/cs/TicketDetail.vue'),
    meta: { title: '工單詳情', requiresAuth: true }
  },
  {
  path: '/cs/chat',
  name: 'CsChat',
  component: () => import('@/pages/modules/cs/CsChat.vue'),
  meta: { title: '智能客服' }
},
{
  path: '/cs/live-chat',
  name: 'LiveChat',
  component: () => import('@/pages/modules/cs/LiveChat.vue'),
  meta: { title: '即時客服' }
},

];
