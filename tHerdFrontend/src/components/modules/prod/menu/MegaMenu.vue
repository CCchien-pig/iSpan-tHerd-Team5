<template>
  <transition name="fade">
    <div
      v-if="visible"
      class="mega-menu-wrapper"
      @mouseenter="$emit('mouseenter')"
      @mouseleave="$emit('mouseleave')"
    >
      <div class="mega-menu shadow-lg bg-white">
        <!-- 🌀 載入中 -->
        <div v-if="isLoading" class="text-center p-4">載入中...</div>

        <!-- ❌ 錯誤 -->
        <div v-else-if="error" class="text-danger p-4">{{ error }}</div>

        <!-- ✅ 顯示分類欄 -->
        <div v-else-if="data" class="menu-columns">
          <div
            v-for="col in data.columns"
            :key="col.title"
            class="menu-column"
          >
            <h4>
              <router-link
                v-if="col.url"
                :to="col.url"
                class="brand-link fw-bold"
                @click="$emit('close')"
              >
                {{ col.title }}
              </router-link>
              <span v-else class="brand-link fw-bold text-muted">
                {{ col.title }}
              </span>
            </h4>

            <ul>
              <li v-for="item in col.items" :key="item.id">
                <router-link
                  v-if="item.url"
                  :to="item.url"
                  class="brand-link"
                  @click="$emit('close')"
                >
                  {{ item.name }}
                </router-link>
                <span v-else class="brand-link text-muted">{{ item.name }}</span>
              </li>
            </ul>
          </div>

          <!-- 熱門品牌 (選擇性) -->
          <div class="menu-column brands" v-if="brands?.length">
            <h4>熱門品牌</h4>
            <div class="brand-list">
              <img
                v-for="b in brands"
                :key="b.id"
                :src="b.logoUrl"
                :alt="b.name"
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  </transition>
</template>

<script setup>
defineProps({
  visible: Boolean,        // 是否顯示
  isLoading: Boolean,      // 是否載入中
  error: String,           // 錯誤訊息（選擇性）
  data: Object,            // 主資料 { columns: [...] }
  brands: Array            // 品牌清單（可選）
})

defineEmits(['mouseenter', 'mouseleave', 'close'])
</script>

<style scoped>
/* ====== 主體設定 ====== */
.mega-menu-wrapper {
  position: absolute;
  top: 100%;
  left: 50%;
  transform: translateX(-50%);
  width: 80vw;
  max-width: 1200px;
  min-width: 700px;
  z-index: 9999;
}

.mega-menu {
  background: #fff;
  border-top: 3px solid rgb(77, 180, 193);
  border-radius: 12px;
  box-shadow: 0 8px 25px rgba(0, 0, 0, 0.15);
  padding: 1.5rem 2rem;
  animation: fadeDown 0.25s ease forwards;

  /* ✅ 固定高度 + 可滾動 */
  max-height: 400px;
  overflow-y: auto;

  /* ✅ 讓內部 sticky 生效 */
  position: relative;
}

/* 滾動列樣式（美化用） */
.mega-menu::-webkit-scrollbar {
  width: 8px;
}
.mega-menu::-webkit-scrollbar-thumb {
  background: rgba(77, 180, 193, 0.6);
  border-radius: 4px;
}
.mega-menu::-webkit-scrollbar-thumb:hover {
  background: rgba(0, 112, 131, 0.8);
}

/* ====== 欄位排列 ====== */
.menu-columns {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  gap: 20px;
}

.menu-column {
  flex: 1 1 22%;
  min-width: 200px;
}

/* ✅ Sticky 標題（凍結窗格效果） */
.menu-column h4 {
  font-weight: bold;
  margin-bottom: 10px;
  color: rgb(77, 180, 193);
  background: white;
  position: sticky;
  top: 0;
  z-index: 5;
  padding: 8px 0;
  border-bottom: 1px solid #e0e0e0;
}

/* ====== 品牌區 ====== */
.brand-list {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 15px;
  margin-top: 10px;
}
.brand-list img {
  width: 80px;
  object-fit: contain;
  filter: grayscale(20%);
  transition: all 0.3s ease;
}
.brand-list img:hover {
  filter: none;
  transform: scale(1.05);
}

/* ====== 連結樣式 ====== */
.brand-link {
  display: inline-block;
  padding: 0.25rem 0.5rem;
  font-size: 0.95rem;
  color: #004d40;
  text-decoration: none;
  border-radius: 4px;
  transition: all 0.2s ease;
  cursor: pointer;
}
.brand-link:hover {
  color: rgb(0, 112, 131);
  background-color: rgba(0, 112, 131, 0.05);
  text-decoration: underline;
  padding-left: 0.75rem;
}

/* ====== 動畫 ====== */
@keyframes fadeDown {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
