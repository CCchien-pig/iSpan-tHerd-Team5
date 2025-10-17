<!--
  About.vue - 關於我們頁面
  功能：展示公司介紹、核心價值、成就數據和聯絡方式
  特色：使用Bootstrap響應式設計，包含多個資訊區塊
-->
<template>
  <!-- 關於我們頁面容器 -->
  <div class="about-page">
    <div class="container">
      <!--
        頁面標題區塊
        功能：展示頁面主標題和副標題
        設計：使用漸層背景和居中對齊
      -->
      <div class="row">
        <div class="col-12">
          <div class="page-header text-center py-5">
            <!-- TODO: 更新為API數據 -->
            <h1 class="display-4 fw-bold text-success mb-4">關於 tHerd</h1>
            <p class="lead">您的健康生活夥伴，致力於提供最優質的健康產品</p>
          </div>
        </div>
      </div>

      <!--
        公司介紹區塊
        功能：展示公司背景、使命和價值主張
        布局：左右分欄，左側文字介紹，右側公司圖片
        響應式：大螢幕並排顯示，小螢幕垂直堆疊
      -->
      <div class="row align-items-center mb-5">
        <div class="col-lg-6">
          <!-- TODO: 更新為API數據 -->
          <h2 class="mb-4">我們的使命</h2>
          <p class="lead">
            tHerd 成立於 1996 年，是全球領先的健康產品電商平台。
            我們致力於為全球客戶提供最優質的維生素、礦物質、美容護膚、
            運動營養等健康產品，幫助人們實現更健康的生活方式。
          </p>
          <p>
            我們相信每個人都應該有機會獲得高品質的健康產品，
            因此我們與全球頂尖品牌合作，確保每一件產品都經過嚴格品質檢驗。
          </p>
        </div>
        <div class="col-lg-6">
          <!-- 公司總部圖片 -->
          <img
            src="https://picsum.photos/600/400?random=15"
            alt="tHerd 總部"
            class="img-fluid rounded shadow"
          />
        </div>
      </div>

      <!--
        核心價值區塊
        功能：展示公司的四大核心價值觀
        設計：4個卡片式布局，每個卡片包含圖標、標題和描述
        響應式：大螢幕4列，中螢幕2列，小螢幕1列
      -->
      <div class="row mb-5">
        <div class="col-12">
          <h2 class="text-center mb-5">核心價值</h2>
          <div class="row g-4">
            <!-- 品質保證卡片 -->
            <div class="col-lg-3 col-md-6">
              <div class="value-card text-center h-100">
                <div class="value-icon">
                  <i class="bi bi-shield-check"></i>
                </div>
                <h5>品質保證</h5>
                <p>所有產品均為正品，經過嚴格品質檢驗</p>
              </div>
            </div>
            <!-- 客戶至上卡片 -->
            <div class="col-lg-3 col-md-6">
              <div class="value-card text-center h-100">
                <div class="value-icon">
                  <i class="bi bi-people"></i>
                </div>
                <h5>客戶至上</h5>
                <p>以客戶需求為中心，提供優質服務</p>
              </div>
            </div>
            <!-- 全球服務卡片 -->
            <div class="col-lg-3 col-md-6">
              <div class="value-card text-center h-100">
                <div class="value-icon">
                  <i class="bi bi-globe"></i>
                </div>
                <h5>全球服務</h5>
                <p>服務全球客戶，配送至世界各地</p>
              </div>
            </div>
            <!-- 健康使命卡片 -->
            <div class="col-lg-3 col-md-6">
              <div class="value-card text-center h-100">
                <div class="value-icon">
                  <i class="bi bi-heart"></i>
                </div>
                <h5>健康使命</h5>
                <p>推廣健康生活理念，關愛每一個人</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!--
        數據統計區塊
        功能：展示公司的關鍵成就數據，建立權威性和信任度
        設計：使用數字突出顯示，配合描述標籤
        布局：4個統計項目，響應式網格布局
      -->
      <div class="row mb-5">
        <div class="col-12">
          <div class="stats-section bg-light rounded p-5">
            <!-- TODO: 更新為API數據 -->
            <h2 class="text-center mb-5">我們的成就</h2>
            <div class="row g-4">
              <!-- 產品種類統計 -->
              <div class="col-lg-3 col-md-6">
                <div class="stat-item text-center">
                  <div class="stat-number">30,000+</div>
                  <div class="stat-label">產品種類</div>
                </div>
              </div>
              <!-- 服務國家統計 -->
              <div class="col-lg-3 col-md-6">
                <div class="stat-item text-center">
                  <div class="stat-number">190+</div>
                  <div class="stat-label">服務國家</div>
                </div>
              </div>
              <!-- 經驗年數統計 -->
              <div class="col-lg-3 col-md-6">
                <div class="stat-item text-center">
                  <div class="stat-number">25+</div>
                  <div class="stat-label">年經驗</div>
                </div>
              </div>
              <!-- 滿意客戶統計 -->
              <div class="col-lg-3 col-md-6">
                <div class="stat-item text-center">
                  <div class="stat-number">1000萬+</div>
                  <div class="stat-label">滿意客戶</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!--
        聯絡我們區塊
        功能：提供多種聯絡方式，方便客戶與公司溝通
        設計：3個聯絡方式卡片，每個包含圖標、標題和聯絡資訊
        布局：響應式網格，大螢幕3列，小螢幕1列
      -->
      <div class="row">
        <div class="col-12">
          <div class="contact-section text-center">
            <!-- TODO: 更新為API數據 -->
            <h2 class="mb-4">聯絡我們</h2>
            <p class="lead mb-4">如果您有任何問題或建議，歡迎隨時與我們聯繫</p>
            <div class="row g-4">
              <!-- 電子郵件聯絡方式 -->
              <div class="col-lg-4">
                <div class="contact-item">
                  <i class="bi bi-envelope contact-icon"></i>
                  <h5>電子郵件</h5>
                  <p>service@tHerd.com</p>
                </div>
              </div>
              <!-- 客服電話聯絡方式 -->
              <div class="col-lg-4">
                <div class="contact-item">
                  <i class="bi bi-telephone contact-icon"></i>
                  <h5>客服電話</h5>
                  <p>+886-2-1234-5678</p>
                </div>
              </div>
              <!-- 線上客服聯絡方式 -->
              <div class="col-lg-4">
                <div class="contact-item">
                  <i class="bi bi-chat-dots contact-icon"></i>
                  <h5>線上客服</h5>
                  <p>24/7 即時服務</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
