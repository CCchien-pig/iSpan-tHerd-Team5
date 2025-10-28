// src/pages/modules/cs/api/tickets.js
import { http } from '@/api/http'

/**
 * 建立客服工單
 * @param {object} payload - 工單資料
 */
export async function createTicket(payload) {
  const res = await http.post('/api/modules/cs/tickets', payload)
  if (res.data?.success) return res.data.data // { ticketId, ticketNo }
  throw new Error(res.data?.message || '建立工單失敗')
}
