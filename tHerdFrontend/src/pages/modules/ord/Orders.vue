<template>
  <div class="container my-5" style="max-width: 1200px;">
    <div class="mb-4">
      <h1 class="fw-bold text-center main-color-green-text">我的訂單</h1>
    </div>
    
    <ul class="nav nav-tabs justify-content-center mb-4 border-0">
      <li class="nav-item">
        <a class="nav-link fs-5 px-4" :class="{active: activeTab === 'orders'}" @click="activeTab = 'orders'" href="#" style="cursor: pointer;">
          <i class="bi bi-box-seam me-2"></i>我的訂單
        </a>
      </li>
      <li class="nav-item">
        <a class="nav-link fs-5 px-4" :class="{active: activeTab === 'rma'}" @click="activeTab = 'rma'; loadRmaList()" href="#" style="cursor: pointer;">
          <i class="bi bi-arrow-return-left me-2"></i>退換貨申請
        </a>
      </li>
    </ul>

    <!-- 訂單列表 -->
    <div v-if="activeTab === 'orders'">
      <div v-if="loading" class="text-center py-5">
        <div class="spinner-border main-color-green" role="status">
          <span class="visually-hidden">載入中...</span>
        </div>
      </div>
      
      <div v-else-if="orders.length === 0" class="alert alert-info text-center fs-5">
        <i class="bi bi-info-circle me-2"></i>目前沒有訂單
      </div>
      
      <div v-else class="row g-4">
        <div v-for="order in paginatedOrders" :key="order.orderId" class="col-12">
          <div class="card order-card shadow">
            <div class="card-body p-4">
              <div class="row align-items-center">
                <div class="col-md-7">
                  <h3 class="mb-3 fw-bold main-color-green-text">訂單號碼: {{ order.orderNo }}</h3>
                  
                  <div class="mb-2 fs-6">
                    <i class="bi bi-calendar3 me-2 text-muted"></i>
                    <span class="text-muted">訂購日期: {{ formatDate(order.createdDate) }}</span>
                  </div>
                  
                  <div class="mb-3 fs-5">
                    <i class="bi bi-currency-dollar me-2 text-muted"></i>
                    <span>總金額: </span>
                    <strong class="main-color-green-text">NT$ {{ order.totalAmount.toLocaleString() }}</strong>
                  </div>
                  
                  <div v-if="order.trackingNumber" class="text-muted" style="font-size: 0.9rem;">
                    <i class="bi bi-truck me-2"></i>物流單號: {{ order.trackingNumber }}
                  </div>
                </div>
                
                <div class="col-md-5">
                  <div class="d-flex flex-column gap-2 align-items-stretch">
                    <button class="btn teal-reflect-button fs-5" @click="viewDetail(order.orderId)">
                      <i class="bi bi-eye me-2"></i>查看詳情
                    </button>
                    
                    <button 
                      v-if="order.canReturn && !order.hasRmaRequest" 
                      class="btn orange-submit-btn fs-5"
                      @click="openRmaModal(order)">
                      <i class="bi bi-arrow-return-left me-2"></i>申請退換貨
                    </button>
                    
                    <button 
                      v-else-if="order.hasRmaRequest" 
                      class="btn silver-reflect-button fs-5" 
                      @click="goToRmaDetail(order.orderId)">
                      <i class="bi bi-check-circle me-2"></i>已申請退換貨
                    </button>
                    
                    <div v-if="order.canReturn && !order.hasRmaRequest && order.deliveredDate" class="text-center mt-1 fs-5 fw-semibold" style="color: #856404;">
                      <i class="bi bi-clock-history me-1"></i>{{ getReturnDaysLeft(order.deliveredDate) }}
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
      
      <!-- 訂單分頁 -->
      <div v-if="orders.length > orderPageSize" class="d-flex justify-content-center align-items-center gap-3 mt-4 py-3">
        <button 
          class="btn btn-sm main-color-green px-3 py-2"
          :disabled="orderCurrentPage === 1"
          :class="{ 'opacity-50': orderCurrentPage === 1 }"
          @click="orderCurrentPage--">
          <i class="bi bi-chevron-left"></i> 上一頁
        </button>
        
        <div class="d-flex align-items-center gap-2">
          <span class="text-muted">第</span>
          <input 
            type="number" 
            class="form-control form-control-sm text-center page-input" 
            v-model.number="pageInputOrder"
            @keyup.enter="goToOrderPage"
            :min="1"
            :max="orderTotalPages"
            style="width: 60px;">
          <span class="text-muted">/ {{ orderTotalPages }} 頁</span>
          <span class="text-muted ms-2">(共 {{ orders.length }} 筆)</span>
        </div>
        
        <button 
          class="btn btn-sm main-color-green px-3 py-2"
          :disabled="orderCurrentPage === orderTotalPages"
          :class="{ 'opacity-50': orderCurrentPage === orderTotalPages }"
          @click="orderCurrentPage++">
          下一頁 <i class="bi bi-chevron-right"></i>
        </button>
      </div>
    </div>

    <!-- 退換貨列表 -->
    <div v-if="activeTab === 'rma'">
      <div v-if="rmaLoading" class="text-center py-5">
        <div class="spinner-border main-color-green" style="width: 3rem; height: 3rem;" role="status">
          <span class="visually-hidden">載入中...</span>
        </div>
      </div>
      
      <div v-else-if="rmaList.length === 0" class="alert alert-info text-center fs-5 py-4">
        <i class="bi bi-info-circle me-2 fs-4"></i>
        <div class="mt-2">目前沒有退換貨申請</div>
      </div>
      
      <div v-else class="row g-4">
        <div 
          v-for="rma in paginatedRmaList" 
          :key="rma.returnRequestId" 
          class="col-12"
          :ref="'rma-' + rma.returnRequestId">
          <div class="card shadow-lg border-0" style="border-left: 6px solid rgb(255, 193, 7);">
            <div class="card-body p-4">
              <div class="d-flex justify-content-between align-items-start mb-3">
                <div>
                  <h3 class="mb-2 fw-bold main-color-green-text fs-4">申請單號: {{ rma.returnRequestId }}</h3>
                  <p class="mb-0 text-muted fs-5">
                    <i class="bi bi-receipt me-2"></i>訂單號碼: {{ rma.orderNo }}
                  </p>
                </div>
                <span :class="getRmaStatusClass(rma.status)" class="px-4 py-2 fs-5 fw-bold rounded-pill">
                  {{ getRmaStatusText(rma.status) }}
                </span>
              </div>
              
              <div class="bg-light p-4 rounded-3">
                <div class="row mb-3">
                  <div class="col-md-6">
                    <p class="mb-0 fs-5">
                      <i class="bi bi-tag me-2"></i>申請類型: 
                      <span :class="rma.requestType === 'refund' ? 'text-danger' : 'text-primary'" class="fw-bold">
                        {{ getRmaTypeText(rma.requestType) }}
                      </span>
                      <span class="ms-2 badge bg-secondary fs-6">{{ rma.scope === 'full' ? '整筆訂單' : '部分商品' }}</span>
                    </p>
                  </div>
                  <div class="col-md-6 text-end">
                    <p class="mb-0 text-muted fs-6">
                      <i class="bi bi-clock me-2"></i>申請時間: {{ formatDate(rma.createdDate) }}
                    </p>
                  </div>
                </div>
                
                <div class="mb-0">
                  <p class="fw-bold mb-2 fs-5"><i class="bi bi-chat-left-text me-2"></i>申請原因:</p>
                  <p class="mb-0 ps-3 fs-6" style="line-height: 1.6;">{{ rma.reasonText }}</p>
                </div>
                
                <div v-if="rma.reviewComment" class="alert alert-warning mt-3 mb-0 fs-6 border-warning" style="border-width: 2px;">
                  <i class="bi bi-exclamation-triangle-fill me-2"></i><strong>審核意見:</strong> {{ rma.reviewComment }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 退換貨分頁 -->
      <div v-if="rmaList.length > rmaPageSize" class="d-flex justify-content-center align-items-center gap-3 mt-4 py-3">
        <button 
          class="btn btn-sm main-color-green px-3 py-2"
          :disabled="rmaCurrentPage === 1"
          :class="{ 'opacity-50': rmaCurrentPage === 1 }"
          @click="rmaCurrentPage--">
          <i class="bi bi-chevron-left"></i> 上一頁
        </button>
        
        <div class="d-flex align-items-center gap-2">
          <span class="text-muted">第</span>
          <input 
            type="number" 
            class="form-control form-control-sm text-center page-input" 
            v-model.number="pageInputRma"
            @keyup.enter="goToRmaPage"
            :min="1"
            :max="rmaTotalPages"
            style="width: 60px;">
          <span class="text-muted">/ {{ rmaTotalPages }} 頁</span>
          <span class="text-muted ms-2">(共 {{ rmaList.length }} 筆)</span>
        </div>
        
        <button 
          class="btn btn-sm main-color-green px-3 py-2"
          :disabled="rmaCurrentPage === rmaTotalPages"
          :class="{ 'opacity-50': rmaCurrentPage === rmaTotalPages }"
          @click="rmaCurrentPage++">
          下一頁 <i class="bi bi-chevron-right"></i>
        </button>
      </div>
    </div>

    <!-- 訂單詳情 Modal -->
    <div class="modal fade" id="orderDetailModal" tabindex="-1">
      <div class="modal-dialog modal-xl">
        <div class="modal-content">
          <div class="modal-header main-color-green">
            <h3 class="modal-title main-color-white-text"><i class="bi bi-receipt me-2"></i>訂單詳情</h3>
            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
          </div>
          <div class="modal-body p-4" style="background-color: #f8f9fa;">
            <div v-if="orderDetail">
              
              <!-- 訂單狀態 - 白底 -->
              <div class="mb-4 p-4 rounded" style="background-color: white;">
                <h4 class="mb-3 main-color-green-text fw-bold fs-4"><i class="bi bi-info-circle me-2"></i>訂單狀態</h4>
                <div class="row">
                  <div class="col-md-4 mb-2">
                    <p class="mb-0 fs-5">
                      <strong>付款狀態: </strong>
                      <span :class="getPaymentStatusClass(orderDetail.order.paymentStatus)" class="px-3 py-2 fs-6">
                        {{ getPaymentStatusText(orderDetail.order.paymentStatus) }}
                      </span>
                    </p>
                  </div>
                  <div class="col-md-4 mb-2">
                    <p class="mb-0 fs-5">
                      <strong>配送狀態: </strong>
                      <span :class="getShippingStatusClass(orderDetail.order.shippingStatusId)" class="px-3 py-2 fs-6">
                        {{ getShippingStatusText(orderDetail.order.shippingStatusId) }}
                      </span>
                    </p>
                  </div>
                  <div class="col-md-4 mb-2">
                    <p class="mb-0 fs-5">
                      <strong>訂單狀態: </strong>
                      <span :class="getOrderStatusClass(orderDetail.order.orderStatusId)" class="px-3 py-2 fs-6">
                        {{ getOrderStatusText(orderDetail.order.orderStatusId) }}
                      </span>
                    </p>
                  </div>
                </div>
              </div>
              
              <!-- 訂單時間軸 - 灰底 -->
              <div class="mb-4 p-4 rounded" style="background-color: #e9ecef;">
                <h4 class="mb-3 main-color-green-text fw-bold fs-4"><i class="bi bi-truck me-2"></i>物流進度</h4>
                <div class="order-timeline">
                  <div 
                    v-for="(step, index) in timelineSteps" 
                    :key="index"
                    class="timeline-step"
                    :class="{ 'active': step.isActive, 'completed': step.isCompleted }">
                    <div class="timeline-icon">
                      <i :class="step.icon"></i>
                    </div>
                    <div class="timeline-content">
                      <div class="timeline-title fs-6">{{ step.title }}</div>
                      <div v-if="step.date" class="timeline-date" style="font-size: 0.85rem;">{{ step.date }}</div>
                    </div>
                    <div v-if="index < timelineSteps.length - 1" class="timeline-line"></div>
                  </div>
                </div>
              </div>
              
              <!-- 收件資訊 - 白底 -->
              <div class="mb-4 p-4 rounded" style="background-color: white;">
                <h4 class="mb-3 main-color-green-text fw-bold fs-4"><i class="bi bi-person-circle me-2"></i>收件資訊</h4>
                <div class="row">
                  <div class="col-md-6 mb-3">
                    <p class="mb-0 fs-6"><strong>姓名:</strong> {{ orderDetail.order.receiverName }}</p>
                  </div>
                  <div class="col-md-6 mb-3">
                    <p class="mb-0 fs-6"><strong>電話:</strong> {{ orderDetail.order.receiverPhone }}</p>
                  </div>
                  <div class="col-md-6 mb-3">
                    <p class="mb-0 fs-6"><strong>配送方式:</strong> {{ getLogisticsName(orderDetail.order.logisticsId) }}</p>
                  </div>
                  <div v-if="orderDetail.order.trackingNumber" class="col-md-6 mb-3">
                    <p class="mb-0 fs-6"><strong>物流單號:</strong> {{ orderDetail.order.trackingNumber }}</p>
                  </div>
                  <div class="col-12">
                    <p class="mb-0 fs-6"><strong>地址:</strong> {{ orderDetail.order.receiverAddress }}</p>
                  </div>
                </div>
              </div>
              
              <!-- 訂購商品 - 灰底 -->
              <div class="mb-4 p-4 rounded" style="background-color: #e9ecef;">
                <h4 class="mb-3 main-color-green-text fw-bold fs-4"><i class="bi bi-box me-2"></i>訂購商品</h4>
                <div class="table-responsive">
                  <table class="table table-hover clean-table" style="background-color: white;">
                    <thead style="background-color: #f8f9fa; border-bottom: 2px solid #dee2e6;">
                      <tr>
                        <th class="fs-6" style="width: 38.1%;">商品</th>
                        <th class="fs-6" style="width: 9.52%;">規格</th>
                        <th class="text-end fs-6" style="width: 19.05%;">單價</th>
                        <th class="text-center fs-6" style="width: 9.52%;">數量</th>
                        <th class="text-end fs-6" style="width: 23.81%;">小計</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="item in orderDetail.items" :key="item.orderItemId">
                        <td class="fs-6">{{ item.productName }}</td>
                        <td class="text-muted fs-6">{{ item.specName || '-' }}</td>
                        <td class="text-end fs-6">NT$ {{ item.unitPrice.toLocaleString() }}</td>
                        <td class="text-center fs-6">{{ item.qty }}</td>
                        <td class="text-end fs-6">NT$ {{ (item.unitPrice * item.qty).toLocaleString() }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
              
              <!-- 金額明細 - 白底 -->
              <div class="mb-3 p-4 rounded" style="background-color: white;">
                <h4 class="mb-3 main-color-green-text fw-bold fs-4"><i class="bi bi-calculator me-2"></i>金額明細</h4>
                <div class="row">
                  <div class="col-md-6 offset-md-6">
                    <div class="d-flex justify-content-between mb-3 fs-5">
                      <span>小計:</span>
                      <span>NT$ {{ orderDetail.order.subtotal.toLocaleString() }}</span>
                    </div>
                    <div class="d-flex justify-content-between mb-3 fs-5">
                      <span>運費:</span>
                      <span>NT$ {{ orderDetail.order.shippingFee.toLocaleString() }}</span>
                    </div>
                    <div v-if="orderDetail.order.discountTotal > 0" class="d-flex justify-content-between mb-3 text-danger fs-5">
                      <span>折扣:</span>
                      <span>- NT$ {{ orderDetail.order.discountTotal.toLocaleString() }}</span>
                    </div>
                    <hr class="my-3">
                    <div class="d-flex justify-content-between fs-4">
                      <strong>總金額:</strong>
                      <strong class="main-color-green-text">NT$ {{ orderDetail.order.totalAmount.toLocaleString() }}</strong>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 退換貨申請 Modal -->
    <div class="modal fade" id="rmaModal" tabindex="-1">
      <div class="modal-dialog modal-xl">
        <div class="modal-content">
          <div class="modal-header orange-submit-bg py-3">
            <h3 class="modal-title text-dark fw-bold fs-4"><i class="bi bi-arrow-return-left me-2"></i>申請退換貨</h3>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <div class="modal-body p-4">
            <div v-if="selectedOrder">
              <div class="alert alert-info mb-4 py-3">
                <p class="mb-0 fs-6"><i class="bi bi-info-circle me-2"></i>訂單號碼: <strong>{{ selectedOrder.orderNo }}</strong></p>
              </div>
              
              <div class="mb-4">
                <label class="form-label fw-bold mb-3 fs-5">申請類型 *</label>
                <div class="d-flex gap-4">
                  <div class="form-check">
                    <input class="form-check-input" type="radio" value="refund" v-model="rmaForm.requestType" id="typeRefund" style="width: 1.3rem; height: 1.3rem;">
                    <label class="form-check-label ms-2 fs-5" for="typeRefund">退款</label>
                  </div>
                  <div class="form-check">
                    <input class="form-check-input" type="radio" value="reship" v-model="rmaForm.requestType" id="typeReship" style="width: 1.3rem; height: 1.3rem;">
                    <label class="form-check-label ms-2 fs-5" for="typeReship">補寄</label>
                  </div>
                </div>
              </div>
              
              <div class="mb-4">
                <label class="form-label fw-bold mb-3 fs-5">退換範圍 *</label>
                <div class="d-flex gap-4">
                  <div class="form-check">
                    <input class="form-check-input" type="radio" value="full" v-model="rmaForm.scope" id="scopeFull" style="width: 1.3rem; height: 1.3rem;">
                    <label class="form-check-label ms-2 fs-5" for="scopeFull">整筆訂單</label>
                  </div>
                  <div class="form-check">
                    <input class="form-check-input" type="radio" value="partial" v-model="rmaForm.scope" id="scopePartial" style="width: 1.3rem; height: 1.3rem;">
                    <label class="form-check-label ms-2 fs-5" for="scopePartial">部分商品</label>
                  </div>
                </div>
              </div>
              
              <div class="mb-4">
                <div class="d-flex justify-content-between align-items-center mb-3">
                  <label class="form-label fw-bold mb-0 fs-5">原因說明 *</label>
                  <button type="button" class="btn btn-outline-secondary px-3 py-2 fs-6" @click="fillDemoReason">
                    <i class="bi bi-file-text me-1"></i>填入範例
                  </button>
                </div>
                <textarea 
                  class="form-control fs-5" 
                  v-model="rmaForm.reasonText" 
                  rows="5" 
                  placeholder="請詳細說明退換貨原因..."
                  style="line-height: 1.6; font-weight: normal;"></textarea>
              </div>
              
              <div class="mb-4">
                <label class="form-label fw-bold mb-3 fs-5">上傳照片（選填）</label>
                <div class="photo-upload-area border rounded p-3">
                  <input 
                    type="file" 
                    ref="photoInput"
                    @change="handlePhotoUpload" 
                    accept="image/*"
                    multiple
                    class="d-none">
                  <button 
                    type="button" 
                    class="btn btn-outline-secondary w-100 py-2 fs-6"
                    @click="$refs.photoInput.click()">
                    <i class="bi bi-camera me-2"></i>選擇照片
                  </button>
                  <div v-if="rmaForm.photos.length > 0" class="mt-3">
                    <div class="d-flex flex-wrap gap-3">
                      <div v-for="(photo, index) in rmaForm.photos" :key="index" class="position-relative">
                        <img :src="photo.preview" class="rounded border" style="width: 110px; height: 110px; object-fit: cover;">
                        <button 
                          type="button" 
                          class="btn btn-danger position-absolute top-0 end-0 m-1"
                          style="width: 30px; height: 30px; padding: 0; line-height: 1;"
                          @click="removePhoto(index)">
                          <i class="bi bi-x"></i>
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              
              <div class="d-flex justify-content-end gap-3 mt-4">
                <button type="button" class="btn btn-secondary px-4 py-2 fs-6" data-bs-dismiss="modal">取消</button>
                <button type="button" class="btn orange-submit-btn px-4 py-2 fs-6 fw-bold" @click="submitRma">
                  <i class="bi bi-send me-2"></i>提交申請
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { Modal } from 'bootstrap';
import { http } from '@/api/http';

export default {
  name: 'Orders',
  data() {
    return {
      activeTab: 'orders',
      orders: [],
      rmaList: [],
      orderDetail: null,
      selectedOrder: null,
      loading: true,
      rmaLoading: false,
      rmaForm: {
        requestType: 'refund',
        scope: 'full',
        reasonText: '',
        photos: []
      },
      orderCurrentPage: 1,
      orderPageSize: 10,
      pageInputOrder: 1,
      rmaCurrentPage: 1,
      rmaPageSize: 10,
      pageInputRma: 1,
      highlightRmaId: null
    };
  },
  computed: {
    paginatedOrders() {
      const start = (this.orderCurrentPage - 1) * this.orderPageSize;
      const end = start + this.orderPageSize;
      return this.orders.slice(start, end);
    },
    orderTotalPages() {
      return Math.ceil(this.orders.length / this.orderPageSize);
    },
    paginatedRmaList() {
      const start = (this.rmaCurrentPage - 1) * this.rmaPageSize;
      const end = start + this.rmaPageSize;
      return this.rmaList.slice(start, end);
    },
    rmaTotalPages() {
      return Math.ceil(this.rmaList.length / this.rmaPageSize);
    },
    timelineSteps() {
      if (!this.orderDetail) return [];
      
      const order = this.orderDetail.order;
      const shippingStatus = order.shippingStatusId;
      
      const steps = [
        {
          title: '訂單成立',
          icon: 'bi bi-check-circle-fill',
          date: order.createdDate ? this.formatDateTime(order.createdDate) : null,
          isCompleted: true,
          isActive: false
        },
        {
          title: '準備出貨',
          icon: 'bi bi-box-seam',
          date: ['processing', 'picking', 'packing', 'shipped', 'shipping', 'delivered'].includes(shippingStatus) && order.revisedDate
            ? this.formatDateTime(order.revisedDate) : null,
          isCompleted: ['picking', 'packing', 'shipped', 'shipping', 'delivered'].includes(shippingStatus),
          isActive: shippingStatus === 'processing'
        },
        {
          title: '已出貨',
          icon: 'bi bi-truck',
          date: ['shipped', 'shipping', 'delivered'].includes(shippingStatus) && order.revisedDate
            ? this.formatDateTime(order.revisedDate) : null,
          isCompleted: ['shipped', 'shipping', 'delivered'].includes(shippingStatus),
          isActive: ['shipped', 'shipping'].includes(shippingStatus) && !order.deliveredDate
        },
        {
          title: '已收貨',
          icon: 'bi bi-house-check-fill',
          date: order.deliveredDate ? this.formatDateTime(order.deliveredDate) : null,
          isCompleted: !!order.deliveredDate,
          isActive: !!order.deliveredDate
        }
      ];
      
      return steps;
    }
  },
  watch: {
    orderCurrentPage(val) {
      this.pageInputOrder = val;
    },
    rmaCurrentPage(val) {
      this.pageInputRma = val;
    }
  },
  mounted() {
    this.loadOrders();
  },
  methods: {
    async loadOrders() {
      this.loading = true;
      try {
        const res = await http.get('/ord/OrdersApi/member/me');
        this.orders = res.data || [];
        this.orderCurrentPage = 1;
        this.pageInputOrder = 1;
      } catch (err) {
        console.error('載入訂單失敗:', err);
        alert('載入訂單失敗: ' + (err.response?.data || err.message));
      } finally {
        this.loading = false;
      }
    },
    async viewDetail(orderId) {
      try {
        const res = await http.get(`/ord/OrdersApi/${orderId}`);
        this.orderDetail = res.data;
        const modal = new Modal(document.getElementById('orderDetailModal'));
        modal.show();
      } catch (err) {
        console.error('載入訂單詳情失敗:', err);
        alert('載入詳情失敗');
      }
    },
    async loadRmaList() {
      this.rmaLoading = true;
      try {
        const res = await http.get('/ord/OrdersApi/member/me/rma');
        this.rmaList = res.data || [];
      } catch (err) {
        console.error('載入退換貨列表失敗:', err);
        alert('載入退換貨申請失敗');
      } finally {
        this.rmaLoading = false;
      }
    },
    async goToRmaDetail(orderId) {
      this.activeTab = 'rma';
      await this.loadRmaList();
      const rmaIndex = this.rmaList.findIndex(rma => rma.orderId === orderId);
      
      if (rmaIndex !== -1) {
        const targetPage = Math.floor(rmaIndex / this.rmaPageSize) + 1;
        this.rmaCurrentPage = targetPage;
        this.pageInputRma = targetPage;
        
        this.$nextTick(() => {
          const rma = this.rmaList[rmaIndex];
          const element = this.$refs['rma-' + rma.returnRequestId];
          if (element && element[0]) {
            element[0].scrollIntoView({ behavior: 'smooth', block: 'center' });
            this.highlightRmaId = rma.returnRequestId;
            setTimeout(() => {
              this.highlightRmaId = null;
            }, 2000);
          }
        });
      }
    },
    goToOrderPage() {
      const page = parseInt(this.pageInputOrder);
      if (page >= 1 && page <= this.orderTotalPages) {
        this.orderCurrentPage = page;
      } else {
        this.pageInputOrder = this.orderCurrentPage;
      }
    },
    goToRmaPage() {
      const page = parseInt(this.pageInputRma);
      if (page >= 1 && page <= this.rmaTotalPages) {
        this.rmaCurrentPage = page;
      } else {
        this.pageInputRma = this.rmaCurrentPage;
      }
    },
    openRmaModal(order) {
      this.selectedOrder = order;
      this.rmaForm = {
        requestType: 'refund',
        scope: 'full',
        reasonText: '',
        photos: []
      };
      const modal = new Modal(document.getElementById('rmaModal'));
      modal.show();
    },
    fillDemoReason() {
      this.rmaForm.reasonText = '商品收到時發現外包裝破損,內部商品有明顯瑕疵,與網站上描述不符。希望能退款或換貨處理,謝謝。';
    },
    handlePhotoUpload(event) {
      const files = event.target.files;
      for (let i = 0; i < files.length; i++) {
        const file = files[i];
        const reader = new FileReader();
        reader.onload = (e) => {
          this.rmaForm.photos.push({
            file: file,
            preview: e.target.result
          });
        };
        reader.readAsDataURL(file);
      }
    },
    removePhoto(index) {
      this.rmaForm.photos.splice(index, 1);
    },
    async submitRma() {
      if (!this.rmaForm.reasonText.trim()) {
        alert('請填寫原因說明');
        return;
      }
      
      try {
        await http.post('/ord/OrdersApi/rma', {
          orderId: this.selectedOrder.orderId,
          requestType: this.rmaForm.requestType,
          refundScope: this.rmaForm.scope,
          reasonCode: 'damaged',
          reason: this.rmaForm.reasonText,
          items: []
        });
        alert('申請成功');
        Modal.getInstance(document.getElementById('rmaModal')).hide();
        this.loadOrders();
      } catch (err) {
        console.error('提交申請失敗:', err);
        alert(err.response?.data || '申請失敗');
      }
    },
    showAlreadyApplied() {
      alert('此訂單已有退換貨申請');
    },
    formatDate(dateStr) {
      if (!dateStr) return '';
      const date = new Date(dateStr);
      return date.toLocaleDateString('zh-TW');
    },
    formatDateTime(dateStr) {
      if (!dateStr) return '';
      const date = new Date(dateStr);
      const year = date.getFullYear();
      const month = String(date.getMonth() + 1).padStart(2, '0');
      const day = String(date.getDate()).padStart(2, '0');
      const hours = String(date.getHours()).padStart(2, '0');
      const minutes = String(date.getMinutes()).padStart(2, '0');
      return `${year}-${month}-${day} ${hours}:${minutes}`;
    },
    getReturnDaysLeft(deliveredDate) {
      if (!deliveredDate) return '尚未送達';
      const delivered = new Date(deliveredDate);
      const now = new Date();
      const diffTime = now - delivered;
      const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));
      const daysLeft = 7 - diffDays;
      if (daysLeft <= 0) return '退換貨期限已過';
      return `還有 ${daysLeft} 天可申請退換貨`;
    },
    getLogisticsName(logisticsId) {
      const map = {
        1000: '宅配到府（順豐速運）',
        1001: '低溫宅配（黑貓宅急便）',
        1002: '超商店到店（7-ELEVEN）',
        1003: 'i郵箱（中華郵政）',
        1004: '掛號包裹（中華郵政）'
      };
      return map[logisticsId] || '宅配到府';
    },
    getPaymentStatusText(status) {
      const map = {
        pending: '待付款',
        paid: '已付款',
        refunded: '已退款',
        cancelled: '已取消'
      };
      return map[status] || status;
    },
    getPaymentStatusClass(status) {
      const map = {
        pending: 'badge orange-badge',
        paid: 'badge bg-success',
        refunded: 'badge bg-info',
        cancelled: 'badge bg-secondary'
      };
      return map[status] || 'badge bg-secondary';
    },
    getShippingStatusText(status) {
      const map = {
        pending: '待出貨',
        processing: '處理中',
        shipped: '已出貨',
        delivered: '已送達',
        picking: '拿貨中',
        packing: '已打包',
        shipping: '配送中',
        returned: '退回',
        unshipped: '未出貨'
      };
      return map[status] || status;
    },
    getShippingStatusClass(status) {
      const map = {
        pending: 'badge bg-secondary',
        processing: 'badge bg-info',
        shipped: 'badge',
        delivered: 'badge bg-success',
        unshipped: 'badge bg-secondary',
        picking: 'badge bg-info',
        packing: 'badge bg-info',
        shipping: 'badge'
      };
      const baseClass = map[status] || 'badge bg-secondary';
      if (status === 'shipped' || status === 'shipping') {
        return baseClass + ' main-color-green';
      }
      return baseClass;
    },
    getOrderStatusText(status) {
      const map = {
        pending: '待處理',
        processing: '處理中',
        confirmed: '已確認',
        completed: '已完成',
        cancelled: '已取消',
        done: '處理完成'
      };
      return map[status] || status;
    },
    getOrderStatusClass(status) {
      const map = {
        pending: 'badge orange-badge',
        processing: 'badge bg-info',
        confirmed: 'badge main-color-green',
        completed: 'badge bg-success',
        cancelled: 'badge bg-danger',
        done: 'badge bg-success'
      };
      return map[status] || 'badge bg-secondary';
    },
    getRmaTypeText(type) {
      return type === 'refund' ? '退款' : '補寄';
    },
    getRmaStatusText(status) {
      const map = {
        pending: '待審核',
        review: '審核中',
        refunding: '退款中',
        done: '已完成',
        rejected: '已拒絕'
      };
      return map[status] || status;
    },
    getRmaStatusClass(status) {
      const map = {
        pending: 'badge orange-badge',
        review: 'badge bg-info',
        refunding: 'badge main-color-green',
        done: 'badge bg-success',
        rejected: 'badge bg-danger'
      };
      return map[status] || 'badge bg-secondary';
    }
  }
};
</script>

