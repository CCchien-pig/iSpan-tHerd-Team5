<!--
  ProductReviews.vue - 商品評價組件
  功能：顯示評價列表、評分摘要、提交評價
-->
<template>
  <div class="product-reviews">
    <h4 class="mb-4">顧客評價</h4>

    <!-- 評價摘要 -->
    <div class="review-summary mb-4 row">
      <!-- 左側：評分總覽 -->
      <div class="col-lg-4 col-md-12 mb-4">
        <div class="rating-overview p-4 border rounded bg-light text-center">
          <div class="average-rating mb-3">
            <div class="score display-3 fw-bold">{{ avgRating || 0 }}</div>
            <div class="stars mb-2">
              <span v-for="i in 5" :key="i" class="star">
                <i
                  class="bi fs-4"
                  :class="
                    i <= Math.floor(avgRating || 0)
                      ? 'bi-star-fill text-warning'
                      : 'bi-star text-warning'
                  "
                ></i>
              </span>
            </div>
            <div class="text-muted">基於 {{ reviewCount?.toLocaleString() }} 評分</div>
          </div>

          <!-- 評分分布 -->
          <div class="rating-distribution">
            <div
              v-for="star in [5, 4, 3, 2, 1]"
              :key="star"
              class="rating-bar d-flex align-items-center mb-2"
            >
              <span class="star-label me-2">{{ star }} 星</span>
              <div class="progress flex-grow-1">
                <div
                  class="progress-bar bg-success"
                  :style="{ width: getRatingPercentage(star) + '%' }"
                ></div>
              </div>
              <span class="count ms-2 text-muted small">{{ getRatingCount(star) }}</span>
            </div>
          </div>

          <!-- 撰寫評價按鈕 -->
          <button class="btn btn-primary w-100 mt-3" @click="handleWriteReview">
            <i class="bi bi-pencil-square me-2"></i>
            撰寫評價
          </button>
        </div>
      </div>

      <!-- 右側：篩選標籤和客戶評論 -->
      <div class="col-lg-8 col-md-12">
        <!-- 已驗證購買標籤 -->
        <div class="verified-badge mb-3">
          <span class="badge bg-success">
            <i class="bi bi-check-circle me-1"></i>
            完全正品
          </span>
          <span class="ms-2 text-muted small">我們的商品皆提供來自經過驗證購買者的評價</span>
        </div>

        <!-- 客戶評論亮點 -->
        <div class="customer-says mb-4 p-3 bg-light border-start border-warning border-4">
          <h6 class="fw-bold mb-2">
            <i class="bi bi-chat-quote text-warning me-2"></i>
            客戶評論
          </h6>
          <p class="mb-0 small">{{ customerHighlight }}</p>
        </div>

        <!-- 篩選標籤 -->
        <div class="filter-tags mb-3">
          <button
            v-for="tag in filterTags"
            :key="tag"
            class="btn btn-sm btn-outline-success me-2 mb-2"
            :class="{ active: selectedTags.includes(tag) }"
            @click="toggleTag(tag)"
          >
            <i class="bi bi-hand-thumbs-up me-1"></i>
            {{ tag }}
          </button>
        </div>

        <!-- 排序選項 -->
        <div class="sort-options mb-3">
          <select class="form-select form-select-sm" v-model="sortBy" style="max-width: 200px">
            <option value="date">最新評價</option>
            <option value="rating_high">評分：高到低</option>
            <option value="rating_low">評分：低到高</option>
            <option value="helpful">最有幫助</option>
          </select>
        </div>
      </div>
    </div>

    <!-- 撰寫評價表單 -->
    <div v-if="showReviewForm" class="review-form mb-4 p-4 border rounded bg-light">
      <h5 class="mb-3">撰寫您的評價</h5>

      <!-- 評分選擇 -->
      <div class="mb-3">
        <label class="form-label fw-bold">評分 *</label>
        <div class="rating-input">
          <span
            v-for="i in 5"
            :key="i"
            class="star-btn"
            @click="newReview.rating = i"
            @mouseover="hoverRating = i"
            @mouseleave="hoverRating = 0"
          >
            <i
              class="bi fs-3"
              :class="
                i <= (hoverRating || newReview.rating)
                  ? 'bi-star-fill text-warning'
                  : 'bi-star text-muted'
              "
            ></i>
          </span>
        </div>
      </div>

      <!-- 評價標題 -->
      <div class="mb-3">
        <label class="form-label fw-bold">標題 *</label>
        <input
          type="text"
          class="form-control"
          v-model="newReview.title"
          placeholder="簡短描述您的體驗"
          maxlength="100"
        />
        <small class="text-muted">{{ newReview.title.length }}/100</small>
      </div>

      <!-- 評價內容 -->
      <div class="mb-3">
        <label class="form-label fw-bold">評價內容 *</label>
        <textarea
          class="form-control"
          v-model="newReview.content"
          rows="5"
          placeholder="分享您的使用體驗..."
          maxlength="1000"
        ></textarea>
        <small class="text-muted">{{ newReview.content.length }}/1000</small>
      </div>

      <!-- 操作按鈕 -->
      <div class="d-flex justify-content-end gap-2">
        <button class="btn btn-secondary" @click="cancelReview">取消</button>
        <button
          class="btn btn-primary"
          @click="handleSubmitReview"
          :disabled="!isReviewValid || submitting"
        >
          <span v-if="submitting">
            <span class="spinner-border spinner-border-sm me-2"></span>
            提交中...
          </span>
          <span v-else>提交評價</span>
        </button>
      </div>
    </div>

    <!-- Loading 狀態 -->
    <div v-if="loading" class="text-center py-4">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">載入中...</span>
      </div>
    </div>

    <!-- 評價列表 -->
    <div v-else-if="filteredReviews.length > 0" class="reviews-list">
      <div
        v-for="review in displayedReviews"
        :key="review.reviewId"
        class="review-item mb-4 p-4 border rounded"
      >
        <!-- 評價頭部 -->
        <div class="review-header d-flex justify-content-between align-items-start mb-3">
          <div class="reviewer-info d-flex align-items-center">
            <i class="bi bi-person-circle fs-2 text-secondary me-3"></i>
            <div>
              <div class="reviewer-name fw-bold">{{ review.userName }}</div>
              <div class="review-date text-muted small">{{ formatDate(review.createdDate) }}</div>
            </div>
          </div>
          <!--
          <div class="review-actions">
            <button class="btn btn-sm btn-link text-muted" @click="reportReview(review.reviewId)">
              報告濫用行為
            </button>
            <button class="btn btn-sm btn-link text-muted" @click="shareReview(review.reviewId)">
              <i class="bi bi-share"></i>
              分享
            </button>
          </div>-->
        </div>

        <!-- 評分 -->
        <div class="review-rating mb-2">
          <span v-for="i in 5" :key="i" class="star">
            <i
              class="bi"
              :class="i <= review.rating ? 'bi-star-fill text-warning' : 'bi-star text-muted'"
            ></i>
          </span>
        </div>

        <!-- 評價標題 -->
        <h6 class="review-title fw-bold mb-2">{{ review.title }}</h6>

        <!-- 評價內容 -->
        <p class="review-content mb-3">{{ review.content }}</p>

        <!-- 評價圖片 -->
        <div v-if="review.images && review.images.length > 0" class="review-images mb-3">
          <img
            v-for="(image, index) in review.images"
            :key="index"
            :src="image.imageUrl"
            class="review-image me-2 mb-2"
            @click="viewImage(image.imageUrl)"
          />
        </div>

        <!-- 評價操作 -->
         <!--
        <div class="review-footer d-flex align-items-center gap-3">
          <button class="btn btn-sm btn-outline-secondary" @click="likeReview(review.reviewId)">
            <i class="bi bi-hand-thumbs-up me-1"></i>
            有幫助 {{ review.helpfulCount || 0 }}
          </button>
          <button class="btn btn-sm btn-outline-secondary" @click="dislikeReview(review.reviewId)">
            <i class="bi bi-hand-thumbs-down me-1"></i>
            {{ review.unhelpfulCount || 0 }}
          </button>
        </div>-->
      </div>

      <!-- 載入更多 -->
      <div v-if="hasMore" class="text-center mt-4">
        <button class="btn btn-outline-primary" @click="loadMore">載入更多評價</button>
      </div>
    </div>

    <!-- 無評價 -->
    <div v-else class="text-center py-5">
      <i class="bi bi-chat-left-text fs-1 text-muted"></i>
      <p class="text-muted mt-3">尚無評價，成為第一個評價的人吧！</p>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { toast, success, error as showError } from '@/utils/sweetalert'
