<template>
  <div class="about-wrap">
    <!-- Hero -->
    <section class="bg-light py-5 border-bottom">
      <div class="container">
        <div class="row align-items-center g-4">
          <div class="col-md-7">
            <h1 class="display-5 fw-bold main-color-green-text">關於 tHerd</h1>
            <p class="lead text-muted mt-3">
              我們是一支熱愛電商與使用者體驗的開發團隊。tHerd 專注在把
              「客服搜尋、工單、即時客服」做得簡單好用，讓顧客更快找到答案、讓客服更有效率。
            </p>
            <div class="d-flex gap-2 mt-4">
              <router-link to="/cs/faq" class="btn btn-primary px-4">看看常見問題</router-link>
              <router-link to="/cs/ticket" class="btn btn-outline-primary px-4">聯絡客服</router-link>
            </div>
          </div>
          <div class="col-md-5 text-center">
            <!-- 可換成品牌插圖或產品照 -->
            <div class="ratio ratio-1x1 rounded-4 shadow-sm bg-white d-flex align-items-center justify-content-center">
              <div class="p-4">
                <span class="fs-1">🤝</span>
                <p class="mt-2 mb-0 fw-semibold main-color-green-text">tHerd Care · 客服模組</p>
                <small class="text-muted">FAQ／工單／即時客服</small>
              </div>
            </div>
          </div>
        </div>

        <!-- KPI / 服務亮點 -->
        <div class="row text-center mt-5 g-3">
          <div class="col-6 col-md-3">
            <div class="card border-0 shadow-sm py-4">
              <div class="fs-3 fw-bold main-color-green-text">70%</div>
              <div class="text-muted small">常見問題自助解決率</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="card border-0 shadow-sm py-4">
              <div class="fs-3 fw-bold main-color-green-text">24/7</div>
              <div class="text-muted small">FAQ 即時搜尋</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="card border-0 shadow-sm py-4">
              <div class="fs-3 fw-bold main-color-green-text">雙向</div>
              <div class="text-muted small">前後台工單串接</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="card border-0 shadow-sm py-4">
              <div class="fs-3 fw-bold main-color-green-text">即時</div>
              <div class="text-muted small">SignalR 即時客服</div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 品牌理念 -->
    <section class="py-5">
      <div class="container">
        <div class="row g-4 align-items-center">
          <div class="col-md-6">
            <h2 class="fw-bold mb-3">我們的理念</h2>
            <p class="text-muted">
              以「搜尋優先」與「流程透明」為核心。tHerd 將 FAQ、工單、即時客服整合在同一個體驗裡—
              使用者可以先用關鍵字快速找到答案；若仍需要協助，工單表單會自動帶入分類與必要資訊，
              後台也能清楚追蹤處理進度。
            </p>
            <ul class="list-unstyled mb-0">
              <li class="d-flex align-items-start mb-2">
                <i class="bi bi-check2-circle me-2 main-color-green-text"></i>
                <span>Debounce + 模糊搜尋 + 多關鍵字，減少 API 壓力、提升命中率</span>
              </li>
              <li class="d-flex align-items-start mb-2">
                <i class="bi bi-check2-circle me-2 main-color-green-text"></i>
                <span>前後台工單雙向串接，完整紀錄與信件回覆</span>
              </li>
              <li class="d-flex align-items-start">
                <i class="bi bi-check2-circle me-2 main-color-green-text"></i>
                <span>可擴充的 AI 自動回覆，支援規則匹配與升級至真人</span>
              </li>
            </ul>
          </div>
          <div class="col-md-6">
            <div class="card border-0 shadow-sm p-4 h-100">
              <h5 class="fw-bold mb-3 main-color-green-text">技術棧</h5>
              <div class="row gy-2">
                <div class="col-6">
                  <div class="small text-muted">前端</div>
                  <div>Vue 3、Vite、Bootstrap 5</div>
                </div>
                <div class="col-6">
                  <div class="small text-muted">後端</div>
                  <div>.NET Core、EF Core、Dapper</div>
                </div>
                <div class="col-6">
                  <div class="small text-muted">即時</div>
                  <div>SignalR、JWT</div>
                </div>
                <div class="col-6">
                  <div class="small text-muted">資料庫</div>
                  <div>SQL Server</div>
                </div>
              </div>
              <hr>
              <div class="small text-muted">設計與品牌</div>
              <div>主色系（藍綠）、精選答案卡、行動導向 CTA</div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 里程碑 -->
    <section class="py-5 bg-light border-top border-bottom">
      <div class="container">
        <h2 class="fw-bold mb-4 text-center">里程碑</h2>
        <div class="timeline">
          <div class="timeline-item">
            <div class="timeline-dot"></div>
            <div class="timeline-content">
              <h6 class="mb-1">2025/09</h6>
              <p class="mb-0">FAQ 知識庫完成：分類、關鍵字、即時搜尋、精選答案卡。</p>
            </div>
          </div>
          <div class="timeline-item">
            <div class="timeline-dot"></div>
            <div class="timeline-content">
              <h6 class="mb-1">2025/10</h6>
              <p class="mb-0">客服工單串接後台：信件回覆、歷史追蹤、附件上傳。</p>
            </div>
          </div>
          <div class="timeline-item">
            <div class="timeline-dot"></div>
            <div class="timeline-content">
              <h6 class="mb-1">2025/11</h6>
              <p class="mb-0">即時客服雛型：SignalR 連線、轉真人流程、權限驗證。</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 核心價值 -->
    <section class="py-5">
      <div class="container">
        <h2 class="fw-bold mb-4 text-center">核心價值</h2>
        <div class="row g-3">
          <div class="col-md-4">
            <div class="card h-100 border-0 shadow-sm">
              <div class="card-body">
                <h5 class="fw-bold main-color-green-text">以人為本</h5>
                <p class="text-muted mb-0">以終端使用者的尋找速度與客服效率為優先，設計每一個互動。</p>
              </div>
            </div>
          </div>
          <div class="col-md-4">
            <div class="card h-100 border-0 shadow-sm">
              <div class="card-body">
                <h5 class="fw-bold main-color-green-text">可觀測</h5>
                <p class="text-muted mb-0">以資料與儀表板驅動改善：分類命中率、工單 SLA、回覆品質。</p>
              </div>
            </div>
          </div>
          <div class="col-md-4">
            <div class="card h-100 border-0 shadow-sm">
              <div class="card-body">
                <h5 class="fw-bold main-color-green-text">可擴充</h5>
                <p class="text-muted mb-0">模組化後端、統一回傳格式與權限控管，方便日後擴充 AI 與多服務。</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 聯絡與 CTA -->
    <section class="py-5 bg-dark text-white">
      <div class="container">
        <div class="row g-4 align-items-center">
          <div class="col-md-8">
            <h3 class="fw-bold">需要協助嗎？</h3>
            <p class="mb-1">Email：support@therd.example</p>
            <p class="mb-0">服務時間：週一至週五 09:30–18:30（GMT+8）</p>
          </div>
          <div class="col-md-4 text-md-end">
            <router-link to="/cs/ticket" class="btn btn-primary btn-lg teal-reflect-button">
              立即提交工單
            </router-link>
          </div>
        </div>
      </div>
    </section>

    <!-- 版權 -->
    <footer class="py-4 bg-light">
      <div class="container">
        <div class="d-flex flex-column flex-md-row justify-content-between align-items-center gap-2">
          <small class="text-muted">© 2025 tHerd Team · All rights reserved.</small>
          <div class="d-flex gap-3 small">
            <router-link to="/cs/faq" class="link-secondary text-decoration-none">FAQ</router-link>
            <router-link to="/cs/ticket" class="link-secondary text-decoration-none">客服工單</router-link>
            <router-link to="/cs/live-chat" class="link-secondary text-decoration-none">即時客服</router-link>
          </div>
        </div>
      </div>
    </footer>
  </div>
