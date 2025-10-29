// src/modules/cs/api/csfaq.js
import { http } from "@/api/http";
/** 取得 FAQ 分類＋問題清單（啟用） */
export async function getFaqList() {
  const res = await http.get('/cs/Faqs/list')
  if (res.data?.success) return res.data.data
  return []
}

/** 關鍵字搜尋（用於正式搜尋與先做的建議） */
export async function searchFaq(q) {
  const res = await http.get('/cs/Faqs/search', { params: { q } })
  if (res.data?.success) return res.data.data
  return []
}

/**（第 2 階段用）建議 API；先留著，後端做完再切換 */
export async function suggestFaq(q, limit = 6) {
  const res = await http.get('/cs/Faqs/suggest', { params: { q, limit } })
  if (res.data?.success) return res.data.data
  return []
}

export async function getFaqDetail(id) {
  const res = await http.get(`/cs/Faqs/${id}`)
  if (res.data?.success) return res.data.data   //  直接回傳 data
  throw new Error(res.data?.message || '取得 FAQ 詳細失敗')
}



export async function createFeedback(payload) {
  // 受保護的路徑，攔截器會自動帶 JWT
  const res = await http.post("/cs/Faqs/feedback", payload);
  if (res.data?.success) return res.data.data;
  throw new Error(res.data?.message || "送出意見回饋失敗");
}
