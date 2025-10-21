<!--
  ProductQA.vue - 商品問答組件
  功能：顯示問答列表、提交問題、回覆問題
-->
<template>
  <div class="product-qa">
    <h4 class="mb-4">問答</h4>

    <!-- 提問按鈕 -->
    <button class="btn btn-primary mb-4" @click="showQuestionForm = true">
      <i class="bi bi-question-circle me-2"></i>
      提問
    </button>

    <!-- 提問表單 -->
    <div v-if="showQuestionForm" class="question-form mb-4 p-4 border rounded bg-light">
      <h5 class="mb-3">提出問題</h5>
      <textarea
        v-model="questionContent"
        class="form-control mb-3"
        rows="4"
        placeholder="請輸入您的問題..."
        maxlength="500"
      ></textarea>
      <div class="d-flex justify-content-between align-items-center">
        <small class="text-muted">{{ questionContent.length }}/500</small>
        <div>
          <button class="btn btn-secondary me-2" @click="cancelQuestion">取消</button>
          <button
            class="btn btn-primary"
            @click="handleSubmitQuestion"
            :disabled="!questionContent.trim() || submitting"
          >
            <span v-if="submitting">
              <span class="spinner-border spinner-border-sm me-2"></span>
              提交中...
            </span>
            <span v-else>提交問題</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Loading 狀態 -->
    <div v-if="loading" class="text-center py-4">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">載入中...</span>
      </div>
    </div>

    <!-- 問答列表 -->
    <div v-else-if="questions.length > 0" class="qa-list">
      <div v-for="qa in questions" :key="qa.questionId" class="qa-item mb-4 p-4 border rounded">
        <!-- 問題 -->
        <div class="question-section mb-3">
          <div class="d-flex justify-content-between align-items-start mb-2">
            <div class="flex-grow-1">
              <div class="d-flex align-items-center mb-2">
                <i class="bi bi-person-circle fs-5 text-secondary me-2"></i>
                <span class="fw-bold">{{ qa.userName }}</span>
                <span class="text-muted ms-2 small">{{ formatDate(qa.createdDate) }}</span>
              </div>
              <p class="question-content mb-2">{{ qa.questionContent }}</p>
            </div>
            <div class="qa-actions">
              <button class="btn btn-sm btn-link text-muted" @click="reportAbuse(qa.questionId)">
                報告濫用行為
              </button>
              <button class="btn btn-sm btn-link text-muted" @click="shareQuestion(qa.questionId)">
                <i class="bi bi-share"></i>
                分享
              </button>
            </div>
          </div>
        </div>

        <!-- 回答列表 -->
        <div v-if="qa.answers && qa.answers.length > 0" class="answers-section">
          <div
            v-for="(answer, index) in qa.answers"
            :key="index"
            class="answer-item ms-4 mb-3 p-3 bg-light rounded"
          >
            <div class="d-flex align-items-start">
              <i
                :class="
                  answer.isOfficial
                    ? 'bi-shield-check text-success'
                    : 'bi-person-circle text-secondary'
                "
                class="bi fs-5 me-2"
              ></i>
              <div class="flex-grow-1">
                <div class="d-flex align-items-center mb-2">
                  <span class="fw-bold" :class="{ 'text-success': answer.isOfficial }">
                    {{ answer.isOfficial ? 'tHerd 官方回覆' : '會員回覆' }}
                  </span>
                  <span class="text-muted ms-2 small">{{ formatDate(answer.createdDate) }}</span>
                </div>
                <p class="answer-content mb-0">{{ answer.answerContent }}</p>
              </div>
            </div>
          </div>
        </div>

        <!-- 回覆按鈕 -->
        <div class="reply-section ms-4">
          <button
            v-if="!qa.showReplyForm"
            class="btn btn-sm btn-outline-primary"
            @click="toggleReplyForm(qa.questionId)"
          >
            <i class="bi bi-reply me-1"></i>
            回覆
          </button>

          <!-- 回覆表單 -->
          <div v-else class="reply-form mt-3 p-3 border rounded bg-white">
            <textarea
              v-model="qa.replyContent"
              class="form-control mb-2"
              rows="3"
              placeholder="請輸入您的回覆..."
              maxlength="500"
            ></textarea>
            <div class="d-flex justify-content-between align-items-center">
              <small class="text-muted">{{ (qa.replyContent || '').length }}/500</small>
              <div>
                <button
                  class="btn btn-sm btn-secondary me-2"
                  @click="toggleReplyForm(qa.questionId)"
                >
                  取消
                </button>
                <button
                  class="btn btn-sm btn-primary"
                  @click="handleSubmitAnswer(qa)"
                  :disabled="!qa.replyContent?.trim() || submitting"
                >
                  <span v-if="submitting">
                    <span class="spinner-border spinner-border-sm me-1"></span>
                    提交中...
                  </span>
                  <span v-else>提交回覆</span>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- 查看更多回答 -->
        <div v-if="qa.answers && qa.answers.length > 1 && !qa.showAllAnswers" class="ms-4 mt-2">
          <button
            class="btn btn-sm btn-link text-primary"
            @click="toggleShowAllAnswers(qa.questionId)"
          >
            查看更多回答 ({{ qa.answers.length }})
            <i class="bi bi-chevron-down"></i>
          </button>
        </div>
      </div>
    </div>

    <!-- 無資料 -->
    <div v-else class="text-center py-4">
      <i class="bi bi-chat-left-text fs-1 text-muted"></i>
      <p class="text-muted mt-3">暫無問答，成為第一個提問的人吧！</p>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import ProductsApi from '@/api/modules/prod/ProductsApi'