<style scoped>
.main-color-green-text {
  color: rgb(0, 112, 131);
}

.main-color-white-text {
  color: white;
}

.main-color-green {
  background-color: rgb(0, 112, 131);
  color: white;
}

.orange-badge {
  background-color: rgb(255, 193, 7);
  color: #000;
}

.orange-submit-btn {
  background-color: rgb(255, 193, 7);
  color: #000;
  border: none;
  font-weight: 600;
  transition: all 0.3s;
}

.orange-submit-btn:hover {
  background-color: rgb(255, 171, 0);
  color: #000;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(255, 193, 7, 0.4);
}

.orange-submit-bg {
  background-color: rgb(255, 193, 7);
  border-bottom: 2px solid rgb(255, 171, 0);
}

.teal-reflect-button {
  color: rgb(248, 249, 250) !important;
}

.nav-tabs {
  border-bottom: 3px solid #dee2e6;
}

.nav-tabs .nav-link {
  color: #6c757d;
  font-weight: 600;
  border: none;
  background-color: transparent;
  transition: all 0.3s;
}

.nav-tabs .nav-link.active {
  color: rgb(0, 112, 131);
  background-color: transparent;
  border-bottom: 4px solid rgb(0, 112, 131);
}

.nav-tabs .nav-link:hover {
  color: rgb(0, 147, 171);
  border-color: transparent;
}

