<template>
  <div class="d-flex min-vh-100 cnt-layout">
    <!-- 左側選單 -->
    <aside class="sidebar bg-light border-end p-3 shadow-sm">
      <h5 class="main-color-green-text mb-3">健康中心</h5>

      <ul class="list-unstyled">
        <!-- 📰 健康文章 -->
        <li>
          <button
            class="btn w-100 text-start fw-semibold mb-2 sidebar-btn"
            @click="toggle('articles')"
          >
            <i class="bi bi-journal-text me-1"></i> 健康文章
            <span class="float-end" :class="{ 'rotate-90': openMenu === 'articles' }">›</span>
          </button>

          <transition name="expand">
            <ul v-show="openMenu === 'articles'" class="submenu ps-3 small">
              <!-- 全部文章 -->
              <li>
                <router-link
                  :to="{ path: '/cnt/articles' }"
                  class="submenu-link"
                  :class="{ 'active-link': !route.query.categoryId }"
                >
                  全部文章
                </router-link>
              </li>

              <li v-for="cat in articleCats" :key="cat.id">
                <router-link
                  :to="{ path: '/cnt/articles', query: { categoryId: cat.id } }"
                  class="submenu-link d-flex justify-content-between align-items-center"
                  :class="{ 'active-link': route.query.categoryId == cat.id }"
                >
                  <span>{{ cat.name }}</span>
                  <small class="text-muted">({{ cat.articleCount }})</small>
                </router-link>
              </li>
            </ul>
          </transition>
        </li>

        <!-- 🧪 營養分析 -->
        <li class="mt-3">
          <button
            class="btn w-100 text-start fw-semibold mb-2 sidebar-btn"
            @click="toggle('nutrition')"
          >
            <i class="bi bi-clipboard-data me-1"></i> 營養分析
            <span class="float-end" :class="{ 'rotate-90': openMenu === 'nutrition' }">›</span>
          </button>

          <transition name="expand">
            <ul v-show="openMenu === 'nutrition'" class="submenu ps-3 small">
              <li>
                <router-link
                  to="/cnt/nutrition"
                  class="submenu-link"
                  active-class="active-link"
                >
                  單項食材分析
                </router-link>
              </li>
              <li>
                <router-link
                  to="/cnt/nutrition/compare"
                  class="submenu-link"
                  active-class="active-link"
                >
                  多項食材比較
                </router-link>
              </li>
            </ul>
          </transition>
        </li>
      </ul>
    </aside>

    <!-- 主內容 -->
    <main class="flex-grow-1 p-4 bg-white">
      <router-view />
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted } from "vue"
import { getArticleCategories } from "@/pages/modules/cnt/api/cntService" // ✅ 改用統一 service
import { useRoute } from 'vue-router'

const route = useRoute()
const openMenu = ref(null)
const articleCats = ref([])

// 🚀 從後端撈 CNT_PageType + 文章數量
async function loadCategories() {
  try {
    const { items } = await getArticleCategories()
    articleCats.value = items // 已自動排除首頁分類
  } catch (err) {
    console.error("載入分類失敗：", err)
  }
}

function toggle(menu) {
  openMenu.value = openMenu.value === menu ? null : menu
}

onMounted(loadCategories)
</script>

<style scoped>
.sidebar {
  width: 240px;
  transition: all 0.3s ease;
}
.sidebar-btn {
  color: #004c4c;
  background: #f9f9f9;
  border-radius: 8px;
}
.sidebar-btn:hover {
  background: #e7f4f2;
  color: #007078;
}
.submenu {
  overflow: hidden;
  padding-top: 4px;
}
.submenu-link {
  display: block;
  padding: 6px 10px;
  border-radius: 6px;
  color: #007078;
  text-decoration: none;
}
.submenu-link:hover {
  background: #d8f1ef;
}
.active-link {
  font-weight: 600;
  color: #004c4c !important;
  background: #c9ebe7;
}
.expand-enter-active,
.expand-leave-active {
  transition: all 0.25s ease-out;
}
.expand-enter-from,
.expand-leave-to {
  max-height: 0;
  opacity: 0;
}
.expand-enter-to,
.expand-leave-from {
  max-height: 200px;
  opacity: 1;
}
.rotate-90 {
  display: inline-block;
  transform: rotate(90deg);
  transition: transform 0.25s ease;
}
</style>