import { toast, success, error as showError } from '@/utils/sweetalert'

const props = defineProps({
  productId: {
    type: Number,
    required: true,
  },
})

// 狀態
const loading = ref(false)
const submitting = ref(false)
const questions = ref([])
const showQuestionForm = ref(false)
const questionContent = ref('')

/**
 * 載入問答列表
 */
const loadQuestions = async () => {
  try {
    loading.value = true
    const response = await ProductsApi.getQuestions(props.productId)

    if (response.success) {
      questions.value = (response.data || []).map((q) => ({
        ...q,
        showReplyForm: false,
        showAllAnswers: false,
        replyContent: '',
      }))
    }
  } catch (err) {
    console.error('載入問答錯誤:', err)
  } finally {
    loading.value = false
  }
}

/**
 * 提交問題
 */
const handleSubmitQuestion = async () => {
  if (!questionContent.value.trim()) {
    return
  }

  try {
    submitting.value = true
    const response = await ProductsApi.submitQuestion({
      productId: props.productId,
      questionContent: questionContent.value,
    })

    if (response.success) {
      success('問題提交成功！', '感謝您的提問')
      questionContent.value = ''
      showQuestionForm.value = false
      // 重新載入問答列表
      await loadQuestions()
    } else {
      showError(response.message || '提交失敗')
    }
  } catch (err) {
    console.error('提交問題錯誤:', err)
    showError('提交失敗，請稍後再試')
  } finally {
    submitting.value = false
  }
}

/**
 * 取消提問
 */
const cancelQuestion = () => {
  questionContent.value = ''
  showQuestionForm.value = false
}

/**
 * 切換回覆表單
 */
const toggleReplyForm = (questionId) => {
  const qa = questions.value.find((q) => q.questionId === questionId)
  if (qa) {
    qa.showReplyForm = !qa.showReplyForm
    if (!qa.showReplyForm) {
      qa.replyContent = ''
    }
  }
}

/**
 * 提交回覆
 */
const handleSubmitAnswer = async (qa) => {
  if (!qa.replyContent?.trim()) {
    return
  }

  try {
    submitting.value = true
    const response = await ProductsApi.submitAnswer({
      questionId: qa.questionId,
      answerContent: qa.replyContent,
    })

    if (response.success) {
      toast('回覆提交成功！', 'success')
      qa.replyContent = ''
      qa.showReplyForm = false
      // 重新載入問答列表
      await loadQuestions()
    } else {
      showError(response.message || '提交失敗')
    }
  } catch (err) {
    console.error('提交回覆錯誤:', err)
    showError('提交失敗，請稍後再試')
  } finally {
    submitting.value = false
  }
}

/**
 * 切換顯示所有回答
 */
const toggleShowAllAnswers = (questionId) => {
  const qa = questions.value.find((q) => q.questionId === questionId)
  if (qa) {
    qa.showAllAnswers = !qa.showAllAnswers
  }
}

/**
 * 報告濫用
 */
const reportAbuse = (questionId) => {
  toast('感謝您的回報，我們會盡快處理', 'info')
  console.log('報告濫用:', questionId)
}

/**
 * 分享問題
 */
const shareQuestion = (questionId) => {
  // 複製分享連結
  const url = `${window.location.origin}${window.location.pathname}#qa-${questionId}`
  navigator.clipboard.writeText(url).then(() => {
    toast('連結已複製到剪貼簿', 'success', 2000)
  })
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

// 生命週期
onMounted(() => {
  loadQuestions()
})
</script>

<style scoped>
.product-qa {
  padding: 1rem 0;
}

/* 問答項目 */
.qa-item {
  background-color: #fff;
  transition: box-shadow 0.3s ease;
}

.qa-item:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

/* 問題內容 */
.question-content {
  font-size: 1rem;
  line-height: 1.6;
  color: #333;
}

/* 回答項目 */
.answer-item {
  border-left: 3px solid #e9ecef;
}

.answer-content {
  font-size: 0.95rem;
  line-height: 1.6;
  color: #495057;
}

/* 操作按鈕 */
.qa-actions button {
  font-size: 0.875rem;
  padding: 0.25rem 0.5rem;
  text-decoration: none;
}

.qa-actions button:hover {
  text-decoration: underline;
}

/* 提問表單 */
.question-form {
  background-color: #f8f9fa;
}

.question-form textarea {
  resize: none;
}

/* 回覆表單 */
.reply-form textarea {
  resize: none;
}

/* 官方回覆特殊樣式 */
.answer-item:has(.text-success) {
  border-left-color: #28a745;
  background-color: #f0f8f4;
}

/* 響應式設計 */
@media (max-width: 768px) {
  .qa-actions {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
  }

  .answer-item {
    margin-left: 0 !important;
  }

  .reply-section {
    margin-left: 0 !important;
  }
}
</style>
