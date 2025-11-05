<template>
  <div class="container my-5" style="max-width: 1200px;">
    <div class="mb-4">
      <h1 class="fw-bold text-center main-color-green-text">我的訂單</h1>
    </div>
    
    <ul class="nav nav-tabs justify-content-center mb-4 border-0">
      <li class="nav-item">
        <a class="nav-link fs-4 px-4" :class="{active: activeTab === 'orders'}" @click="activeTab = 'orders'" href="#" style="cursor: pointer;">
          <i class="bi bi-box-seam me-2"></i>我的訂單
        </a>
      </li>
      <li class="nav-item">
        <a class="nav-link fs-4 px-4" :class="{active: activeTab === 'rma'}" @click="activeTab = 'rma'; loadRmaList()" href="#" style="cursor: pointer;">
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
      
      <div v-else-if="orders.length === 0" class="alert alert-info text-center fs-4">
        <i class="bi bi-info-circle me-2"></i>目前沒有訂單
      </div>
      
      <div v-else class="row g-4">
        <div v-for="order in orders" :key="order.orderId" class="col-12">
          <div class="card order-card shadow">
            <div class="card-body p-4">
              <div class="row align-items-center">
                <div class="col-md-7">
                  <h3 class="mb-3 fw-bold main-color-green-text">訂單號碼: {{ order.orderNo }}</h3>
                  
                  <div class="mb-2 fs-5">
                    <i class="bi bi-calendar3 me-2 text-muted"></i>
                    <span class="text-muted">訂購日期: {{ formatDate(order.createdDate) }}</span>
                  </div>
                  
                  <div class="mb-3 fs-4">
                    <i class="bi bi-currency-dollar me-2 text-muted"></i>
                    <span>總金額: </span>
                    <strong class="main-color-green-text">NT$ {{ order.totalAmount.toLocaleString() }}</strong>
                  </div>
                  
                  <div v-if="order.trackingNumber" class="text-muted fs-6">
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
                      class="btn btn-outline-primary fs-5"
                      @click="openRmaModal(order)">
                      <i class="bi bi-arrow-return-left me-2"></i>申請退換貨
                    </button>
                    
                    <button 
                      v-else-if="order.hasRmaRequest" 
                      class="btn silver-reflect-button fs-5" 
                      @click="showAlreadyApplied">
                      <i class="bi bi-check-circle me-2"></i>已申請退換貨
                    </button>
                    
                    <div v-if="order.canReturn && !order.hasRmaRequest" class="badge orange-badge py-2 fs-6">
                      <i class="bi bi-check-circle me-1"></i>可申請退換貨 ({{ getRemainingDays(order.deliveredDate) }}天內)
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- RMA 列表 -->
    <div v-if="activeTab === 'rma'">
      <div v-if="loadingRma" class="text-center py-5">
        <div class="spinner-border main-color-green" role="status">
          <span class="visually-hidden">載入中...</span>
        </div>
      </div>
      
      <div v-else-if="rmaList.length === 0" class="alert alert-info text-center fs-4">
        <i class="bi bi-info-circle me-2"></i>目前沒有退換貨申請
      </div>
      
      <div v-else class="row g-4">
        <div v-for="rma in rmaList" :key="rma.returnRequestId" class="col-12">
          <div class="card shadow">
            <div class="card-body p-4">
              <div class="d-flex justify-content-between align-items-start mb-3">
                <div>
                  <h3 class="mb-2 fw-bold main-color-green-text">申請單號: {{ rma.rmaId }}</h3>
                  <p class="text-muted mb-0 fs-5">訂單號碼: {{ rma.orderNo }}</p>
                </div>
                <span :class="getRmaStatusClass(rma.status)" class="fs-5 px-4 py-2">
                  {{ getRmaStatusText(rma.status) }}
                </span>
              </div>
              
              <div class="bg-light p-4 rounded">
                <p class="mb-3 fs-5">
                  <strong>申請類型:</strong> {{ getRmaTypeText(rma.requestType) }}
                  <span class="ms-2 text-muted">({{ rma.refundScope === 'order' ? '整筆訂單' : '部分商品' }})</span>
                </p>
                <p class="mb-3 fs-5"><strong>申請原因:</strong> {{ rma.reasonText }}</p>
                <p class="mb-0 text-muted fs-6">
                  <i class="bi bi-clock me-1"></i>申請時間: {{ formatDate(rma.createdDate) }}
                </p>
                <p v-if="rma.reviewComment" class="mb-0 alert alert-warning mt-3 fs-5">
                  <strong>審核意見:</strong> {{ rma.reviewComment }}
                </p>
              </div>
            </div>
          </div>
        </div>
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
          <div class="modal-body p-4">
            <div v-if="orderDetail">
              <!-- 訂單狀態 -->
              <div class="mb-4 p-4 bg-light rounded">
                <h4 class="mb-3 main-color-green-text"><i class="bi bi-info-circle me-2"></i>訂單狀態</h4>
                <div class="row">
                  <div class="col-md-4 mb-3">
                    <p class="mb-2 fs-5"><strong>付款狀態:</strong></p>
                    <span :class="getPaymentStatusClass(orderDetail.order.paymentStatus)" class="fs-5 px-3 py-2">
                      {{ getPaymentStatusText(orderDetail.order.paymentStatus) }}
                    </span>
                  </div>
                  <div class="col-md-4 mb-3">
                    <p class="mb-2 fs-5"><strong>配送狀態:</strong></p>
                    <span :class="getShippingStatusClass(orderDetail.order.shippingStatusId)" class="fs-5 px-3 py-2">
                      {{ getShippingStatusText(orderDetail.order.shippingStatusId) }}
                    </span>
                  </div>
                  <div class="col-md-4 mb-3">
                    <p class="mb-2 fs-5"><strong>訂單狀態:</strong></p>
                    <span :class="getOrderStatusClass(orderDetail.order.orderStatusId)" class="fs-5 px-3 py-2">
                      {{ getOrderStatusText(orderDetail.order.orderStatusId) }}
                    </span>
                  </div>
                </div>
              </div>
              
              <!-- 收件資訊 -->
              <div class="mb-4">
                <h4 class="border-bottom pb-3 mb-3 main-color-green-text"><i class="bi bi-person-circle me-2"></i>收件資訊</h4>
                <div class="row fs-5">
                  <div class="col-md-6 mb-3">
                    <strong>姓名:</strong> {{ orderDetail.order.receiverName }}
                  </div>
                  <div class="col-md-6 mb-3">
                    <strong>電話:</strong> {{ orderDetail.order.receiverPhone }}
                  </div>
                  <div class="col-12 mb-3">
                    <strong>地址:</strong> {{ orderDetail.order.receiverAddress }}
                  </div>
                  <div v-if="orderDetail.order.trackingNumber" class="col-12">
                    <strong>物流單號:</strong> {{ orderDetail.order.trackingNumber }}
                  </div>
                </div>
              </div>
              
              <!-- 訂購商品 -->
              <div class="mb-4">
                <h4 class="border-bottom pb-3 mb-3 main-color-green-text"><i class="bi bi-box me-2"></i>訂購商品</h4>
                <div class="table-responsive">
                  <table class="table table-hover fs-5">
                    <thead class="main-color-green main-color-white-text">
                      <tr>
                        <th>商品</th>
                        <th>規格</th>
                        <th class="text-end">單價</th>
                        <th class="text-center">數量</th>
                        <th class="text-end">小計</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="item in orderDetail.items" :key="item.orderItemId">
                        <td>{{ item.productName }}</td>
                        <td><span class="badge" style="background-color: rgb(77, 180, 193); font-size: 1rem;">{{ item.specCode }}</span></td>
                        <td class="text-end">NT$ {{ item.unitPrice.toLocaleString() }}</td>
                        <td class="text-center">{{ item.qty }}</td>
                        <td class="text-end">NT$ {{ item.subtotal.toLocaleString() }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
              
              <!-- 金額總計 -->
              <div class="p-4 rounded" style="background-color: #e8f4f8;">
                <div class="d-flex justify-content-between mb-3 fs-4">
                  <span>商品小計:</span>
                  <span>NT$ {{ orderDetail.order.subtotal.toLocaleString() }}</span>
                </div>
                <div class="d-flex justify-content-between mb-3 fs-4">
                  <span>折扣:</span>
                  <span class="text-danger">-NT$ {{ orderDetail.order.discountTotal.toLocaleString() }}</span>
                </div>
                <div class="d-flex justify-content-between mb-4 fs-4">
                  <span>運費:</span>
                  <span>NT$ {{ orderDetail.order.shippingFee.toLocaleString() }}</span>
                </div>
                <div class="d-flex justify-content-between border-top pt-3">
                  <h3 class="mb-0 fw-bold">總計:</h3>
                  <h3 class="mb-0 fw-bold main-color-green-text">NT$ {{ orderDetail.order.totalAmount.toLocaleString() }}</h3>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 退換貨 Modal -->
    <div class="modal fade" id="rmaModal" tabindex="-1">
      <div class="modal-dialog modal-lg">
        <div class="modal-content">
          <div class="modal-header" style="background-color: rgb(255, 193, 7);">
            <h3 class="modal-title"><i class="bi bi-exclamation-triangle me-2"></i>申請退換貨</h3>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <div class="modal-body p-4">
            <div class="mb-4">
              <label class="form-label fs-4 fw-bold"><i class="bi bi-list-ul me-2"></i>申請類型</label>
              <select class="form-select form-select-lg fs-5" v-model="rmaForm.requestType">
                <option value="refund">退款</option>
                <option value="reship">補寄</option>
              </select>
            </div>
            <div class="mb-4">
              <label class="form-label fs-4 fw-bold"><i class="bi bi-diagram-3 me-2"></i>退款範圍</label>
              <select class="form-select form-select-lg fs-5" v-model="rmaForm.refundScope">
                <option value="order">整筆訂單</option>
                <option value="items">部分商品</option>
              </select>
            </div>
            <div class="mb-4">
              <label class="form-label fs-4 fw-bold"><i class="bi bi-tags me-2"></i>原因類型</label>
              <select class="form-select form-select-lg fs-5" v-model="rmaForm.reasonCode">
                <option value="damaged">商品損壞</option>
                <option value="missing">缺件</option>
                <option value="wrong">寄錯商品</option>
                <option value="quality">品質問題</option>
                <option value="other">其他</option>
              </select>
            </div>
            <div class="mb-4">
              <label class="form-label fs-4 fw-bold"><i class="bi bi-pencil-square me-2"></i>詳細說明</label>
              <textarea class="form-control form-control-lg fs-5" v-model="rmaForm.reason" rows="4" placeholder="請詳細描述退換貨原因..."></textarea>
            </div>
            <div class="mb-4">
              <label class="form-label fs-4 fw-bold"><i class="bi bi-image me-2"></i>上傳照片</label>
              <input type="file" class="form-control form-control-lg" accept="image/*" multiple>
              <small class="text-muted fs-6">請上傳商品照片以利審核（最多3張）</small>
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn silver-reflect-button fs-5 px-5" data-bs-dismiss="modal">取消</button>
            <button type="button" class="btn orange-submit-btn fs-5 px-5" @click="submitRma">
              <i class="bi bi-send me-2"></i>送出申請
            </button>
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
     userNumberId: 0,
      orders: [],
      orderDetail: null,
      loading: false,
      activeTab: 'orders',
      rmaList: [],
      loadingRma: false,
      rmaForm: {
        orderId: null,
        requestType: 'refund',
        refundScope: 'order',
        reasonCode: 'damaged',
        reason: '',
        items: []
      }
    };
  },
  mounted() {
    this.loadOrders();
  },
  methods: {
    async loadOrders() {
      this.loading = true;
      try {
      const res = await http.get(`/ord/OrdersApi/member/me`);
        this.orders = res.data;
      } catch (err) {
        console.error(err);
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
        console.error(err);
        alert('載入詳情失敗');
      }
    },
    showAlreadyApplied() {
      alert('此訂單已經申請過退換貨，無法重複申請！');
    },
    async openRmaModal(order) {
      if (order.hasRmaRequest) {
        alert('此訂單已經申請過退換貨，無法重複申請！');
        return;
      }
      if (!order.canReturn) {
        alert('此訂單無法申請退換貨！');
        return;
      }

      this.rmaForm.orderId = order.orderId;
      this.rmaForm.requestType = 'refund';
      this.rmaForm.refundScope = 'order';
      this.rmaForm.reasonCode = 'damaged';
      this.rmaForm.reason = '';

      try {
       const res = await http.get(`/ord/OrdersApi/${order.orderId}`);
        this.orderDetail = res.data;
      } catch (err) {
        console.error(err);
      }

      const modal = new Modal(document.getElementById('rmaModal'));
      modal.show();
    },
    async submitRma() {
      if (!this.rmaForm.reason.trim()) {
        alert('請填寫退換貨原因');
        return;
      }

      let items = [];
      if (this.rmaForm.refundScope === 'items' && this.orderDetail?.items) {
        items = this.orderDetail.items.map(item => ({
          orderItemId: item.orderItemId,
          qty: item.qty
        }));
      }

      try {
        await http.post('/ord/OrdersApi/rma', {
          orderId: this.rmaForm.orderId,
        //  userNumberId: this.userNumberId,
          requestType: this.rmaForm.requestType,
          refundScope: this.rmaForm.refundScope,
          reasonCode: this.rmaForm.reasonCode,
          reason: this.rmaForm.reason,
          items
        });
        alert('申請成功！');
        Modal.getInstance(document.getElementById('rmaModal')).hide();
        await this.loadOrders();
      } catch (err) {
        console.error(err);
        alert(err.response?.data || '申請失敗');
      }
    },
    async loadRmaList() {
      this.loadingRma = true;
      try {
      const res = await http.get(`/ord/OrdersApi/member/me/rma`);
        this.rmaList = res.data;
      } catch (err) {
        console.error(err);
        alert('載入退換貨申請失敗');
      } finally {
        this.loadingRma = false;
      }
    },
    formatDate(date) {
      return new Date(date).toLocaleDateString('zh-TW', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit'
      });
    },
    getRemainingDays(deliveredDate) {
      if (!deliveredDate) return 0;
      const delivered = new Date(deliveredDate);
      const now = new Date();
      const diffDays = Math.floor((now - delivered) / (1000 * 60 * 60 * 24));
      return Math.max(0, 7 - diffDays);
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
        picking: '撿貨中',
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
</style>