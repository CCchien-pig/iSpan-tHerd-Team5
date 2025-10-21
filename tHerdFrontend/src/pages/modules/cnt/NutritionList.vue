<template>
  <div class="container py-4">

    <!-- 🏷 頁面標題 -->
    <h2 class="mb-4 main-color-green-text">營養資料庫</h2>

    <!-- 🔍 搜尋區 -->
    <div class="mb-4">
      <input
        type="text"
        class="form-control"
        placeholder="搜尋食材名稱（例如：雞胸肉、鮭魚、蘋果）"
        v-model="searchQuery"
      />
    </div>

    <!-- 🔽 滾動定位起點（新增） -->
    <div id="nutrition-list-start"></div>

    <!-- 🧾 食材清單 -->
    <div v-for="food in filteredFoods" :key="food.id" class="row py-3 border-bottom align-items-center">
      <!-- 食材名稱與描述 -->
      <div class="col-md-8">
        <h5 class="fw-bold">{{ food.name }}</h5>
        <p class="text-muted small">{{ food.description }}</p>
      </div>

      <!-- 操作按鈕 -->
      <div class="col-md-4 text-md-end text-start">
        <router-link
          :to="`/cnt/nutrition/${food.slug}-${food.id}`"
          class="btn btn-outline-primary btn-sm me-2"
        >
          查看營養 ➜
        </router-link>
        <button class="btn btn-outline-success btn-sm" @click="addToCompare(food)">
          加入比較
        </button>
      </div>
    </div>

    <!-- ⛔ 無資料 -->
    <div v-if="filteredFoods.length === 0" class="text-center text-muted py-5">
      找不到相關食材
    </div>
  </div>
</template>

<script>
export default {
  name: 'NutritionList',
  data() {
    return {
      searchQuery: '',
      // 🧪 假資料
      foods: [
        { id: 1, name: '鮭魚', slug: 'salmon', description: '富含 Omega-3 的高營養食材' },
        { id: 2, name: '蘋果', slug: 'apple', description: '含膳食纖維與抗氧化物的常見水果' },
        { id: 3, name: '西蘭花', slug: 'broccoli', description: '維生素C與葉酸的優質來源' }
      ],
      compareList: []
    }
  },

  mounted() {
    // ✅ 從首頁跳轉時自動定位到清單區
    if (this.$route.query.scroll === 'nutrition') {
      setTimeout(() => {
        const target = document.getElementById('nutrition-list-start')
        if (target) {
          target.scrollIntoView({ behavior: 'smooth', block: 'start' })
        }
      }, 300)
    }
  },

  computed: {
    filteredFoods() {
      if (!this.searchQuery) {
        return this.foods
      }
      return this.foods.filter(food =>
        food.name.includes(this.searchQuery) ||
        food.description.includes(this.searchQuery)
      )
    }
  },

  methods: {
    addToCompare(food) {
      if (!this.compareList.some(f => f.id === food.id)) {
        this.compareList.push(food)
        alert(`已加入比較：${food.name}`)
      } else {
        alert(`此食材已在比較清單中`)
      }
      console.log('目前比較清單：', this.compareList)
    }
  }
}
</script>

<style scoped>
/* 可以補充細節樣式 */
</style>
