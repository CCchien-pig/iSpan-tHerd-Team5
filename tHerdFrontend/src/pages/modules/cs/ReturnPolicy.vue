<template>
  <div class="policy-wrap py-5">
    <div class="container">
      <h2 class="fw-bold mb-4 text-center main-color-green-text">退貨政策</h2>

      <p class="text-muted mb-4 text-center">
        我們希望您對每一次購物都感到滿意。若商品不符預期，請依以下流程辦理退貨。
      </p>

      <div class="card shadow-sm border-0 p-4">
        <h5 class="fw-bold mb-3">一、可退貨條件</h5>
        <ul>
          <li>商品須保持全新、完整包裝（含附件、吊牌、贈品等）。</li>
          <li>收到商品七日內（含例假日）提出退貨申請。</li>
          <li>若為瑕疵、錯誤出貨，請附上清晰照片以利確認。</li>
        </ul>

        <h5 class="fw-bold mt-4 mb-3">二、不可退貨情形</h5>
        <ul>
          <li>超過七日鑑賞期。</li>
          <li>商品經穿著、洗滌、或人為損壞。</li>
          <li>個人衛生用品、特價品除非瑕疵恕不退換。</li>
        </ul>

        <h5 class="fw-bold mt-4 mb-3">三、退貨流程</h5>
        <ol>
          <li>登入會員中心 →「我的訂單」→ 點選欲退貨的訂單。</li>
          <li>點擊「申請退貨」，填寫原因與上傳照片。</li>
          <li>客服確認後寄出退貨通知信件。</li>
          <li>寄回商品後，完成檢查即退款（約 3–5 個工作天）。</li>
        </ol>

        <h5 class="fw-bold mt-4 mb-3">四、退款方式</h5>
        <p>
          退款將依原付款方式退回。若為信用卡付款，退款時間依各發卡銀行作業而異。
        </p>

        <h5 class="fw-bold mt-4 mb-3">五、客服聯絡方式</h5>
        <p>
          若有退貨疑問，請透過
          <router-link to="/cs/ticket" class="link-primary">客服工單</router-link>
          或來信至 <a href="mailto:support@therd.example">support@therd.example</a>。
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.policy-wrap ul,
.policy-wrap ol {
  line-height: 1.8;
  margin-left: 1.25rem;
}
.policy-wrap h5 {
  color: var(--main-color, #3aa89b);
}
</style>
