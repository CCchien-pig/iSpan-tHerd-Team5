document.addEventListener("DOMContentLoaded", function () {
    // === DOM 元素 ===
    const resetBtn = document.getElementById("resetBtn");
    const selectBtn = document.getElementById("selectBtn");
    const fileInput = document.getElementById("fileInput");
    const preview = document.getElementById("preview");
    const previewArea = document.getElementById("previewArea");
    const dropArea = document.getElementById("dropArea");
    const hiddenInputs = document.getElementById("hiddenInputs");

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
    window.toggleActive = async function (fileId, btn) {
        const isCurrentlyActive = btn.textContent.trim() === "停用";
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
            // ✅ 從圖片屬性讀取 update API（或預設）
            const updateApiUrl = btn.dataset.updateApi || "/SYS/UploadTest/UpdateMeta";

            const res = await fetch(`${window.location.origin}${updateApiUrl}`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ FileId: fileId, IsActive: newState })
            });

            const data = await res.json();

            if (data.success) {
                const item = btn.closest(".img-item");
                const img = item.querySelector("img");

                item.classList.toggle("inactive", !newState);
                img.dataset.isActive = newState.toString();
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
            if (!file.type.startsWith("image/")) return;

            const index = startIndex + i;
            const reader = new FileReader();

            reader.onload = e => {
                const wrapper = document.createElement("div");
                wrapper.className = "img-item";

                wrapper.innerHTML = `
                    <img src="${e.target.result}" alt="" class="preview-img" />
                    <button type="button" class="btn-close-custom" aria-label="Close">
                        <i class="bi bi-x-lg"></i>
                    </button>
                    <input type="text" class="form-control mt-2 alt-input"
                        placeholder="AltText (必填)" name="Meta[${index}].AltText" required>
                    <textarea class="form-control mt-2 caption-input"
                        placeholder="Caption (必填)" name="Meta[${index}].Caption" required></textarea>
                `;

                wrapper.querySelector(".btn-close-custom").addEventListener("click", () => {
                    wrapper.remove();
                    showReset();
                    if (preview.children.length === 0) togglePreviewArea(false);
                });

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
            };

            reader.readAsDataURL(file);
        });

        if (files.length > 0) {
            togglePreviewArea(true);
            showReset();
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

    // === 選擇檔案按鈕 ===
    if (selectBtn && fileInput) {
        selectBtn.addEventListener("click", () => fileInput.click());
        fileInput.addEventListener("change", () => {
            if (fileInput.files.length > 0) {
                showPreview(fileInput.files);
                fileInput.value = "";
            }
        });
    }

    // === Modal（支援多個 UpdateImage 元件） ===
    document.querySelectorAll(".modal").forEach(modalElement => {
        const modalImg = modalElement.querySelector(".img-zoomable");
        if (!modalImg) return;

        const modalAlt = modalElement.querySelector("#modalAlt");
        const modalCaption = modalElement.querySelector("#modalCaption");
        const modalIsActive = modalElement.querySelector("#modalIsActive");
        const confirmBtn = modalElement.querySelector("#confirmMetaBtn");
        // === 點擊 Modal 內的圖片 → 直接開原圖 ===

        modalImg.addEventListener("click", e => {
            e.preventDefault();
            e.stopPropagation();
            const imgUrl = modalImg.src;
            if (imgUrl) window.open(imgUrl, "_blank");
        });

        // === 點擊縮圖開啟 Modal ===
        document.querySelectorAll(`.thumb-clickable[data-bs-target="#${modalElement.id}"]`).forEach(img => {
            img.addEventListener("click", () => {
                modalImg.src = img.src;
                modalAlt.value = img.dataset.alt || "";
                modalCaption.value = img.dataset.caption || "";
                modalIsActive.checked = img.dataset.isActive === "true";

                // 同步 fileId
                modalImg.dataset.fileId = img.dataset.fileId;

                // 關鍵：只在縮圖沒有 API 時才保留 Razor 預設值
                modalImg.dataset.updateApi = img.dataset.updateApi || modalImg.dataset.updateApi;
                modalImg.dataset.deleteApi = img.dataset.deleteApi || modalImg.dataset.deleteApi;

                // 開啟 Modal
                const instance = bootstrap.Modal.getOrCreateInstance(modalElement);
                instance.show();
            });
        });

        // === Modal 確認更新 ===
        if (confirmBtn) {
            if (!modalImg.dataset.updateApi) {
                console.warn("⚠️ 找不到 updateApi，請確認 shown.bs.modal 有正確同步屬性");
            }

            confirmBtn.addEventListener("click", async () => {
                const fileId = modalImg.dataset.fileId;
                if (!fileId) {
                    Swal.fire("⚠️ 找不到圖片 ID", "", "warning");
                    return;
                }

                const altText = modalAlt.value.trim();
                const caption = modalCaption.value.trim();
                const isActive = modalIsActive.checked;
                const updateApiUrl = modalImg.dataset.updateApi || "/SYS/UploadTest/UpdateMeta";

                try {
                    const res = await fetch(`${window.location.origin}${updateApiUrl}`, {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify({ FileId: parseInt(fileId), AltText: altText, Caption: caption, IsActive: isActive })
                    });
                    const data = await res.json();

                    if (data.success) {
                        await Swal.fire({
                            icon: "success",
                            title: "✅ 更新成功",
                            timer: 1000,
                            showConfirmButton: false
                        });

                        updateImageState(fileId, altText, caption, isActive);
                        bootstrap.Modal.getInstance(modalElement)?.hide();
                    } else {
                        Swal.fire("❌ 更新失敗", data.message || "", "error");
                    }
                } catch (err) {
                    Swal.fire("❌ 錯誤", "無法連線至伺服器", "error");
                    console.error("update-image.js 錯誤:", err);
                }
            });
        }
    });

    // === 更新畫面資料 ===
    function updateImageState(fileId, altText, caption, isActive) {
        const img = document.querySelector(`.thumb-clickable[data-file-id="${fileId}"]`);
        if (!img) return;

        img.alt = altText;
        img.dataset.alt = altText;
        img.dataset.caption = caption;
        img.dataset.isActive = isActive.toString();

        const parent = img.closest(".img-item");
        parent.classList.toggle("inactive", !isActive);

        const toggleBtn = parent.querySelector(".toggle-active-btn");
        if (toggleBtn) toggleBtn.textContent = isActive ? "停用" : "啟用";
    }

    // === 刪除圖片 ===
    window.deleteFile = async function (fileId, btn) {
        // ✅ 從按鈕或圖片讀取 delete API（或預設）
        const deleteApi = btn?.dataset.deleteApi || "/SYS/UploadTest/DeleteFile";

        const confirm = await Swal.fire({
            title: "確定刪除？",
            text: "此圖片將從雲端與資料庫永久移除",
            icon: "warning",
            showCancelButton: true,
            confirmButtonText: "刪除",
            cancelButtonText: "取消"
        });
        if (!confirm.isConfirmed) return;

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
            const targetImg = document.querySelector(`.thumb-clickable[data-file-id="${fileId}"]`);
            if (targetImg) {
                const parent = targetImg.closest(".img-item");
                if (parent) parent.remove();
            }

            const grid = document.querySelector("form > div.img-grid");
            if (grid && grid.children.length === 0) {
                grid.innerHTML = `<p class="text-muted">目前沒有圖片。</p>`;
            }
        } else {
            Swal.fire("❌ 刪除失敗", data.message || "", "error");
        }
    };

    // === 修正版本：正確更新 Modal 內容 ===
    document.addEventListener("shown.bs.modal", function (event) {
        const modal = event.target;
        const triggerImg = event.relatedTarget;
        if (!triggerImg) return;

        const modalImg = modal.querySelector(".img-zoomable");
        const modalAlt = modal.querySelector("#modalAlt");
        const modalCaption = modal.querySelector("#modalCaption");
        const modalIsActive = modal.querySelector("#modalIsActive");

        // 更新 Modal 內容
        modalImg.src = triggerImg.src;
        modalImg.alt = triggerImg.alt;
        modalAlt.value = triggerImg.dataset.alt || "";
        modalCaption.value = triggerImg.dataset.caption || "";
        modalIsActive.checked = triggerImg.dataset.isActive === "true";

        // 關鍵修正：把縮圖的 API 屬性同步進 modal 圖片
        modalImg.dataset.fileId = triggerImg.dataset.fileId;
        modalImg.dataset.updateApi = triggerImg.dataset.updateApi;
        modalImg.dataset.deleteApi = triggerImg.dataset.deleteApi;

        // 移除無用的變數 modalElement，改為直接聚焦 modal
        modal.focus();
    });

    document.addEventListener("hidden.bs.modal", () => {
        if (document.activeElement && document.activeElement.classList.contains("btn-close")) {
            document.activeElement.blur();
        }
    });
});