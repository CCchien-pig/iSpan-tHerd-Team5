// src/utils/imageColors.js

// 針對瀏覽器端使用正確入口與具名匯入
import { Vibrant } from 'node-vibrant/browser'

export const DEFAULT_BG = { r: 0, g: 147, b: 171 }
export const DEFAULT_HOVER_BG = { r: 77, g: 180, b: 193 }

export const toRgb = ({ r, g, b }) => `rgb(${r}, ${g}, ${b})`

// 計算相對亮度，決定字色（白/深色）
export const getLuma = ({ r, g, b }) => 0.2126 * r + 0.7152 * g + 0.0722 * b
export const pickTextColor = (bg, light = 'rgb(248,249,250)', dark = '#0b3a3f') =>
  getLuma(bg) > 180 ? dark : light

// 取較接近「最大面積」色：從全部 swatches 依 population 排序挑第一名，過亮/過暗則往下找
export async function extractDominantByPopulation(imgUrl, fallback = DEFAULT_BG) {
  try {
    if (!imgUrl) return fallback
    const palette = await Vibrant.from(imgUrl).getPalette()
    const swatches = Object.values(palette || {}).filter(Boolean)
    if (!swatches.length) return fallback
    // 依 population 由大到小
    swatches.sort((a, b) => (b.population || 0) - (a.population || 0))
    // 過亮/過暗篩掉，選中間的
    const isBad = (rgb) => {
      const [r, g, b] = rgb
      const luma = getLuma({ r, g, b })
      return luma < 40 || luma > 235
    }
    let pick = swatches.find((s) => s.rgb && !isBad(s.rgb)) || swatches[0]
    const [r, g, b] = pick.rgb
    return { r: Math.round(r), g: Math.round(g), b: Math.round(b) }
  } catch {
    return fallback
  }
}

// hover：反轉為白底＋主色字
export function makeHoverInvert(base = DEFAULT_BG) {
  return {
    hoverBg: 'rgb(248,249,250)',
    hoverText: toRgb(base),
  }
}
