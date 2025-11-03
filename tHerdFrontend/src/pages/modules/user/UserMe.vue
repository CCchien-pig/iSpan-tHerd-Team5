<!-- /src/pages/modules/user/UserMe.vue -->
<template>
  <div class="myaccount container py-4">
    <!-- 麵包屑 -->
    <div class="breadcrumb">
      <router-link :to="{ name: 'userme' }">我的帳戶</router-link>
      <span>儀表板</span>
    </div>

    <div v-if="!me" class="alert alert-warning">尚未登入</div>

    <div v-else class="layout">
      <!-- 左側側邊欄 -->
      <aside class="sidebar">
        <MyAccountSidebar />
      </aside>

      <!-- 右側主內容 -->
      <main class="content">
        <!-- 基本資料卡 -->
        <el-card class="base-card" shadow="never">
          <div class="base-card__left">
            <div class="avatar">
              <svg v-if="!avatarUrl" viewBox="0 0 56 56">
                <circle cx="28" cy="28" r="28" fill="#FFF"></circle>
                <path fill="#D8D8D8"
                  d="M28 0c15.464 0 28 12.536 28 28S43.464 56 28 56 0 43.464 0 28 12.536 0 28 0zm-.039 29.647c-13.896 0-21.864 7.56-17.5 12.666 4.365 5.105 10.278 8.443 17.5 8.443 7.223 0 13.136-3.338 17.5-8.443 4.365-5.105-3.603-12.666-17.5-12.666zM27.96 5.56c-4.64 0-8.4 3.898-8.4 8.707 0 4.808 3.76 9.588 8.4 9.588 4.639 0 8.4-4.78 8.4-9.588 0-4.809-3.761-8.707-8.4-8.707z" />
              </svg>
              <img v-else :src="avatarUrl" alt="profile" />
            </div>
            <div class="base-info">
              <div class="hello">嗨，{{ me.email }}！</div>
              <div class="joined">用戶加入時間 {{ joinedAtText }}</div>
              <div class="meta">
                <span>姓名：{{ me.name }}</span>
                <span>會員編號：#{{ me.userNumberId }}</span>
                <span>角色：{{ (me.roles || []).join(', ') || '—' }}</span>
              </div>
            </div>
          </div>
          <div>- - - - - - - - -</div>
          <div class="base-card__right">
            <el-button class="me-2" @click="goHome" plain>回首頁</el-button>
            <el-button type="danger" @click="doLogout">登出</el-button>
          </div>
        </el-card>

        <!-- 等級 / 回饋率 / 使用推薦碼 & 領券卡 -->
        <el-card class="referral-card" shadow="never">
          <div class="referral-card__header">
            <div class="title">會員等級</div>
            <div class="desc">
              <span class="rank-name">{{ memberRank?.rankName || '—' }}</span>
              <span v-if="memberRank" class="rebate">（回饋 {{ memberRank.rebateRate }}%）</span>
            </div>
          </div>

          <div class="referral-card__body">
            <div class="code-box">
              <div class="label">已使用的推薦碼</div>
              <div class="code">{{ profile?.usedReferralCode || '—' }}</div>
            </div>

            <div class="claim">
              <el-button
                type="primary"
                :disabled="!profile?.usedReferralCode || claiming"
                :loading="claiming"
                @click="claimReferralCoupon"
              >
                領取推薦碼優惠券
              </el-button>
              <div class="hint">＊需先有「已使用的推薦碼」才可領取。</div>
            </div>
          </div>
        </el-card>

        <!-- 功能磚 -->
        <div class="feature-grid">
          <el-card class="feature" shadow="hover" @click="todoFeature('訂單')">
            <div class="title">訂單</div>
            <div class="desc">跟蹤訂單、申請退貨、重新訂購、撰寫評價。</div>
          </el-card>

          <el-card class="feature" shadow="hover" @click="todoFeature('定期自動送貨享優惠')">
            <div class="title">定期自動送貨享優惠</div>
            <div class="desc">設定經常性訂購，輕鬆補貨更優惠。</div>
          </el-card>

          <el-card class="feature" shadow="hover" @click="todoFeature('促銷與優惠')">
            <div class="title">促銷與優惠</div>
            <div class="desc">查看全站優惠，領取活動折扣。</div>
          </el-card>

          <el-card class="feature" shadow="hover" @click="todoFeature('我的清單')">
            <div class="title">我的清單</div>
            <div class="desc">保留喜愛商品，補貨即買。</div>
          </el-card>

          <el-card class="feature" shadow="hover" @click="todoFeature('聯盟計畫')">
            <div class="title">聯盟計畫</div>
            <div class="desc">成為聯盟成員，越分享越賺！</div>
          </el-card>

          <el-card class="feature" shadow="hover" @click="todoFeature('地址簿')">
            <div class="title">地址簿</div>
            <div class="desc">集中管理你的收貨地址。</div>
          </el-card>
        </div>

        <!-- 猜你喜歡（容器） -->
        <section class="recommend">
          <h2>猜你喜歡</h2>
          <div class="carousel-placeholder">
            （此處保留你的輪播元件位，之後可接資料）
          </div>
        </section>
      </main>
    </div>
  </div>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import MyAccountSidebar from '@/components/account/MyAccountSidebar.vue'