.order-card {
  border-left: 6px solid rgb(0, 112, 131);
  transition: all 0.3s ease;
}

.order-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 8px 25px rgba(0, 112, 131, 0.25) !important;
}

.page-input {
  border: 2px solid #dee2e6;
  font-weight: 600;
}

.page-input:focus {
  border-color: rgb(0, 112, 131);
  box-shadow: 0 0 0 0.2rem rgba(0, 112, 131, 0.25);
}

.clean-table {
  border-collapse: collapse;
}

.clean-table thead th {
  padding: 14px 16px;
  font-weight: 500;
  color: #495057;
  text-align: left;
}

.clean-table tbody td {
  padding: 14px 16px;
  color: #212529;
  vertical-align: middle;
  border-bottom: 1px solid #e9ecef;
}

.clean-table tbody tr:last-child td {
  border-bottom: none;
}

.clean-table tbody tr:hover {
  background-color: #f8f9fa;
}

.order-timeline {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: 25px 20px;
  background-color: transparent;
  border-radius: 8px;
  position: relative;
}

.timeline-step {
  display: flex;
  flex-direction: column;
  align-items: center;
  flex: 1;
  position: relative;
  z-index: 2;
}

.timeline-icon {
  width: 50px;
  height: 50px;
  border-radius: 50%;
  background-color: #e0e0e0;
  border: 3px solid #e0e0e0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  color: #999;
  transition: all 0.3s ease;
  margin-bottom: 15px;
}