</template>

<script setup>
// 靜態頁，不需額外邏輯。
// 若未來要從後端帶 KPI/里程碑資料，可在此發 API。
</script>

<style scoped>
.about-wrap :is(h1,h2,h3,h4,h5) { letter-spacing: .5px; }

/* 時間軸 */
.timeline { position: relative; margin-left: 1rem; }
.timeline::before {
  content: ""; position: absolute; left: .5rem; top: 0; bottom: 0;
  width: 2px; background: #e9ecef;
}
.timeline-item { position: relative; padding-left: 2rem; margin-bottom: 1.25rem; }
.timeline-dot {
  position: absolute; left: 0; top: .25rem; width: 1rem; height: 1rem;
  border-radius: 50%; background: #4db4c1; /* 淺藍綠(與主色系相容) */
  box-shadow: 0 0 0 4px rgba(77,180,193,.15);
}
.timeline-content { background: #fff; border-radius: .75rem; padding: .75rem 1rem; box-shadow: 0 1px 6px rgba(0,0,0,.05); }

/* 深色區塊按鈕 hover 更醒目 */
.bg-dark .btn.btn-primary:hover { filter: brightness(1.05); }

/* RWD 微調 */
@media (max-width: 576px) {
  .display-5 { font-size: 2rem; }
}
</style>
