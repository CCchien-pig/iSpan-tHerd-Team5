// === ✅ 全域 Loading 函式（放最上面）===
window.showLoading = function (message = "處理中，請稍候...", mode = "global") {
    const loader = document.getElementById("globalLoading");
    if (!loader) return;

    const text = loader.querySelector("div.mt-3");
    if (text) text.textContent = message;

    loader.style.display = "flex";
    loader.style.opacity = "1";
    loader.style.pointerEvents = "auto";
    loader.dataset.mode = mode;

    if (mode === "inline") {
        loader.style.background = "rgba(255,255,255,0.6)";
    } else {
        loader.style.background = "rgba(0,0,0,0.5)";
    }
};

window.hideLoading = function () {
    const loader = document.getElementById("globalLoading");
    if (!loader) return;
    loader.style.transition = "opacity .3s ease";
    loader.style.opacity = "0";
    setTimeout(() => {
        loader.style.display = "none";
        loader.style.pointerEvents = "none";
    }, 300);
};

// === ✅ 把這段放在最上面 ===
window.refreshFileList = async function (message = "正在重新載入資料...") {
    try {
        showLoading(message, "inline");

        const currentProgId = document.querySelector("#ProgId")?.value || "UploadTest";
        const currentFolderId = document.querySelector("#CurrentFolderId")?.value || "";

        const query = new URLSearchParams({
            moduleId: "SYS",
            progId: currentProgId
        });
        if (currentFolderId) query.append("folderId", currentFolderId);

        const res = await fetch(`/SYS/UploadTest/GetFilesByProg?${query.toString()}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);

        const html = await res.text();

        const listContainer = document.querySelector("#fileListContainer");
        if (listContainer) {
            listContainer.innerHTML = html.trim()
                ? html
                : `<p class="text-muted">目前沒有圖片。</p>`;
        }
    } catch (err) {
        console.error("❌ 重新抓取檔案清單失敗：", err);
        Swal.fire("錯誤", "無法重新載入最新資料", "error");
    } finally {
        hideLoading();
    }
};

document.addEventListener("DOMContentLoaded", async function () {
    // 初次載入：顯示全頁遮罩
    showLoading("載入中，請稍候...", "global");

    try {
        await window.refreshFileList();
    } catch (err) {
        console.error("初次載入失敗：", err);
    } finally {
        hideLoading(); // 保證無論成功與否都會關閉
    }

    // === DOM 元素 ===
    const resetBtn = document.getElementById("resetBtn");
    const selectBtn = document.getElementById("selectBtn");
    const fileInput = document.getElementById("fileInput");
    const preview = document.getElementById("preview");
    const previewArea = document.getElementById("previewArea");
    const dropArea = document.getElementById("dropArea");
    const hiddenInputs = document.getElementById("hiddenInputs");

    // === 增加欄位綁定機制 ===
    const fieldMap = {
        fileId: "#modalFileId",
        fileKey: "#modalFileKey",
        fileUrl: "#modalFileUrl",
        fileExt: "#modalFileExt",
        mimeType: "#modalMimeType",
        width: "#modalWidth",
        height: "#modalHeight",
        fileSizeBytes: "#modalFileSizeBytes",
        altText: "#modalAlt",
        caption: "#modalCaption",
        isActive: "#modalIsActive",
        folderId: "#modalFolderId",
        createdDate: "#modalCreatedDate"
    };

    // === 工具函式 ===
    function showReset() {
        if (resetBtn) resetBtn.classList.remove("d-none");
    }

    function togglePreviewArea(show) {
        if (previewArea) {
            previewArea.classList.toggle("d-none", !show);
        }
    }

    // === 啟用／停用圖片 ===
    window.toggleActive = async function (btn) {
        const item = btn.closest(".img-item");
        const img = item.querySelector("img");
        const fileId = img.dataset.fileId;

        const isCurrentlyActive = img.dataset.isActive === "true";
        const newState = !isCurrentlyActive;

        const confirm = await Swal.fire({
            title: newState ? "要啟用這張圖片嗎？" : "要停用這張圖片嗎？",
            text: newState ? "啟用後，此圖片會重新顯示。" : "停用後，前台將不再顯示此圖片。",
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "確定",
            cancelButtonText: "取消"
        });

        if (!confirm.isConfirmed) return;

        try {
            const res = await fetch("/SYS/Images/UpdateFile", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    FileId: fileId,
                    IsActive: newState
                })
            });

            const data = await res.json();

            if (data.success) {
                img.dataset.isActive = newState.toString();
                item.classList.toggle("inactive", !newState);
                btn.textContent = newState ? "停用" : "啟用";

                Swal.fire({
                    title: newState ? "已啟用" : "已停用",
                    icon: "success",
                    showConfirmButton: false,
                    timer: 1000
                });
            } else {
                Swal.fire("❌ 更新失敗", data.message || "", "error");
            }
        } catch (err) {
            Swal.fire("❌ 錯誤", "伺服器連線失敗", "error");
            console.error("toggleActive 錯誤：", err);
        }
    };

    // === 圖片預覽 ===
    function showPreview(files) {
        const currentCount = preview.children.length;
        const startIndex = currentCount;

        [...files].forEach((file, i) => {
            const index = startIndex + i;
            const reader = new FileReader();
            const wrapper = document.createElement("div");
            wrapper.className = "img-item border rounded p-3 mb-3 bg-white shadow-sm";

            const mime = file.type;

            // === 圖片 ===
            if (mime.startsWith("image/")) {
                reader.onload = e => {
                    wrapper.innerHTML = `
                <div class="d-flex align-items-start gap-3">
                    <div class="flex-shrink-0" style="width:120px;height:120px;overflow:hidden;border-radius:6px;">
                        <img src="${e.target.result}" class="preview-img" 
                             style="width:100%;height:100%;object-fit:cover;border-radius:6px;">
                    </div>
                    <div class="flex-grow-1">
                        <p class="mb-1 text-muted small">${file.name}</p>
                        <input type="text" class="form-control form-control-sm mb-2 alt-input"
                            placeholder="AltText (可選)" name="Meta[${index}].AltText">
                        <textarea class="form-control form-control-sm caption-input"
                            rows="2" placeholder="Caption (可選)" name="Meta[${index}].Caption"></textarea>
                    </div>
                    <button type="button" class="btn-close-custom position-absolute top-0 end-0 mt-2 me-2" aria-label="Close">
                        <i class="bi bi-x-lg text-muted"></i>
                    </button>
                </div>`;
                    bindRemove(wrapper);
                };
                reader.readAsDataURL(file);
            }

            // === 影片 ===
            else if (mime.startsWith("video/")) {
                wrapper.innerHTML = `
            <div class="d-flex align-items-start gap-3">
                <div class="flex-shrink-0 d-flex align-items-center justify-content-center bg-light"
                     style="width:120px;height:120px;border-radius:6px;">
                    <i class="bi bi-file-earmark-play fs-1 text-info"></i>
                </div>
                <div class="flex-grow-1">
                    <p class="mb-1 text-muted small">${file.name}</p>
                    <input type="text" class="form-control form-control-sm mb-2 alt-input"
                        placeholder="影片描述 (可選)" name="Meta[${index}].AltText">
                    <textarea class="form-control form-control-sm caption-input"
                        rows="2" placeholder="影片說明 (可選)" name="Meta[${index}].Caption"></textarea>
                </div>
                <button type="button" class="btn-close-custom position-absolute top-0 end-0 mt-2 me-2" aria-label="Close">
                    <i class="bi bi-x-lg text-muted"></i>
                </button>
            </div>`;
                bindRemove(wrapper);
            }

            // === PDF ===
            else if (mime === "application/pdf") {
                wrapper.innerHTML = `
            <div class="d-flex align-items-start gap-3">
                <div class="flex-shrink-0 d-flex align-items-center justify-content-center bg-light"
                     style="width:120px;height:120px;border-radius:6px;">
                    <i class="bi bi-file-earmark-pdf fs-1 text-danger"></i>
                </div>
                <div class="flex-grow-1">
                    <p class="mb-1 text-muted small">${file.name}</p>
                    <input type="text" class="form-control form-control-sm mb-2 alt-input"
                        placeholder="文件標題 (可選)" name="Meta[${index}].AltText">
                    <textarea class="form-control form-control-sm caption-input"
                        rows="2" placeholder="文件說明 (可選)" name="Meta[${index}].Caption"></textarea>
                </div>
                <button type="button" class="btn-close-custom position-absolute top-0 end-0 mt-2 me-2" aria-label="Close">
                    <i class="bi bi-x-lg text-muted"></i>
                </button>
            </div>`;
                bindRemove(wrapper);
            }

            // === 其他檔案 ===
            else {
                wrapper.innerHTML = `
            <div class="d-flex align-items-start gap-3">
                <div class="flex-shrink-0 d-flex align-items-center justify-content-center bg-light"
                     style="width:120px;height:120px;border-radius:6px;">
                    <i class="bi bi-file-earmark fs-1 text-secondary"></i>
                </div>
                <div class="flex-grow-1">
                    <p class="mb-1 text-muted small">不支援預覽：${file.name}</p>
                </div>
                <button type="button" class="btn-close-custom position-absolute top-0 end-0 mt-2 me-2" aria-label="Close">
                    <i class="bi bi-x-lg text-muted"></i>
                </button>
            </div>`;
                bindRemove(wrapper);
            }

            preview.appendChild(wrapper);

            // 隱藏 input 存檔案
            const dataTransfer = new DataTransfer();
            dataTransfer.items.add(file);
            const fileInputHidden = document.createElement("input");
            fileInputHidden.type = "file";
            fileInputHidden.name = `Meta[${index}].File`;
            fileInputHidden.files = dataTransfer.files;
            fileInputHidden.hidden = true;
            hiddenInputs.appendChild(fileInputHidden);
        });

        if (files.length > 0) {
            togglePreviewArea(true);
            showReset();
        }

        // 綁定刪除事件
        function bindRemove(wrapper) {
            const closeBtn = wrapper.querySelector(".btn-close-custom");
            if (!closeBtn) return;
            closeBtn.addEventListener("click", () => {
                wrapper.remove();
                if (preview.children.length === 0) togglePreviewArea(false);
            });
        }
    }

    // === 拖拉上傳區 ===
    if (dropArea) {
        ["dragenter", "dragover", "dragleave", "drop"].forEach(evtName => {
            dropArea.addEventListener(evtName, e => {
                e.preventDefault();
                e.stopPropagation();
            });
        });

        ["dragenter", "dragover"].forEach(evtName => {
            dropArea.addEventListener(evtName, () => dropArea.classList.add("bg-info", "bg-opacity-25"));
        });
        ["dragleave", "drop"].forEach(evtName => {
            dropArea.addEventListener(evtName, () => dropArea.classList.remove("bg-info", "bg-opacity-25"));
        });

        dropArea.addEventListener("drop", e => {
            const files = e.dataTransfer.files;
            if (files.length > 0) showPreview(files);
        });
    }

    // === 等待 Modal 元素確實載入（最多等 1 秒）===
    async function waitForElement(selector, timeout = 1000) {
        const start = Date.now();
        while (Date.now() - start < timeout) {
            const el = document.querySelector(selector);
            if (el) return el;
            await new Promise(r => setTimeout(r, 50));
        }
        console.warn(`⚠️ 元素超時未載入: ${selector}`);
        return null;
    }

    // === 共用函式：開啟並填入圖片資訊到 Modal ===
    // 支援 dataset (HTML data-*) 或 DTO (JSON 物件)
    async function openImageModal(fileData, modalSelector = "#imgMetaModal") {
        // 🧩 確保全域 loading 被關掉（防止黑屏）
        window.hideLoading?.();

        const modal = await waitForElement(modalSelector);
        if (!modal) {
            console.error("❌ 找不到指定的 Modal：", modalSelector);
            return;
        }

        const modalImg = modal.querySelector(".img-zoomable");
        const bsModal = bootstrap.Modal.getOrCreateInstance(modal);

        // === 同步所有欄位（含 checkbox） ===
        for (const [key, selector] of Object.entries(fieldMap)) {
            const input = modal.querySelector(selector);
            if (!input) continue;

            // 支援 dataset 小寫 / 駝峰 / Pascal 三種型態
            let val =
                fileData[key] ??
                fileData[key.toLowerCase()] ??
                fileData[key.charAt(0).toLowerCase() + key.slice(1)];

            // ✅ 是否啟用
            if (key === "isActive") {
                input.checked = val === true || val === "true";
                continue;
            }

            // ✅ 檔案大小自動轉換
            if (key === "fileSizeBytes") {
                const bytes = parseInt(val || "0", 10);
                let formatted;
                if (isNaN(bytes)) formatted = "--";
                else if (bytes < 1024) formatted = `${bytes} Bytes`;
                else if (bytes < 1024 * 1024) formatted = `${(bytes / 1024).toFixed(1)} KB`;
                else formatted = `${(bytes / 1024 / 1024).toFixed(2)} MB`;
                input.value = formatted;
                continue;
            }

            if (input.tagName === "INPUT" || input.tagName === "TEXTAREA") {
                input.value = val ?? "";
            }
        }

        // === 同步圖片 / 影片預覽 ===
        const mimeType = fileData.mimeType || fileData.MimeType || "";
        const previewContainer = modal.querySelector(".preview-container");
        if (previewContainer) {
            previewContainer.innerHTML = "";
            let previewEl;

            if (mimeType.startsWith("video/")) {
                const videoUrl = fileData.fileUrl || fileData.FileUrl || "";

                // 包成 <a> 避免 Chrome 黑頻
                const link = document.createElement("a");
                link.href = videoUrl;
                link.target = "_blank";
                link.rel = "noopener noreferrer";
                link.title = "點擊開啟完整影片（另開新分頁）";

                const video = document.createElement("video");
                video.src = videoUrl;
                video.controls = true;
                video.muted = true;
                video.preload = "metadata";
                video.className = "dynamic-preview rounded shadow-sm";
                video.style = `
                    max-width: 600px;
                    max-height: 400px;
                    object-fit: contain;
                    cursor: pointer;
                    border-radius: 8px;
                    border: 1px solid #ccc;
                `;

                video.dataset.fileId = fileData.fileId || fileData.FileId || "";
                video.dataset.updateApi = "/SYS/Images/UpdateFile";

                link.appendChild(video);
                previewEl = link;
            } else if (mimeType.startsWith("image/")) {
                const imgUrl = fileData.fileUrl || fileData.FileUrl || "/images/No-Image.svg";

                // 用 <a> 包一層取代 window.open()
                const link = document.createElement("a");
                link.href = imgUrl;
                link.target = "_blank";
                link.rel = "noopener noreferrer";
                link.title = "點擊開啟完整圖片（另開新分頁）";

                const img = document.createElement("img");
                img.src = imgUrl;
                img.className = "dynamic-preview rounded shadow-sm img-zoomable";
                img.dataset.fileId = fileData.fileId || fileData.FileId || "";
                img.dataset.updateApi = "/SYS/Images/UpdateFile";  // 預設 API
                img.dataset.isActive = fileData.isActive === true || fileData.isActive === "true" ? "true" : "false";
                img.dataset.isExternal = fileData.isExternal === true || fileData.isExternal === "true" ? "true" : "false";
                img.style = `
                    max-width: 600px;
                    max-height: 400px;
                    object-fit: contain;
                    cursor: zoom-in;
                    border: 1px solid #ccc;
                    border-radius: 8px;
                `;

                link.appendChild(img);
                previewEl = link;
            } else {
                previewEl = document.createElement("div");
                previewEl.className = "text-muted small mt-3";
                previewEl.textContent = `無法預覽 (${mimeType || "未知"})`;
            }

            previewContainer.appendChild(previewEl);
        }

        // === ✅ 同步外部連結 Badge ===
        const badge = modal.querySelector("#modalIsExternalBadge");
        if (badge) {
            const isExternal =
                fileData.isExternal === true ||
                fileData.IsExternal === true ||
                fileData.isExternal === "true" ||
                fileData.IsExternal === "true" ||
                (fileData.fileUrl || fileData.FileUrl || "").startsWith("http");

            badge.textContent = isExternal ? "外部連結" : "自有檔案";
            badge.classList.remove("bg-success", "bg-secondary");
            badge.classList.add(isExternal ? "bg-success" : "bg-secondary");
        }

        // === ✅ 更新圖片 dataset ===
        if (modalImg) {
            modalImg.dataset.fileId = fileData.fileId || fileData.FileId || "";
            modalImg.dataset.isActive =
                fileData.isActive === true || fileData.isActive === "true" ? "true" : "false";
            modalImg.dataset.isExternal =
                fileData.isExternal === true || fileData.isExternal === "true" ? "true" : "false";
        }

        if (window.table) {
            table.ajax.reload(null, false); // false = 保留當前頁碼
        }

        bsModal.show();
    }

    // 在 modal 關閉前觸發事件，例如 UpdateImageModal.js 裡
    document.addEventListener("imageUpdated", (e) => {
        const updated = e.detail; // e.g. { fileId: 123, altText: "...", caption: "...", isActive: true }
        const row = table.rows().data().toArray().find(r =>
            (r.fileId || r.FileId) === updated.fileId
        );

        if (row) {
            Object.assign(row, updated);
            table.row((idx, data) => (data.fileId || data.FileId) === updated.fileId)
                .data(row)
                .invalidate()
                .draw(false);
        }
    });

    // === Modal（支援多個 UpdateImage 元件） ===
    document.querySelectorAll(".modal").forEach(modalElement => {
        const modalImg = modalElement.querySelector(".img-zoomable");
        if (!modalImg) return;

        const modalAlt = modalElement.querySelector("#modalAlt");
        const modalCaption = modalElement.querySelector("#modalCaption");
        const modalIsActive = modalElement.querySelector("#modalIsActive");
        const confirmBtn = modalElement.querySelector("#confirmMetaBtn");

        // === 點擊 Modal 內圖片 → 開原圖 ===
        modalImg.addEventListener("click", e => {
            e.preventDefault();
            e.stopPropagation();
            if (modalImg.src) window.open(modalImg.src, "_blank");
        });

        // === 點擊縮圖 → 開啟對應 Modal ===
        document.querySelectorAll(`.thumb-clickable[data-bs-target="#${modalElement.id}"]`).forEach(img => {
            img.addEventListener("click", () => {
                openImageModal(img.dataset, `#${modalElement.id}`);
            });
        });

        // === 儲存（更新圖片 Meta）===
        if (confirmBtn) {
            confirmBtn.addEventListener("click", async () => {
                const modalImg = modalElement.querySelector(".img-zoomable");
                if (!modalImg) {
                    Swal.fire({ icon: "error", title: "找不到圖片預覽", text: "請重新開啟圖片視窗" });
                    return;
                }

                const fileId = modalImg.dataset.fileId;
                const data = {
                    FileId: fileId,
                    AltText: modalElement.querySelector("#modalAlt").value,
                    Caption: modalElement.querySelector("#modalCaption").value,
                    IsActive: modalElement.querySelector("#modalIsActive").checked,
                    Width: modalElement.querySelector("#modalWidth").value,
                    Height: modalElement.querySelector("#modalHeight").value
                };

                showLoading("正在儲存中...");

                try {
                    const res = await fetch("/SYS/Images/UpdateFile", {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify(data)
                    });

                    const result = await res.json();

                    if (result.success) {
                        Swal.fire({
                            icon: "success",
                            title: "更新成功",
                            timer: 1200,
                            showConfirmButton: false
                        });

                        bootstrap.Modal.getInstance(modalElement)?.hide();

                        // ✅ 更新前端圖片（局部）
                        if (data) {
                            updateBoundImage(data);
                            syncImageInput(data);
                        }
                    } else {
                        Swal.fire({ icon: "error", title: "更新失敗", text: result.message });
                    }
                } catch (err) {
                    Swal.fire({ icon: "error", title: "錯誤", text: err.message });
                } finally {
                    hideLoading();
                }
            });
        }

        // === 更新畫面卡片 + DTO ===
        function updateBoundImage(fileDto) {
            if (!fileDto || !fileDto.fileId) return;

            const card = document.querySelector(`.prod-img-item [data-file-id='${fileDto.fileId}']`);
            if (card) {
                card.src = fileDto.fileUrl;
                card.dataset.altText = fileDto.altText;
                card.dataset.caption = fileDto.caption;
                card.dataset.width = fileDto.width;
                card.dataset.height = fileDto.height;
                card.dataset.isActive = fileDto.isActive;

                const captionEl = card.closest(".prod-img-item").querySelector(".caption-text");
                if (captionEl) captionEl.textContent = fileDto.caption || "";

                const badge = card.closest(".prod-img-item").querySelector(".status-badge");
                if (badge) {
                    badge.textContent = fileDto.isActive ? "啟用" : "停用";
                    badge.classList.toggle("bg-success", fileDto.isActive);
                    badge.classList.toggle("bg-secondary", !fileDto.isActive);
                }

                // 視覺提示
                card.closest(".prod-img-item").style.boxShadow = "0 0 0 3px #28a74580";
                setTimeout(() => (card.closest(".prod-img-item").style.boxShadow = ""), 600);
            }

            // 同步更新前端 DTO
            if (window.productDto && Array.isArray(productDto.Images)) {
                const target = productDto.Images.find(x => x.imgId == fileDto.fileId);
                if (target) {
                    target.fileUrl = fileDto.fileUrl;
                    target.altText = fileDto.altText;
                    target.caption = fileDto.caption;
                    target.width = fileDto.width;
                    target.height = fileDto.height;
                    target.isActive = fileDto.isActive;
                }
            }

            console.log("✅ 已同步更新圖片資料", fileDto);
        }

        // === 更新 Razor 隱藏輸入欄位（for MVC ModelBinding） ===
        function syncImageInput(fileDto) {
            const idxInput = document.querySelector(`[name^="Images"][value="${fileDto.fileId}"]`);
            if (!idxInput) return;

            const parentIndex = idxInput.name.match(/\[(\d+)\]/)?.[1];
            if (parentIndex) {
                document.querySelector(`[name="Images[${parentIndex}].AltText"]`).value = fileDto.altText;
                document.querySelector(`[name="Images[${parentIndex}].Caption"]`).value = fileDto.caption;
                document.querySelector(`[name="Images[${parentIndex}].IsActive"]`).checked = fileDto.isActive;
            }
        }
    });

    // === 更新畫面資料 ===
    function updateImageState(fileId, altText, caption, isActive) {
        const img = document.querySelector(`.thumb-clickable[data-file-id="${fileId}"]`);
        if (!img) return;

        const modal = document.querySelector("#imgMetaModal");

        // === 從 Modal 取出最新值 ===
        const width = modal?.querySelector("#modalWidth")?.value;
        const height = modal?.querySelector("#modalHeight")?.value;
        const mimeType = modal?.querySelector("#modalMimeType")?.value;
        const fileSizeBytes = modal?.querySelector("#modalFileSizeBytes")?.value;
        const createdDate = modal?.querySelector("#modalCreatedDate")?.value;

        // === 更新 dataset ===
        img.alt = altText;
        img.dataset.altText = altText;
        img.dataset.caption = caption;
        img.dataset.isActive = isActive.toString();
        img.dataset.width = width;
        img.dataset.height = height;
        img.dataset.mimeType = mimeType;
        img.dataset.fileSizeBytes = fileSizeBytes;
        img.dataset.createdDate = createdDate;

        // === 更新 UI 狀態 ===
        const parent = img.closest(".img-item");
        parent.classList.toggle("inactive", !isActive);

        const toggleBtn = parent.querySelector(".toggle-active-btn");
        if (toggleBtn) toggleBtn.textContent = isActive ? "停用" : "啟用";
    }

    // === 修正版本：正確更新 Modal 內容 ===
    document.addEventListener("shown.bs.modal", function (event) {
        const modal = event.target;
        if (!modal.id.startsWith("imgMetaModal")) return;

        const triggerImg = event.relatedTarget;
        if (!triggerImg) return;

        const fileData = triggerImg.dataset; // ✅ 關鍵：從 dataset 抓資料
        const mimeType = fileData.mimeType || "";
        const previewContainer = modal.querySelector(".preview-container");
        if (!previewContainer) return;

        // === 🔹 清空舊內容（避免殘影或多重預覽） ===
        previewContainer.innerHTML = "";

        let previewEl;
        if (mimeType.startsWith("video/")) {
            const videoUrl = fileData.fileUrl || triggerImg.src || "";

            const link = document.createElement("a");
            link.href = videoUrl;
            link.target = "_blank";
            link.rel = "noopener noreferrer";
            link.title = "點擊開啟完整影片";

            const video = document.createElement("video");
            video.src = videoUrl;
            video.controls = true;
            video.muted = true;
            video.preload = "metadata";
            video.playsInline = true;
            video.className = "dynamic-preview mb-3 rounded shadow-sm";
            video.style = `
                max-width: 600px;
                max-height: 400px;
                object-fit: contain;
                cursor: pointer;
                border-radius: 8px;
                border: 1px solid #ccc;
            `;

            video.dataset.fileId = fileData.fileId || fileData.FileId || "";
            video.dataset.updateApi = "/SYS/Images/UpdateFile";

            link.appendChild(video);
            previewEl = link;
        } else if (mimeType.startsWith("image/")) {
            const imgUrl = fileData.fileUrl || fileData.FileUrl || "/images/No-Image.svg";

            // 用 <a> 包一層取代 window.open()
            const link = document.createElement("a");
            link.href = imgUrl;
            link.target = "_blank";
            link.rel = "noopener noreferrer";
            link.title = "點擊開啟完整圖片（另開新分頁）";

            const img = document.createElement("img");
            img.src = imgUrl;
            img.className = "dynamic-preview rounded shadow-sm img-zoomable";
            img.dataset.fileId = fileData.fileId || fileData.FileId || "";
            img.dataset.updateApi = "/SYS/Images/UpdateFile";  // 預設 API
            img.dataset.isActive = fileData.isActive === true || fileData.isActive === "true" ? "true" : "false";
            img.dataset.isExternal = fileData.isExternal === true || fileData.isExternal === "true" ? "true" : "false";
            img.style = `
                max-width: 600px;
                max-height: 400px;
                object-fit: contain;
                cursor: zoom-in;
                border: 1px solid #ccc;
                border-radius: 8px;
            `;

            link.appendChild(img);
            previewEl = link;
        } else {
            // 📄 其他類型（不支援預覽）
            previewEl = document.createElement("div");
            previewEl.className = "dynamic-preview text-muted small mt-3";
            previewEl.innerText = `無法預覽此類型 (${mimeType || "未知"})`;
        }

        previewContainer.appendChild(previewEl);

        // === 🔹 更新欄位資料 ===
        for (const [key, selector] of Object.entries(fieldMap)) {
            const input = modal.querySelector(selector);
            if (!input) continue;

            let val =
                fileData[key] ??
                fileData[key.toLowerCase()] ??
                fileData[key.charAt(0).toLowerCase() + key.slice(1)];

            // 是否啟用（checkbox）
            if (key === "isActive") {
                input.checked = val === "true";
                continue;
            }

            // 檔案大小（Bytes → KB/MB 格式化）
            if (key === "fileSizeBytes") {
                const bytes = parseInt(val || "0", 10);
                let formatted;
                if (isNaN(bytes)) formatted = "--";
                else if (bytes < 1024) formatted = `${bytes} Bytes`;
                else if (bytes < 1024 * 1024) formatted = `${(bytes / 1024).toFixed(1)} KB`;
                else formatted = `${(bytes / 1024 / 1024).toFixed(2)} MB`;
                input.value = formatted;
                continue;
            }

            // 其他一般欄位
            if (input.tagName === "INPUT" || input.tagName === "TEXTAREA") {
                input.value = val ?? "";
            }
        }

        // === ✅ 是否啟用開關 ===
        const activeSwitch = modal.querySelector("#modalIsActive");
        if (activeSwitch && fileData.isActive !== undefined) {
            activeSwitch.checked = fileData.isActive === "true";
        }

        // === ✅ 是否外部連結 Badge ===
        const badge = modal.querySelector("#modalIsExternalBadge");
        if (badge) {
            const isExternal = fileData.isExternal === "true";

            // 強制更新樣式與文字
            badge.textContent = isExternal ? "外部連結" : "自有檔案";
            badge.classList.remove("bg-success", "bg-secondary");
            badge.classList.add(isExternal ? "bg-success" : "bg-secondary");
        }

        // === ✅ 額外防呆（如果沒抓到 triggerImg） ===
        if (!fileData || !fileData.fileId) {
            console.warn("⚠️ 未取得 fileData，請檢查 data-* 屬性是否完整");
        }
    });

    // === 關閉時自動暫停影片並清除預覽 ===
    document.addEventListener("hidden.bs.modal", function (event) {
        if (!event.target.id.startsWith("imgMetaModal")) return;

        const previewContainer = event.target.querySelector(".preview-container");
        if (!previewContainer) return;

        const video = previewContainer.querySelector("video");
        if (video) video.pause();

        previewContainer.innerHTML = "";
    });

    document.addEventListener("hidden.bs.modal", () => {
        if (document.activeElement && document.activeElement.classList.contains("btn-close")) {
            document.activeElement.blur();
        }
        document.querySelectorAll("video").forEach(v => v.pause());
    });

    window.openImageModal = openImageModal;

    // === 刪除圖片 ===
    window.deleteFile = async function (fileId, btn) {
        // 從按鈕或圖片讀取 delete API（或預設）
        const deleteApi = btn?.dataset.deleteApi || "/SYS/Images/DeleteFile";

        const confirm = await Swal.fire({
            title: "確定刪除？",
            text: "此圖片將從雲端與資料庫永久移除",
            icon: "warning",
            showCancelButton: true,
            confirmButtonText: "刪除",
            cancelButtonText: "取消"
        });
        if (!confirm.isConfirmed) return;

        try {
            // 🌀 顯示 Loading
            showLoading("正在刪除檔案...");

            const res = await fetch(deleteApi, {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: new URLSearchParams({ fileId })
            });

            const data = await res.json();

            if (data.success) {
                Swal.fire({
                    title: "✅ 刪除成功",
                    icon: "success",
                    showConfirmButton: false,
                    timer: 1000
                });

                // 🔄 更新畫面（移除對應項）
                const targetImg = document.querySelector(`.thumb-clickable[data-file-id="${fileId}"]`);
                if (targetImg) {
                    const parent = targetImg.closest(".img-item");
                    if (parent) parent.remove();
                }

                const grid = document.querySelector("form > div.img-grid");
                if (grid && grid.children.length === 0) {
                    grid.innerHTML = `<p class="text-muted">目前沒有圖片。</p>`;
                }

                // ✅ 可選：同步刷新列表（若有多使用者同時上傳時建議保留）
                await refreshFileList();

            } else {
                Swal.fire("❌ 刪除失敗", data.message || "", "error");
            }
        } catch (err) {
            console.error("❌ 刪除錯誤：", err);
            Swal.fire("錯誤", err.message || "伺服器連線失敗", "error");
        } finally {
            // 🟢 確保不論成功或失敗都關閉 Loading
            hideLoading();
        }
    };
});