.timeline-step.completed .timeline-icon,
.timeline-step.active .timeline-icon {
  background-color: rgb(0, 112, 131);
  border-color: rgb(0, 112, 131);
  color: white;
}

.timeline-content {
  text-align: center;
}

.timeline-title {
  font-weight: 700;
  color: #666;
  margin-bottom: 6px;
}

.timeline-step.completed .timeline-title,
.timeline-step.active .timeline-title {
  color: rgb(0, 112, 131);
}

.timeline-date {
  color: #999;
  font-weight: 500;
}

.timeline-step.completed .timeline-date,
.timeline-step.active .timeline-date {
  color: rgb(0, 112, 131);
  font-weight: 600;
}

.timeline-line {
  position: absolute;
  top: 25px;
  left: 50%;
  width: 100%;
  height: 3px;
  background-color: #e0e0e0;
  z-index: 1;
}

.timeline-step.completed .timeline-line {
  background-color: rgb(0, 112, 131);
}

@media (max-width: 768px) {
  .order-timeline {
    flex-direction: column;
    padding: 15px;
  }
  
  .timeline-step {
    flex-direction: row;
    align-items: center;
    width: 100%;
    margin-bottom: 15px;
  }
  
  .timeline-icon {
    margin-bottom: 0;
    margin-right: 12px;
  }
  
  .timeline-content {
    text-align: left;
    flex: 1;
  }
  
  .timeline-line {
    display: none;
  }
}
</style>