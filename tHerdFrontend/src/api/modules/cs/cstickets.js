// src/api/modules/cs/cstickets.js
import http from '@/api/http'

// 取得 FAQ 分類清單（工單下拉選單用）
export async function getCategories() {
  const res = await http.get('cs/CsTickets/categories')
  if (res.data?.success) return res.data.data
  return []
}

// 建立新工單
export async function createTicket(formData) {
  const res = await http.post('cs/CsTickets/create', formData,{
    headers: { 'Content-Type': 'multipart/form-data' } 
  })
  return res.data
}
// 取得「我的工單」清單
export async function getTickets() {
  const res = await http.get('/cs/CsTickets/list')
  if (res.data?.success) return res.data.data
  return []
}

