/**
 * sweetalert.js - SweetAlert2 封裝工具
 * 功能：提供統一的提示框功能
 */

import Swal from 'sweetalert2'

/**
 * 成功提示
 * @param {string} message - 提示訊息
 * @param {string} title - 標題（選填）
 * @returns {Promise}
 */
export const success = (message, title = '成功！') => {
  return Swal.fire({
    icon: 'success',
    title: title,
    text: message,
    confirmButtonText: '確定',
    confirmButtonColor: '#28a745',
  })
}

/**
 * 錯誤提示
 * @param {string} message - 錯誤訊息
 * @param {string} title - 標題（選填）
 * @returns {Promise}
 */
export const error = (message, title = '錯誤！') => {
  return Swal.fire({
    icon: 'error',
    title: title,
    text: message,
    confirmButtonText: '確定',
    confirmButtonColor: '#dc3545',
  })
}

/**
 * 警告提示
 * @param {string} message - 警告訊息
 * @param {string} title - 標題（選填）
 * @returns {Promise}
 */
export const warning = (message, title = '注意！') => {
  return Swal.fire({
    icon: 'warning',
    title: title,
    text: message,
    confirmButtonText: '確定',
    confirmButtonColor: '#ffc107',
  })
}

/**
 * 資訊提示
 * @param {string} message - 資訊訊息
 * @param {string} title - 標題（選填）
 * @returns {Promise}
 */
export const info = (message, title = '提示') => {
  return Swal.fire({
    icon: 'info',
    title: title,
    text: message,
    confirmButtonText: '確定',
    confirmButtonColor: '#17a2b8',
  })
}

/**
 * 確認對話框
 * @param {string} message - 確認訊息
 * @param {string} title - 標題（選填）
 * @returns {Promise<boolean>} 用戶是否確認
 */
export const confirm = (message, title = '確認操作') => {
  return Swal.fire({
    icon: 'question',
    title: title,
    text: message,
    showCancelButton: true,
    confirmButtonText: '確定',
    cancelButtonText: '取消',
    confirmButtonColor: '#007bff',
    cancelButtonColor: '#6c757d',
    reverseButtons: true,
  }).then((result) => result.isConfirmed)
}

/**
 * Toast 輕量提示（右上角）
 * @param {string} message - 提示訊息
 * @param {string} icon - 圖標類型 (success/error/warning/info)
 * @param {number} timer - 顯示時間（毫秒，預設 3000）
 * @returns {Promise}
 */
export const toast = (message, icon = 'success', timer = 3000) => {
  const Toast = Swal.mixin({
    toast: true,
    position: 'bottom-end',
    showConfirmButton: false,
    timer: timer,
    timerProgressBar: true,
    didOpen: (toast) => {
      toast.addEventListener('mouseenter', Swal.stopTimer)
      toast.addEventListener('mouseleave', Swal.resumeTimer)
    },
  })

  return Toast.fire({
    icon: icon,
    title: message,
  })
}

/**
 * 載入中提示
 * @param {string} message - 載入訊息
 * @returns {void}
 */
export const loading = (message = '載入中...') => {
  Swal.fire({
    title: message,
    allowOutsideClick: false,
    allowEscapeKey: false,
    showConfirmButton: false,
    didOpen: () => {
      Swal.showLoading()
    },
  })
}

/**
 * 關閉提示框
 * @returns {void}
 */
export const close = () => {
  Swal.close()
}

/**
 * 簡單的提示（類似原生 alert）
 * @param {string} message - 提示訊息
 * @returns {Promise}
 */
export const alert = (message) => {
  return Swal.fire({
    text: message,
    confirmButtonText: '確定',
    confirmButtonColor: '#007bff',
  })
}

/**
 * 預設匯出所有方法
 */
export default {
  success,
  error,
  warning,
  info,
  confirm,
  toast,
  loading,
  close,
  alert,
}