document.addEventListener("click", async (e) => {
    if (e.target && e.target.id === "confirmMetaBtn") {

        const modalElement = e.target.closest(".modal");
        const modalImg = modalElement.querySelector(".img-zoomable");
        if (!modalImg) {
            Swal.fire({ icon: "error", title: "找不到圖片預覽" });
            return;
        }

        const api = modalImg.dataset.updateApi || "/SYS/Images/UpdateFile";
        const fileId = modalImg.dataset.fileId;
        const alt = modalElement.querySelector("#modalAlt").value;
        const caption = modalElement.querySelector("#modalCaption").value;
        const isActive = modalElement.querySelector("#modalIsActive").checked;
        const width = modalElement.querySelector("#modalWidth").value;
        const height = modalElement.querySelector("#modalHeight").value;

        showLoading("正在儲存中...");
        try {
            const res = await fetch(api, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ FileId: fileId, AltText: alt, Caption: caption, IsActive: isActive, Width: width, Height: height })
            });
            const result = await res.json();
            if (result.success) {
                Swal.fire({ icon: "success", title: "更新成功", timer: 1200, showConfirmButton: false });
                bootstrap.Modal.getInstance(modalElement)?.hide();

                // 派發全域事件，讓 DataTable 那邊監聽刷新
                document.dispatchEvent(new CustomEvent("imageMetaUpdated"));

                // 在 UploadTest 畫面上，也同步刷新
                refreshFileList?.();
            } else {
                Swal.fire({ icon: "error", title: "更新失敗", text: result.message });
            }
        } catch (err) {
            Swal.fire({ icon: "error", title: "錯誤", text: err.message });
        } finally {
            hideLoading();
        }
    }
});

// === ✅ 針對圖片選擇器的事件統一註冊 ===
document.addEventListener("click", e => {
    const item = e.target.closest(".img-item");
    if (!item) return;

    // 若是直接點 checkbox，就不做多餘處理
    if (e.target.classList.contains("select-file")) return;

    const chk = item.querySelector(".select-file");
    if (chk) chk.click(); // 觸發 change 事件
});

// ✅ change 時切換高亮
document.addEventListener("change", e => {
    if (e.target.classList.contains("select-file")) {
        const item = e.target.closest(".img-item");
        if (item) {
            item.classList.toggle("selected", e.target.checked);
        }
    }
});