import { http } from '@/api/http'

const emit = defineEmits(['refresh'])

const props = defineProps({
  productId: { type: Number, required: true },
  reviews: { type: Array, default: () => [] },
  avgRating: { type: Number, default: 0 },
  reviewCount: { type: Number, default: 0 }
})

// 狀態
const loading = ref(false)
const submitting = ref(false)
const reviews = ref(props.reviews || [])
const showReviewForm = ref(false)
const hoverRating = ref(0)
const selectedTags = ref([])
const sortBy = ref('date')
const displayCount = ref(10)

// 新評價表單
const newReview = ref({
  rating: 0,
  title: '',
  content: '',
})

// 篩選標籤
//const filterTags = ['有幫助', '真實感', '物超所值', '含純度', '心血管健康', '減少炎症']
// 客戶評論亮點
// const customerHighlight = computed(() => {
//   if (reviews.value.length === 0) return ''
//   // 從評價中提取最常見的評論
//   return '這款800倍高濃縮Omega-3魚油很有感，對心血管保健、減少炎症都有幫助，有真實感覺心臟更強壯有力，還可以搭配其他產品，沒有反式脂肪，很好吞服！'
// })

// 評分分布數據
const ratingDistribution = computed(() => {
  const dist = { 5: 0, 4: 0, 3: 0, 2: 0, 1: 0 }
  reviews.value.forEach((review) => {
    if (review.rating >= 1 && review.rating <= 5) {
      dist[review.rating]++
    }
  })
  return dist
})

