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
        <!-- 基本資料卡（改用 profile 優先） -->
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

            <div class="avatar-actions">
              <el-button size="small" class="teal-reflect-button" @click="openFileDialog" :loading="uploading" :disabled="uploading">
                {{ uploading ? '上傳中…' : '上傳/更換大頭貼' }}
              </el-button>
              <el-button size="small" type="danger" plain @click="removeAvatar" :disabled="!avatarUrl || removing" :loading="removing">
                移除
              </el-button>
              <input ref="fileInput" class="d-none" type="file" accept="image/*" @change="onFileChange" />
              <div class="avatar-hint text-muted small">支援圖片檔，最大 5MB。</div>
            </div>

            <div class="base-info">
              <div class="hello">嗨，{{ displayEmail }}！</div>
              <div class="joined">用戶加入時間 {{ joinedAtText }}</div>
              <div class="meta">
                <span>姓名：{{ displayName }}</span>
                <span>會員編號：#{{ displayUserNumberId }}</span>
                <span>角色：{{ displayRoles }}</span>
              </div>
            </div>
          </div>
          <div>- - - - - - - - -</div>
          <div class="base-card__right">
            <el-button class="me-2 silver-reflect-button" @click="goHome" plain>回首頁</el-button>
            <el-button class="silver-reflect-button" @click="doLogout">登出</el-button>
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
                class="teal-reflect-button"
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
          <el-card class="feature" shadow="hover" role="button" tabindex="0" @click="todoFeature('訂單')">
            <div class="title">訂單</div>
            <div class="desc">跟蹤訂單、申請退貨、重新訂購。</div>
          </el-card>

          <el-card class="feature" shadow="hover" role="button" tabindex="0" @click="todoFeature('健康新知')">
            <div class="title">健康新知</div>
            <div class="desc">瞭解最新健康資訊，幫助您更知道該買什麼產品。</div>
          </el-card>

          <el-card class="feature" shadow="hover" role="button" tabindex="0" @click="todoFeature('促銷與優惠')">
            <div class="title">促銷與優惠</div>
            <div class="desc">查看全站優惠，領取活動折扣。</div>
          </el-card>
        </div>

        <!-- 猜你喜歡（容器） -->
        <!-- <section class="recommend">
          <h2>猜你喜歡</h2>
          <div class="carousel-placeholder">
            （此處保留你的輪播元件位，之後可接資料）
          </div>
        </section> -->
      </main>
    </div>
  </div>
</template>

