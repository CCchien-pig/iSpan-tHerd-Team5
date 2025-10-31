export default [
  // {
  //   path: '/user',
  //   component: () => import('../pages/user/UserHome.vue'),
  //   meta: { title: '會員管理' }
  // },
  // {
  //   path: '/user/profile/:id(\\d+)',
  //   component: () => import('../pages/user/UserProfile.vue'),
  //   meta: { title: '會員資料' }
  // }
  {
    path: '/user/login',
    name: 'userlogin',
    component: () => import('@/pages/modules/user/UserLogin.vue'),
    meta: { title: '登入' },
  },
  {
    path: '/user/register',
    name: 'userregister',
    component: () => import('@/pages/modules/user/UserRegister.vue'),
    meta: { title: '註冊' },
  },
  {
    path: '/user/me',
    name: 'userme',
    component: () => import('@/pages/modules/user/UserMe.vue'),
    meta: { title: '我的帳戶', requiresAuth: false },
  },
];
