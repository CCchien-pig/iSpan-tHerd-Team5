// src/utils/imageHelper.js
export const API_BASE = "https://localhost:7103";

// 判斷是否為 placeholder（不當作有效封面）
export function isPlaceholder(path = "") {
    const p = String(path).toLowerCase();
    return !p ||
        p.includes("placeholder") ||
        p.includes("cover00");
}

// 將相對路徑轉為完整 URL（含 ../../../uploads、/uploads、uploads）
export function absoluteImageUrl(path) {
    if (!path) return null;

    // 已是完整網址 → 直接用
    if (/^https?:\/\//i.test(path)) return path;

    // 富文本/區塊常見：../../../uploads/images/xxx.jpg
    if (path.includes("uploads")) {
        // 去掉開頭的 ../ 或 /，再補 API_BASE
        const cleaned = path.replace(/^\.*\/*/, "");
        return `${API_BASE}/${cleaned}`;
    }

    // 其他相對路徑（通常不會用在上傳圖）→ 原樣返回
    return path;
}

// 專給封面圖：有真封面就轉完整 URL；沒有就回 null 讓外層套插畫
export function coverUrlOrNull(coverImage) {
    if (isPlaceholder(coverImage)) return null;
    const url = absoluteImageUrl(coverImage);
    return url || null;
}

// 修復富文本 HTML 內的 <img src="..."> 路徑（v-html 之前先走這個）
export function safeHtml(html = "") {
    if (!html) return "";

    // 用正則抓出所有 <img src="...">，逐一修正
    return html.replace(/(<img\b[^>]*?\bsrc=)(['"])([^'"]+)\2/gi, (m, prefix, quote, src) => {
        // 跳過已是 http/https
        if (/^https?:\/\//i.test(src)) return m;

        // uploads 系列 → 補 API_BASE
        if (src.includes("uploads")) {
            const fixed = absoluteImageUrl(src);
            return `${prefix}${quote}${fixed}${quote}`;
        }

        // 其他保持原樣
        return m;
    });
}

// 統一的 <img> onerror fallback
export function onImgError(e) {
    e.target.src = "/images/cover01.png"; // 你的品牌插畫任一張皆可
}