import { http } from '@/api/http'
import { ElMessage } from 'element-plus'

const router = useRouter()
const auth = useAuthStore()
const me = computed(() => auth.user || null)

const profile = ref(null)
const memberRank = ref(null)
const claiming = ref(false)
const avatarUrl = ref(null)

const joinedAtText = computed(() => {
  const dt = profile.value && profile.value.createdDate
  if (!dt) return '—'
  try {
    const d = new Date(dt)
    const mm = String(d.getMonth() + 1).padStart(2, '0')
    const dd = String(d.getDate()).padStart(2, '0')
    return `${d.getFullYear()} 年 ${mm} 月 ${dd} 日`
  } catch {
    return '—'
  }
})

watch(
  () => me.value,
  async (v) => {
    if (!v) {
      profile.value = null
      memberRank.value = null
      return
    }
    await loadProfile()
    await loadMemberRank()
  },
  { immediate: true }
)

async function loadProfile() {
  try {
    // 後端可能回 PascalCase，這裡做最小映射 → camelCase
    const { data } = await http.get('/user/me/detail')

    const patched = {
      // 從 me 帶入既有 camelCase
      id: me.value.id,
      email: me.value.email,
      name: me.value.name,
      userNumberId: me.value.userNumberId,
      roles: me.value.roles || [],
      // 從 API 轉成 camelCase（最小必要欄位）
      createdDate: data.CreatedDate ?? data.createdDate,
      memberRankId: data.MemberRankId ?? data.memberRankId,
      usedReferralCode: data.UsedReferralCode ?? data.usedReferralCode,
      // 如還有其他欄位要用，再逐一加對應即可
      phoneNumber: data.PhoneNumber ?? data.phoneNumber,
      twoFactorEnabled: data.TwoFactorEnabled ?? data.twoFactorEnabled,
      imgId: data.ImgId ?? data.imgId,
      gender: data.Gender ?? data.gender,
      address: data.Address ?? data.address,
      lastLoginDate: data.LastLoginDate ?? data.lastLoginDate,
      emailConfirmed: data.EmailConfirmed ?? data.emailConfirmed,
      isActive: data.IsActive ?? data.isActive
    }

    profile.value = patched
    console.debug('[UserMe] profile', profile.value)
  } catch (e) {
    profile.value = me.value
    console.error('[UserMe] profile FAIL', e?.response?.status, e?.response?.data || e)
  }
}