// 獲取評分百分比
const getRatingPercentage = (star) => {
  if (reviews.value.length === 0) return 0
  return ((ratingDistribution.value[star] / reviews.value.length) * 100).toFixed(1)
}

/**
 * ✨ 自動生成篩選標籤（filterTags）
 * 根據所有評論內容進行關鍵詞出現統計，挑出熱門詞
 */
const filterTags = computed(() => {
  if (!reviews.value || reviews.value.length === 0) return []

  const text = reviews.value.map(r => (r.content || '') + ' ' + (r.title || '')).join(' ')
  const lower = text.toLowerCase()

  // 定義候選關鍵詞與主題分類
  const tagMap = {
    效果顯著: ['有效', '有感', '改善', '幫助', '明顯'],
    物超所值: ['便宜', '划算', '超值', 'cp值', '實惠'],
    味道不錯: ['好吃', '味道', '香', '口感', '不苦'],
    使用方便: ['方便', '容易吞', '好吸收', '好吃', '包裝'],
    健康改善: ['健康', '心臟', '炎症', '免疫', '保健'],
    品質優良: ['品質', '純度', '成分', '穩定', '信任']
  }

  // 統計命中次數
  const counts = Object.entries(tagMap).map(([tag, words]) => {
    const count = words.reduce((sum, w) => sum + (lower.includes(w) ? 1 : 0), 0)
    return { tag, count }
  })

  // 過濾出有命中的標籤（至少出現一次）
  const hotTags = counts.filter(c => c.count > 0)

  // 排序取前幾個熱門標籤（例如最多 6 個）
  hotTags.sort((a, b) => b.count - a.count)
  return hotTags.slice(0, 6).map(c => c.tag)
})

/**
 * ✨ 客戶評論亮點（從資料中動態生成）
 * - 根據關鍵詞與字詞出現次數進行統計
 * - 簡單摘要主要正面回饋方向
 */