<script setup>
import { computed, ref, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import MyAccountSidebar from '@/components/account/MyAccountSidebar.vue'
import { http } from '@/api/http'
import { ElMessage } from 'element-plus'

const router = useRouter()
const auth = useAuthStore()

// ============= 核心：避免直接用 token 快照 =============
const me = computed(() => auth.user || null)        // 仍保留 token 快照（授權/備援）
const profile = ref(null)                           // 權威資料（/user/me/detail）
const memberRank = ref(null)
const claiming = ref(false)

const avatarUrl = ref('')
const uploading = ref(false)
const removing  = ref(false)
const fileInput = ref(null)

// 顯示欄位一律以 profile 為優先，沒有再回退 me（token）
const displayEmail = computed(() => profile.value?.email || me.value?.email || '—')
const displayName = computed(() => {
  if (profile.value?.name && profile.value.name.trim()) return profile.value.name
  const ln = profile.value?.lastName || ''
  const fn = profile.value?.firstName || ''
  const combined = `${ln}${fn}`.trim()
  if (combined) return combined
  return me.value?.name || '—'
})
const displayUserNumberId = computed(() => profile.value?.userNumberId ?? me.value?.userNumberId ?? '—')
const displayRoles = computed(() => (me.value?.roles || []).join(', ') || '—') // 角色多半仍由 token 提供即可

const joinedAtText = computed(() => {
  const dt = profile.value && (profile.value.createdDate || profile.value.CreatedDate)
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

// me 有值 → 載入權威資料與等級
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

// ---- API：載入我的詳細資料（權威）並同步 auth.user.name，確保 header/側欄即時更新 ----
async function loadProfile() {
  try {
    const { data } = await http.get('/user/me/detail')

    // 最小映射：以 camelCase 為準
    const patched = {
      id: me.value.id,
      email: data.email ?? data.Email,
      // 後端若未回統一 name，就組合
      name: (data.name ?? data.Name) || `${data.LastName ?? data.lastName ?? ''}${data.FirstName ?? data.firstName ?? ''}`.trim(),
      userNumberId: data.userNumberId ?? data.UserNumberId,
      roles: me.value.roles || [],
      createdDate: data.createdDate ?? data.CreatedDate,
      memberRankId: data.memberRankId ?? data.MemberRankId,
      usedReferralCode: data.usedReferralCode ?? data.UsedReferralCode,
      phoneNumber: data.phoneNumber ?? data.PhoneNumber,
      twoFactorEnabled: data.twoFactorEnabled ?? data.TwoFactorEnabled,
      imgId: data.imgId ?? data.ImgId,
      gender: data.gender ?? data.Gender,
      address: data.address ?? data.Address,
      lastLoginDate: data.lastLoginDate ?? data.LastLoginDate,
      emailConfirmed: data.emailConfirmed ?? data.EmailConfirmed,
      isActive: data.isActive ?? data.IsActive,
      lastName: data.lastName ?? data.LastName,
      firstName: data.firstName ?? data.FirstName
    }

    profile.value = patched

    // 關鍵：若顯示姓名改了，立即同步到 auth store（避免 header 仍顯示舊值）
    if (auth?.setUser && me.value?.name !== patched.name) {
      auth.setUser({ ...auth.user, name: patched.name })
    }

    // 如果 token 內沒有 email（少見），同步補上（選擇性）
    if (auth?.setUser && me.value?.email !== patched.email && patched.email) {
      auth.setUser({ ...auth.user, email: patched.email })
    }

    // 載入頭像（第一次讀到 profile 才載）
    if (!avatarUrl.value) {
      await loadAvatar()
    }

    // console.debug('[UserMe] profile', profile.value)
  } catch (e) {
    profile.value = me.value
    console.error('[UserMe] profile FAIL', e?.response?.status, e?.response?.data || e)
  }
}

// 會員等級
async function loadMemberRank() {
  try {
    const rankId = profile.value?.memberRankId ?? me.value?.memberRankId
    if (!rankId) return
    const { data } = await http.get(`/user/member-ranks/${encodeURIComponent(rankId)}`)
    memberRank.value = {
      memberRankId: data.memberRankId ?? data.MemberRankId,
      rankName: data.rankName ?? data.RankName,
      totalSpentForUpgrade: data.totalSpentForUpgrade ?? data.TotalSpentForUpgrade,
      orderCountForUpgrade: data.orderCountForUpgrade ?? data.OrderCountForUpgrade,
      rebateRate: data.rebateRate ?? data.RebateRate,
      rankDescription: data.rankDescription ?? data.RankDescription,
      isActive: data.isActive ?? data.IsActive
    }
  } catch (e) {
    memberRank.value = null
    console.warn('[UserMe] memberRank FAIL', e?.response?.status, e?.response?.data || e)
  }
}

// 推薦碼領券
// const claiming = ref(false)
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

// Avatar
function openFileDialog() {
  if (fileInput.value) fileInput.value.click()
}
async function loadAvatar() {
  try {
    const { data } = await http.get('/user/avatar')
    avatarUrl.value = data && data.fileUrl ? `${data.fileUrl}?v=${Date.now()}` : ''
  } catch (err) {
    if (err?.response?.status !== 401) console.error('[UserMe] loadAvatar FAIL', err)
  }
}
async function onFileChange(e) {
  const input = e.target
  const file = input?.files?.[0]
  if (!file) return
  if (!file.type?.startsWith('image/')) {
    ElMessage.error('僅接受圖片檔案'); input.value = ''; return
  }
  if (file.size > 5 * 1024 * 1024) {
    ElMessage.error('檔案過大，請小於 5MB'); input.value = ''; return
  }
  const form = new FormData()
  form.append('file', file)
  uploading.value = true
  try {
    const { data } = await http.post('/user/avatar', form)
    avatarUrl.value = data && data.fileUrl ? `${data.fileUrl}?v=${Date.now()}` : ''
    ElMessage.success('大頭貼已更新')
  } catch (err) {
    ElMessage.error(err?.response?.data?.error || '上傳失敗')
  } finally {
    uploading.value = false
    if (input) input.value = ''
  }
}
async function removeAvatar() {
  if (!avatarUrl.value) return
  try {
    removing.value = true
    await http.delete('/user/avatar')
    avatarUrl.value = ''
    ElMessage.success('已移除大頭貼')
  } catch (err) {
    ElMessage.error(err?.response?.data?.error || '移除失敗')
  } finally {
    removing.value = false
  }
}

// 初次與 me 變動時皆嘗試載入頭像（保持原有行為）
onMounted(() => { if (me.value) loadAvatar() })
watch(() => me.value, (v) => { if (v) loadAvatar() })

// 功能磚
const featureToRoute = {
  '訂單': 'orders',
  '健康新知': 'cnt-home',
  '促銷與優惠': 'rewards',
}
function routeExists(name) { try { return router.hasRoute(name) } catch { return false } }
function goByName(name) { router.push(routeExists(name) ? { name } : { name: 'userme' }) }
function todoFeature(label) {
  const name = featureToRoute[label]
  if (name) goByName(name)
  else ElMessage.info(`「${label}」尚未開通`)
}

function goHome() { router.push({ name: 'home' }) }
async function doLogout() {
  alert('你已成功登出')
  await auth.logout()
  router.replace({ name: 'home' })
}
</script>


<style scoped>
.myaccount { max-width: 1200px; }

/* ===== 版面：維持原寬與位置；只把 sidebar 視覺往右推約 13px ===== */
.layout {
  display: grid;
  grid-template-columns: 300px 1fr;
  gap: 20px; /* 保持不變，確保主內容位置/寬度不動 */
}
.sidebar {
  min-width: 0;
  display: flex;          /* 使內部 myaccount-sidebar 能 height:100% */
  transform: translateX(100px);  /* ← 把 sidebar 往右移 2/3 的原間距（20px * 2/3 ≈ 13） */
}
.sidebar :deep(.myaccount-sidebar) {
  height: 100%;           /* 搭配子元件的 height:100%，等高主內容 */
}
.content { min-width: 0; }

/* ===== 色票與線條：與 Sidebar 一致 ===== */
:root {}
/* 可重用的色票（在 scoped 內直接用顏色，避免 :root 滲漏） */
.base-card,
.referral-card,
.feature {
  border: 1px solid rgb(77, 180, 193);
  border-radius: 6px;
  background: #fff;
}

/* 基本資料卡視覺微調 */
.base-card {
  display:flex; align-items:center; justify-content:space-between; padding:16px;
  /* 內側左緣加入與 sidebar active 類似的細線意象（更淡一點） */
  box-shadow: inset 3px 0 0 0 rgba(0, 112, 131, 0.2);
}
.base-card__left { display:flex; align-items:center; gap:16px; }

/* 頭像邊框與底色呼應側欄 */
.avatar {
  width:98px; height:98px; border-radius:50%; overflow:hidden;
  background: rgba(77, 180, 193, 0.06);
  border: 1px solid rgba(77, 180, 193, 0.35);
  display:flex; align-items:center; justify-content:center;
}
.avatar img { width:98px; height:98px; object-fit:cover; border-radius:50%; }

/* 文字色階與側欄一致 */
.base-info .hello { font-weight:600; margin-bottom:4px; color:#2c3e50; }
.base-info .joined { font-size:12px; color:#4a5568; margin-bottom:6px; }
.base-info .meta { display:flex; gap:14px; flex-wrap: wrap; font-size:14px; color:#333; }

/* 推薦卡：沿用淺青底與虛線邊 */
.referral-card { margin-top:16px; }
.referral-card__header { display:flex; align-items:baseline; gap:12px; margin-bottom:10px; }
.referral-card .title { font-weight:700; color:#2c3e50; }
.referral-card .desc { color:#333; }
.rank-name { font-weight:600; }
.rebate { color:#418a3b; }

.referral-card__body { display:flex; justify-content:space-between; gap:16px; flex-wrap:wrap; }
.code-box {
  background: rgba(77, 180, 193, 0.06);
  border: 1px dashed rgba(77, 180, 193, 0.5);
  border-radius: 8px;
  padding: 12px 16px;
  min-width: 260px;
}
.code-box .label { font-size:12px; color:#4a5568; }
.code-box .code { font-size:20px; font-weight:700; margin-top:4px; letter-spacing:1px; color:#2c3e50; }
.claim { display:flex; align-items:center; gap:12px; }

/* 功能磚：hover 與側欄 hover 呼應 */
.feature-grid { margin-top:20px; display:grid; grid-template-columns: repeat(3, 1fr); gap:16px; }
.feature { cursor:pointer; transition: transform .12s ease, box-shadow .12s ease, background-color .12s ease, border-color .12s ease; }
.feature .title { font-weight:700; margin-bottom:6px; color:#2c3e50; }
.feature .desc { color:#4a5568; font-size:14px; }
.feature:hover {
  background-color: rgba(77, 180, 193, 0.08);
  border-color: rgba(77, 180, 193, 0.35);
  transform: translateY(-1px);
}
.feature:focus { outline: 2px solid #409eff; }

/* 其他原樣式沿用 */
.breadcrumb { display:flex; gap:8px; color:#666; font-size:14px; margin-bottom:12px;transform: translateX(100px); }
.breadcrumb a { color:#4183c4; }

/* 猜你喜歡容器（若日後開啟），沿用側欄的淡底與虛線 */
.carousel-placeholder {
  background: rgba(77, 180, 193, 0.06);
  border: 1px dashed rgba(77, 180, 193, 0.35);
  padding:24px; text-align:center; color:#4a5568;
}

@media (max-width: 992px) {
  .layout { grid-template-columns: 1fr; gap: 16px; }
  .sidebar { order:2; transform: none; }
  .content { order:1; }
}

</style>
<style src="@/assets/main.css"></style>