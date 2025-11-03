/**
 * supSlugify - 產生品牌詳頁的 SEO 友善路由
 *
 * - 讓網址包含品牌名稱（/brands/allmax-1001）
 *
 * 用法：
 *   import { toBrandSlug } from '@/utils/supSlugify'
 *   const url = toBrandSlug(brandName, brandId) // -> /brands/{slug}-{id}
 *
 * slug 規則（可依需求調整）：
 * - 去頭尾空白
 * - 全小寫
 * - 空白轉連字號（-）
 * - & 轉 and
 * - 去掉非 a-z / 0-9 / - 的符號
 * - 連續 - 合併為單一 -
 */
export const toBrandSlug = (name, id) => {
  const slug = String(name || '')
    .trim()
    .toLowerCase()
    .replace(/\s+/g, '-') // 空白 -> -
    .replace(/&/g, 'and') // & -> and
    .replace(/[^a-z0-9-]/g, '') // 去掉其它符號
    .replace(/-+/g, '-') // 連續 - 合併
  return `/brands/${slug}-${id}`
}