const customerHighlight = computed(() => {
  if (!reviews.value || reviews.value.length === 0) return '目前尚無顧客評論'

  // 抽取所有內容
  const allText = reviews.value.map(r => (r.content || '') + ' ' + (r.title || '')).join(' ')
  const lower = allText.toLowerCase()

  // 定義一些常見關鍵詞
  const keywords = {
    效果: ['有效', '改善', '幫助', '有感', '明顯'],
    價值: ['便宜', '划算', '超值', '物超所值'],
    味道: ['好吃', '不錯', '味道', '苦', '香', '口感'],
    健康: ['健康', '身體', '心臟', '免疫力', '炎症'],
    使用感: ['方便', '吸收快', '容易吞', '包裝好', '質感']
  }

  // 統計關鍵詞出現次數
  const stats = {}
  for (const [cat, list] of Object.entries(keywords)) {
    stats[cat] = list.reduce((sum, k) => sum + (lower.includes(k) ? 1 : 0), 0)
  }

  // 找出主要特點
  const top = Object.entries(stats).sort((a, b) => b[1] - a[1])[0]
  const key = top?.[0]

  switch (key) {
    case '效果':
      return '多數顧客表示產品「效果明顯」，使用後有明顯改善與實感。'
    case '價值':
      return '大家普遍認為這款商品「物超所值」，價格與品質都令人滿意。'
    case '味道':
      return '顧客提到「口感好」與「味道不錯」，食用體驗佳。'
    case '健康':
      return '許多評論提到「健康改善」與「心血管有幫助」，是養生族群推薦的產品。'
    case '使用感':
      return '大家覺得「使用方便」、「容易吞服」，整體包裝質感良好。'
    default:
      return '顧客整體評價良好，普遍滿意這款商品的品質與體驗。'
  }
})

// 獲取評分數量
const getRatingCount = (star) => {
  return ratingDistribution.value[star]?.toLocaleString() || 0
}

// 篩選後的評價
const filteredReviews = computed(() => {
  let result = [...reviews.value]

  // 排序
  switch (sortBy.value) {
    case 'date':
      result.sort((a, b) => new Date(b.createdDate) - new Date(a.createdDate))
      break
    case 'rating_high':
      result.sort((a, b) => b.rating - a.rating)
      break
    case 'rating_low':
      result.sort((a, b) => a.rating - b.rating)
      break
    case 'helpful':
      result.sort((a, b) => (b.helpfulCount || 0) - (a.helpfulCount || 0))
      break
  }

  return result
})

/**
 * 📝 撰寫評價前的檢查
 */
const handleWriteReview = async () => {
  try {
    const { data } = await http.get(`/prod/Products/check-can-review/${props.productId}`)
    const hasPurchased = data?.data?.hasPurchased ?? data?.hasPurchased

    // 若尚未購買 → 提示
    if (!hasPurchased) {
      toast('請先購買此商品後才能撰寫評價', 'warning')
      return
    }

    // 通過檢查 → 開啟評價表單
    showReviewForm.value = true
  } catch (err) {
    console.error('檢查評價資格錯誤:', err)
    showError('檢查失敗，請稍後再試')
  }
}

// 顯示的評價
const displayedReviews = computed(() => {
  return filteredReviews.value.slice(0, displayCount.value)
})

// 是否還有更多
const hasMore = computed(() => {
  return displayCount.value < filteredReviews.value.length
})

// 評價表單驗證
const isReviewValid = computed(() => {
  return (
    newReview.value.rating > 0 && newReview.value.title.trim() && newReview.value.content.trim()
  )
})

/**
 * 載入評價列表
 */
watch(
  () => props.reviews,
  (newVal) => {
    reviews.value = newVal || []
  },
  { immediate: true }
)

/**
 * 提交評價
 */
const handleSubmitReview = async () => {
  if (!isReviewValid.value) {
    toast('請完整填寫評價內容', 'warning')
    return
  }

  try {
    submitting.value = true

    // 呼叫後端 API
    const { data } = await http.post('/prod/Products/submit-review', {
      productId: props.productId,
      skuId: null, // TODO: 若有規格可再接
      rating: newReview.value.rating,
      title: newReview.value.title.trim(),
      content: newReview.value.content.trim(),
    })

    // 後端成功回傳
    if (data?.success) {
      success('評價提交成功！', '感謝您的分享')

      // ✅ 重新查詢商品詳細（更新評價列表與星等）
      emit('refresh')

      // 即時新增到畫面頂部（不必重整）
      reviews.value.unshift({
        reviewId: Date.now(),
        userName: '您',
        rating: newReview.value.rating,
        title: newReview.value.title,
        content: newReview.value.content,
        createdDate: new Date().toISOString(),
        helpfulCount: 0,
        unhelpfulCount: 0,
        images: [],
      })

      // 清空表單
      newReview.value = { rating: 0, title: '', content: '' }
      showReviewForm.value = false
    } else {
      // ❌ 後端回傳失敗
      showError(data?.message || '提交失敗，請稍後再試')
    }
  } catch (err) {
    // 🧯 例外處理（如 401、500）
    console.error('提交評價錯誤:', err)
    if (err.response?.status === 401) {
      toast('請先登入後再撰寫評價', 'info')
    } else {
      showError('伺服器發生錯誤，請稍後再試')
    }
  } finally {
    submitting.value = false
  }
}

