<!-- /src/pages/modules/user/AccountInfo.vue -->
<template>
  <div class="container py-4 account-page">
    <div class="breadcrumb">
      <router-link :to="{ name: 'userme' }">我的帳戶</router-link>
      <span>帳號資訊</span>
    </div>

    <div class="layout">
      <aside class="sidebar">
        <MyAccountSidebar />
      </aside>

      <main class="content">
        <h2 class="title">帳號資訊</h2>

        <!-- 會員編號（唯讀） -->
        <el-card shadow="never" class="card">
          <div class="row">
            <div class="icon"><i class="bi bi-person-badge"></i></div>
            <div class="field">
              <div class="label">會員編號</div>
              <div class="value">#{{ form.userNumberId || '—' }}</div>
            </div>
          </div>
        </el-card>

        <!-- 基本資料（可編輯） -->
        <el-card shadow="never" class="card">
          <div class="row">
            <div class="icon"><i class="bi bi-person"></i></div>
            <div class="field flex-1">
              <div class="label">個人資料</div>
              <el-form :model="form" :rules="rules" ref="profileForm" label-width="96px" class="mt-2">
                <el-form-item label="姓氏" prop="lastName">
                  <el-input v-model="form.lastName" placeholder="請輸入姓氏" />
                </el-form-item>
                <el-form-item label="名字" prop="firstName">
                  <el-input v-model="form.firstName" placeholder="請輸入名字" />
                </el-form-item>
                <el-form-item label="電子郵件" prop="email">
                  <el-input v-model="form.email" placeholder="email" disabled />
                </el-form-item>
                <el-form-item label="手機號碼" prop="phoneNumber">
                  <el-input v-model="form.phoneNumber" placeholder="0912-345-678" />
                </el-form-item>
                <el-form-item label="住址" prop="address">
                  <el-input v-model="form.address" placeholder="地址" />
                </el-form-item>
                <el-form-item label="性別" prop="gender">
                  <el-select v-model="form.gender" placeholder="請選擇">
                    <el-option label="男" value="男" />
                    <el-option label="女" value="女" />
                    <el-option label="不透露" value="不透露" />
                  </el-select>
                </el-form-item>
                <el-form-item label="生日" prop="birthDate">
                  <el-date-picker v-model="form.birthDate" type="date" placeholder="選擇日期" value-format="YYYY-MM-DD" />
                </el-form-item>

                <el-form-item>
                  <el-button type="primary" :loading="saving" @click="saveProfile">儲存變更</el-button>
                </el-form-item>
              </el-form>
            </div>
          </div>
        </el-card>

        <!-- 變更密碼（需舊密碼） -->
        <el-card shadow="never" class="card">
          <div class="row">
            <div class="icon"><i class="bi bi-shield-lock"></i></div>
            <div class="field flex-1">
              <div class="label">密碼</div>
              <el-form :model="pwd" :rules="pwdRules" ref="pwdForm" label-width="96px" class="mt-2">
                <el-form-item label="目前密碼" prop="oldPassword">
                  <el-input v-model="pwd.oldPassword" type="password" autocomplete="current-password" />
                </el-form-item>
                <el-form-item label="新密碼" prop="newPassword">
                  <el-input v-model="pwd.newPassword" type="password" autocomplete="new-password" />
                </el-form-item>
                <el-form-item label="確認新密碼" prop="confirmNewPassword">
                  <el-input v-model="pwd.confirmNewPassword" type="password" autocomplete="new-password" />
                </el-form-item>

                <el-form-item>
                  <el-button type="primary" :loading="changing" @click="changePassword">變更密碼</el-button>
                </el-form-item>
              </el-form>
            </div>
          </div>
        </el-card>

        <!-- 國家與語言（展示/未實作） -->
        <!-- <el-card shadow="never" class="card">
          <div class="row">
            <div class="icon"><i class="bi bi-translate"></i></div>
            <div class="field">
              <div class="label">國家與語言偏好</div>
              <div class="value">台灣 · 繁體中文</div>
            </div>
          </div>
        </el-card> -->

        <div class="mt-3">
          <!-- <router-link class="delete-link" :to="{ name: 'accountDelete' }">刪除帳戶 &gt;</router-link> -->
           <el-button type="danger" link class="delete-link" :loading="deleting" @click="confirmDeleteAccount">
    刪除帳戶 >
  </el-button>
        </div>
      </main>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { ElMessage,ElMessageBox  } from 'element-plus'
import { useRouter } from 'vue-router'
import { http } from '@/api/http'
import MyAccountSidebar from '@/components/account/MyAccountSidebar.vue'
import { useAuthStore } from '@/stores/auth'

const profileForm = ref(null)
const pwdForm = ref(null)
const saving = ref(false)
const changing = ref(false)
const deleting = ref(false)
const auth = useAuthStore()
const router = useRouter()


const form = reactive({
  userNumberId: null,
  lastName: '',
  firstName: '',
  email: '',
  phoneNumber: '',
  address: '',
  gender: '',
  birthDate: '', // YYYY-MM-DD
})

