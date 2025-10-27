document.addEventListener("DOMContentLoaded", function () {
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
        const updateApiUrl = img.dataset.updateApi || "/SYS/Images/UpdateFile";

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
            const res = await fetch(updateApiUrl, {
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
        const modal = await waitForElement(modalSelector);
        if (!modal) {
            console.error("❌ 找不到指定的 Modal：", modalSelector);
            return;
        }

        const modalImg = modal.querySelector(".img-zoomable");
        const bsModal = bootstrap.Modal.getOrCreateInstance(modal);

        // === 更新欄位 ===
        for (const [key, selector] of Object.entries(fieldMap)) {
            const input = modal.querySelector(selector);
            if (!input) continue;

            let val = fileData[key] ?? fileData[key.charAt(0).toLowerCase() + key.slice(1)];
            if (key === "createdDate") {
                val = fileData.formateCreatedDate || fileData.FormateCreatedDate || val;
            }

            if (key === "isActive") {
                input.checked = val === true || val === "true";
            } else if (input.tagName === "INPUT" || input.tagName === "TEXTAREA") {
                input.value = val ?? "";
            }
        }

        // === 圖片安全載入 + dataset ===
        const fileUrl =
            fileData.PublicUrl || fileData.publicUrl ||
            fileData.FileUrl || fileData.fileUrl ||
            "/images/No-Image.svg";
        const fileId = fileData.FileId ?? fileData.fileId ?? "";

        if (modalImg) {
            modalImg.src = fileUrl;
            modalImg.dataset.fileId = fileId;
            modalImg.dataset.isExternal = (
                fileData.IsExternal === true ||
                fileData.isExternal === true ||
                fileData.IsExternal === "true" ||
                fileData.isExternal === "true" ||
                fileUrl.startsWith("http")
            ).toString();

            // 更新 API dataset
            if (fileData.UpdateApiUrl) modalImg.dataset.updateApi = fileData.UpdateApiUrl;
            if (fileData.DeleteApiUrl) modalImg.dataset.deleteApi = fileData.DeleteApiUrl;
        }

        // === ⬇️ 移到這裡：在 src 已更新後再檢查外部連結 ===
        const badge = modal.querySelector(".badge.fs-6");
        if (badge && modalImg) {
            const isExternal =
                modalImg.dataset.isExternal === "true" ||
                (modalImg.src?.startsWith("http") && !modalImg.src.includes(window.location.hostname));

            badge.textContent = isExternal ? "外部連結" : "自有檔案";
            badge.classList.toggle("bg-success", isExternal);
            badge.classList.toggle("bg-secondary", !isExternal);
        }

        bsModal.show();
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
                openImageModal(img.dataset, `#${modalElement.id}`);
            });
        });

        // === Modal 確認更新 ===
        if (confirmBtn) {
            if (!modalImg.dataset.updateApi) {
                console.warn("⚠️ 找不到 updateApi，請確認 shown.bs.modal 有正確同步屬性");
            }

            confirmBtn.addEventListener("click", async () => {
                const modal = document.querySelector("#imgMetaModal");
                const modalImg = modal.querySelector(".img-zoomable");

                const api = modalImg.dataset.updateApi || "/SYS/UploadTest/UpdateFile";
                const fileId = modalImg.dataset.fileId;

                const alt = modal.querySelector("#modalAlt").value;
                const caption = modal.querySelector("#modalCaption").value;
                const isActive = modal.querySelector("#modalIsActive").checked;
                const width = modal.querySelector("#modalWidth").value;
                const height = modal.querySelector("#modalHeight").value;

                if (!api) {
                    Swal.fire({ icon: "error", title: "找不到更新 API", text: "請檢查 updateApiUrl 是否設定。" });
                    return;
                }

                try {
                    const res = await fetch(api, {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify({
                            FileId: fileId,
                            AltText: alt,
                            Caption: caption,
                            IsActive: isActive,
                            Width: width,
                            Height: height
                        })
                    });

                    const contentType = res.headers.get("content-type") || "";
                    let result = null;

                    // 檢查回傳型別是不是 JSON
                    if (!contentType.includes("application/json")) {
                        // 伺服器沒有回 JSON → 可能是 HTML 錯誤頁或未登入頁
                        const text = await res.text();
                        console.error("⚠️ 伺服器回傳非 JSON：", text.slice(0, 150));
                        throw new Error("伺服器回傳非 JSON（可能路由錯誤或登入過期）");
                    } else {
                        result = await res.json();
                    }

                    if (!res.ok || !result) {
                        throw new Error(result?.message || `伺服器狀態碼 ${res.status}`);
                    }

                    if (result.success) {
                        Swal.fire({
                            icon: "success",
                            title: "更新成功",
                            timer: 1000,
                            showConfirmButton: false
                        });

                        // 關閉 Modal
                        const bsModal = bootstrap.Modal.getInstance(modal);
                        if (bsModal) bsModal.hide();

                        // 同步更新畫面上的縮圖資料
                        updateImageState(fileId, alt, caption, isActive);

                        // 立即更新圖片的寬高 dataset（否則會顯示舊值）
                        const img = document.querySelector(`.thumb-clickable[data-file-id="${fileId}"]`);
                        if (img) {
                            const modalWidth = modal.querySelector("#modalWidth")?.value;
                            const modalHeight = modal.querySelector("#modalHeight")?.value;

                            img.dataset.width = modalWidth || img.dataset.width;
                            img.dataset.height = modalHeight || img.dataset.height;
                        }
                    } else {
                        Swal.fire({
                            icon: "error",
                            title: "更新失敗",
                            text: result.message || "伺服器未回傳成功"
                        });
                    }
                } catch (err) {
                    console.error("❌ 更新錯誤：", err);
                    Swal.fire({
                        icon: "error",
                        title: "錯誤",
                        text: err.message || "無法連線至伺服器"
                    });
                }
            });
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
        modalImg.src = triggerImg.src;

        // 逐一更新欄位
        for (const [key, selector] of Object.entries(fieldMap)) {
            const input = modal.querySelector(selector);
            if (!input) continue;

            let val = triggerImg.dataset[key];
            if (key === "isActive") {
                input.checked = val === "true";
            } else if (input.tagName === "INPUT" || input.tagName === "TEXTAREA") {
                input.value = val || "";
            }
        }

        // 把縮圖的 API 屬性同步進 dataset 圖片
        modalImg.dataset.fileId = triggerImg.dataset.fileId;
        modalImg.dataset.updateApi = triggerImg.dataset.updateApi;
        modalImg.dataset.deleteApi = triggerImg.dataset.deleteApi;
    });

    document.addEventListener("hidden.bs.modal", () => {
        if (document.activeElement && document.activeElement.classList.contains("btn-close")) {
            document.activeElement.blur();
        }
    });

    window.openImageModal = openImageModal;
});