/**
 * 取消評價
 */
const cancelReview = () => {
  newReview.value = { rating: 0, title: '', content: '' }
  showReviewForm.value = false
}

/**
 * 切換標籤
 */
const toggleTag = (tag) => {
  const index = selectedTags.value.indexOf(tag)
  if (index > -1) {
    selectedTags.value.splice(index, 1)
  } else {
    selectedTags.value.push(tag)
  }
}

/**
 * 載入更多
 */
const loadMore = () => {
  displayCount.value += 10
}

/**
 * 點讚評價
 */
const likeReview = (reviewId) => {
  const review = reviews.value.find((r) => r.reviewId === reviewId)
  if (review) {
    review.helpfulCount = (review.helpfulCount || 0) + 1
    toast('感謝您的反饋', 'success', 1500)
  }
}

/**
 * 點踩評價
 */
const dislikeReview = (reviewId) => {
  const review = reviews.value.find((r) => r.reviewId === reviewId)
  if (review) {
    review.unhelpfulCount = (review.unhelpfulCount || 0) + 1
    toast('感謝您的反饋', 'info', 1500)
  }
}

/**
 * 報告評價
 */
const reportReview = () => {
  toast('感謝您的回報，我們會盡快處理', 'info')
}

/**
 * 分享評價
 */
const shareReview = (reviewId) => {
  const url = `${window.location.origin}${window.location.pathname}#review-${reviewId}`
  navigator.clipboard.writeText(url).then(() => {
    toast('連結已複製到剪貼簿', 'success', 2000)
  })
}

/**
 * 查看圖片
 */
const viewImage = (imageUrl) => {
  window.open(imageUrl, '_blank')
}

/**
 * 格式化日期
 */
const formatDate = (dateString) => {
  if (!dateString) return ''
  const date = new Date(dateString)
  const year = date.getFullYear()
  const month = date.getMonth() + 1
  const day = date.getDate()
  return `${year}年${month}月${day}日`
}
</script>

<style scoped>
/* 評分總覽 */
.rating-overview {
  background-color: #f8f9fa;
}

.average-rating .score {
  color: #333;
}

/* 評分分布 */
.rating-bar {
  font-size: 0.875rem;
}

.star-label {
  min-width: 45px;
}

.progress {
  height: 8px;
}

/* 篩選標籤 */
.filter-tags .btn.active {
  background-color: #28a745;
  color: white;
  border-color: #28a745;
}

/* 評價項目 */
.review-item {
  background-color: #fff;
  transition: box-shadow 0.3s ease;
}

.review-item:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

/* 評分輸入 */
.rating-input {
  display: flex;
  gap: 0.5rem;
}

.star-btn {
  cursor: pointer;
  transition: transform 0.2s;
}

.star-btn:hover {
  transform: scale(1.2);
}

/* 評價圖片 */
.review-image {
  width: 100px;
  height: 100px;
  object-fit: cover;
  border-radius: 8px;
  cursor: pointer;
  transition: transform 0.3s ease;
}

.review-image:hover {
  transform: scale(1.05);
}

/* 評價內容 */
.review-title {
  font-size: 1.1rem;
  color: #333;
}

.review-content {
  line-height: 1.6;
  color: #495057;
}

/* 響應式設計 */
@media (max-width: 768px) {
  .review-actions {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
  }

  .filter-tags .btn {
    font-size: 0.875rem;
  }
}
</style>