const rules = {
  lastName: [{ required: true, message: '請輸入姓氏', trigger: 'blur' }],
  firstName: [{ required: true, message: '請輸入名字', trigger: 'blur' }],
  phoneNumber: [{ min: 6, message: '電話長度不正確', trigger: 'blur' }],
}

const pwd = reactive({
  oldPassword: '',
  newPassword: '',
  confirmNewPassword: '',
})
const pwdRules = {
  oldPassword: [{ required: true, message: '請輸入目前密碼', trigger: 'blur' }],
  newPassword: [{ required: true, message: '請輸入新密碼', trigger: 'blur' }, { min: 8, message: '至少 8 碼', trigger: 'blur' }],
  confirmNewPassword: [
    { required: true, message: '請再次輸入新密碼', trigger: 'blur' },
    { validator: (_, v, cb) => (v !== pwd.newPassword ? cb(new Error('兩次新密碼不一致')) : cb()), trigger: 'blur' },
  ],
}

async function loadDetail() {
  try {
    const { data } = await http.get('/user/me/detail')
    form.userNumberId = data.userNumberId ?? data.UserNumberId
    form.lastName     = data.lastName ?? data.LastName ?? ''
    form.firstName    = data.firstName ?? data.FirstName ?? ''
    form.email        = data.email ?? data.Email ?? ''
    form.phoneNumber  = data.phoneNumber ?? data.PhoneNumber ?? ''
    form.address      = data.address ?? data.Address ?? ''
    form.gender       = data.gender ?? data.Gender ?? ''
    // 後端回 DateTime? → 取 yyyy-MM-dd
    const birth = data.birthDate ?? data.BirthDate
    form.birthDate = birth ? (typeof birth === 'string' ? birth.slice(0, 10) : '') : ''
  } catch (err) {
    ElMessage.error(err?.response?.data?.error || '載入失敗')
    console.error('[AccountInfo] load', err)
  }
}

async function saveProfile() {
  await profileForm.value?.validate()
  saving.value = true
  try {
    // 允許更新的欄位（camelCase）
    const payload = {
      lastName: form.lastName,
      firstName: form.firstName,
      phoneNumber: form.phoneNumber,
      address: form.address,
      gender: form.gender,
      birthDate: form.birthDate || null,
    }
    await http.patch('/user/me', payload)
    ElMessage.success('個人資料已更新')
  } catch (err) {
    ElMessage.error(err?.response?.data?.error || '更新失敗')
    console.error('[AccountInfo] save', err)
  } finally {
    saving.value = false
  }
}

async function changePassword() {
  await pwdForm.value?.validate()
  changing.value = true
  try {
    await http.post('/user/change-password', {
      oldPassword: pwd.oldPassword,
      newPassword: pwd.newPassword,
    })
    ElMessage.success('密碼已變更')
    pwd.oldPassword = ''
    pwd.newPassword = ''
    pwd.confirmNewPassword = ''
  } catch (err) {
    ElMessage.error(err?.response?.data?.error || '變更失敗')
    console.error('[AccountInfo] changePassword', err)
  } finally {
    changing.value = false
  }
}

async function confirmDeleteAccount() {
  try {
    await ElMessageBox.confirm(
      '此動作將刪除您的帳戶與個人資料（通常不可復原）。是否繼續？',
      '刪除帳戶確認',
      {
        confirmButtonText: '刪除',
        cancelButtonText: '取消',
        type: 'warning',
        autofocus: false,
        confirmButtonClass: 'el-button--danger'
      }
    )

    deleting.value = true

    await http.delete('/user/me')

    ElMessage.success('帳戶已刪除')
    if (auth && typeof auth.logout === 'function') {
      await auth.logout()
    }
    router.replace({ name: 'home' })
  } catch (err) {
    // 使用者取消
    if (err === 'cancel' || err === 'close') {
      ElMessage.info('已取消刪除')
      return
    }

    // 不用 ?.，改成防禦式取值
    let msg = '刪除失敗'
    if (err && err.response && err.response.data && err.response.data.error) {
      msg = err.response.data.error
    } else if (err && err.message) {
      msg = err.message
    }
    ElMessage.error(msg)
    console.error('[AccountInfo] delete account FAIL', err)
  } finally {
    deleting.value = false
  }
}

onMounted(loadDetail)
</script>

<style scoped>
.account-page { max-width: 1200px; }
.breadcrumb { display:flex; gap:8px; color:#666; font-size:14px; margin-bottom:12px; }
.breadcrumb a { color:#4183c4; }
.layout { display:grid; grid-template-columns: 300px 1fr; gap:20px; }
.sidebar { min-width:0; }
.content { min-width:0; }
.title { font-size:22px; font-weight:700; margin-bottom:12px; }

.card { margin-bottom:16px; }
.row { display:flex; align-items:flex-start; gap:16px; }
.icon { width:36px; display:flex; align-items:center; justify-content:center; font-size:20px; color:#666; }
.field .label { font-weight:700; margin-bottom:4px; }
.value { color:#333; }

.delete-link { color:#d9534f; }
.mt-2 { margin-top:8px; }
.mt-3 { margin-top:12px; }

@media (max-width: 992px) {
  .layout { grid-template-columns: 1fr; }
}
</style>