/**
 * About.vue 組件配置
 * 功能：關於我們頁面，展示公司資訊和聯絡方式
 * 特色：純展示型頁面，無複雜邏輯，適合新人學習
 *
 * 注意：此頁面使用靜態內容，實際項目中可能需要：
 * 1. 從CMS或API獲取公司資訊
 * 2. 動態載入聯絡方式
 * 3. 添加表單提交功能
 */
export default {
  name: 'AboutView', // 組件名稱，用於Vue DevTools調試

  // 此頁面為純展示型，無需額外的數據或方法
  // 所有內容都直接寫在模板中，便於維護和理解
}
</script>

<style scoped>
.about-page {
  min-height: 100vh;
  padding: 2rem 0;
}

.page-header {
  background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
  border-radius: 1rem;
  margin-bottom: 3rem;
}

.value-card {
  background: white;
  border-radius: 1rem;
  padding: 2rem;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.08);
  transition: transform 0.3s ease;
}

.value-card:hover {
  transform: translateY(-5px);
}

.value-icon {
  font-size: 3rem;
  color: #28a745;
  margin-bottom: 1rem;
}

.stats-section {
  background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
}

.stat-item {
  padding: 1rem;
}

.stat-number {
  font-size: 3rem;
  font-weight: bold;
  color: #28a745;
  margin-bottom: 0.5rem;
}

.stat-label {
  font-size: 1.1rem;
  color: #6c757d;
  font-weight: 500;
}

.contact-item {
  padding: 2rem 1rem;
}

.contact-icon {
  font-size: 2.5rem;
  color: #28a745;
  margin-bottom: 1rem;
}

@media (max-width: 768px) {
  .about-page {
    padding: 1rem 0;
  }

  .page-header {
    padding: 2rem 1rem;
  }

  .display-4 {
    font-size: 2rem;
  }

  .stat-number {
    font-size: 2rem;
  }
}
</style>