async function loadMemberRank() {
  try {
    const rankId = profile.value?.memberRankId ?? me.value?.memberRankId
    if (!rankId) return

    const { data } = await http.get(`/user/member-ranks/${encodeURIComponent(rankId)}`)

    // 後端 MemberRankDto 也可能是 PascalCase → 映射成 camelCase
    memberRank.value = {
      memberRankId: data.MemberRankId ?? data.memberRankId,
      rankName: data.RankName ?? data.rankName,
      totalSpentForUpgrade: data.TotalSpentForUpgrade ?? data.totalSpentForUpgrade,
      orderCountForUpgrade: data.OrderCountForUpgrade ?? data.orderCountForUpgrade,
      rebateRate: data.RebateRate ?? data.rebateRate,
      rankDescription: data.RankDescription ?? data.rankDescription,
      isActive: data.IsActive ?? data.isActive
    }
  } catch (e) {
    memberRank.value = null
    console.warn('[UserMe] memberRank FAIL', e?.response?.status, e?.response?.data || e)
  }
}

async function claimReferralCoupon() {
  if (!profile.value || !profile.value.usedReferralCode) return
  claiming.value = true
  try {
    const { data } = await http.post('/mkt/referral/claim', { code: profile.value.usedReferralCode })
    ElMessage.success(`已領取推薦優惠券（#${data.couponId} / ${data.couponName}），可至錢包/結帳使用`)
  } catch (err) {
    const msg = err?.response?.data?.error || err?.response?.data?.message
    ElMessage.error(msg || '領取失敗，請稍後再試')
  } finally {
    claiming.value = false
  }
}

// 功能磚暫不導路由：僅提示
function todoFeature(label) {
  ElMessage?.info?.(`「${label}」尚未開通`)
}

function goHome() {
  router.push({ name: 'home' })
}

async function doLogout() {
  alert('你已成功登出')
  await auth.logout()
  router.replace({ name: 'home' })
}
</script>

<style scoped>
.myaccount { max-width: 1200px; }
.breadcrumb { display:flex; gap:8px; color:#666; font-size:14px; margin-bottom:12px; }
.breadcrumb a { color:#4183c4; }
.layout { display: grid; grid-template-columns: 300px 1fr; gap: 20px; }
.sidebar { min-width: 0; }
.content { min-width: 0; }

.base-card { display:flex; align-items:center; justify-content:space-between; padding:16px; }
.base-card__left { display:flex; align-items:center; gap:16px; }
.avatar { width:98px; height:98px; border-radius:50%; overflow:hidden; background:#f5f5f5; display:flex; align-items:center; justify-content:center; }
.avatar img { width:98px; height:98px; object-fit:cover; border-radius:50%; }
.base-info .hello { font-weight:600; margin-bottom:4px; }
.base-info .joined { font-size:12px; color:#666; margin-bottom:6px; }
.base-info .meta { display:flex; gap:14px; flex-wrap: wrap; font-size:14px; color:#333; }

.referral-card { margin-top:16px; }
.referral-card__header { display:flex; align-items:baseline; gap:12px; margin-bottom:10px; }
.referral-card .title { font-weight:700; }
.referral-card .desc { color:#333; }
.rank-name { font-weight:600; }
.rebate { color:#418a3b; }

.referral-card__body { display:flex; justify-content:space-between; gap:16px; flex-wrap:wrap; }
.code-box { background:#f7faf7; border:1px solid #e2efe2; border-radius:8px; padding:12px 16px; min-width: 260px; }
.code-box .label { font-size:12px; color:#666; }
.code-box .code { font-size:20px; font-weight:700; margin-top:4px; letter-spacing:1px; }
.claim { display:flex; align-items:center; gap:12px; }

.feature-grid { margin-top:20px; display:grid; grid-template-columns: repeat(3, 1fr); gap:16px; }
.feature { cursor:pointer; }
.feature .title { font-weight:700; margin-bottom:6px; }
.feature .desc { color:#555; font-size:14px; }

.recommend { margin-top:28px; }
.carousel-placeholder { background:#fafafa; border:1px dashed #ddd; padding:24px; text-align:center; color:#999; }

@media (max-width: 992px) {
  .layout { grid-template-columns: 1fr; }
  .sidebar { order:2; }
  .content { order:1; }
}
</style>
