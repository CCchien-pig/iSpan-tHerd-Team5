(function (global) {
    /**
     * 將 ISO 字串轉成 yyyy/MM/dd HH:mm:ss 格式
     * @param {string} isoString - 例如 "2025-09-10T18:41:31.8947362"
     * @param {number} addDays - 可選，額外增加的天數
     * @returns {string} 格式化後的字串
     */
    function formatDateTime(isoString, addDays = 0) {
        if (!isoString) return "";

        const dt = new Date(isoString);
        if (isNaN(dt)) return ""; // 無效日期

        dt.setDate(dt.getDate() + addDays);

        const pad = n => String(n).padStart(2, "0");

        const yyyy = dt.getFullYear();
        const MM = pad(dt.getMonth() + 1);
        const dd = pad(dt.getDate());
        const HH = pad(dt.getHours());
        const mm = pad(dt.getMinutes());
        const ss = pad(dt.getSeconds());

        return `${yyyy}/${MM}/${dd} ${HH}:${mm}:${ss}`;
    }

    // 掛到全域，方便呼叫
    global.formatDateTime = formatDateTime;

})(window);