// src/pages/modules/cs/api/csfaq.js
import http from "./http";
export async function getFaqList() {
  const res = await http.get("/api/cs/Faqs/list");
  // 後端回傳統一是 ApiResponse<T>，要讀 res.data.data
  if (res.data?.success) return res.data.data;
  throw new Error(res.data?.message || "取得 FAQ 失敗");
}

export async function getFaqDetail(id) {
  const res = await http.get(`/api/cs/faq/${id}`);
  if (res.data?.success) return res.data.data;
  throw new Error(res.data?.message || "取得 FAQ 詳細失敗");
}
export async function searchFaq(q) {
  const res = await http.get('/api/cs/Faqs/search', { params: { q } });
  if (res.data?.success) return res.data.data;
  throw new Error(res.data?.message || '搜尋失敗');}

export async function createFeedback(payload) {
  // 受保護的路徑，攔截器會自動帶 JWT
  const res = await http.post("/api/cs/Faqs/feedback", payload);
  if (res.data?.success) return res.data.data;
  throw new Error(res.data?.message || "送出意見回饋失敗");
}
