document.addEventListener("DOMContentLoaded", function () {
    // === Modal ===
    const modal = new bootstrap.Modal(document.getElementById("imgMetaModal"));
    const modalImg = document.getElementById("modalImg");
    const modalAlt = document.getElementById("modalAlt");
    const modalCaption = document.getElementById("modalCaption");
    const modalIsActive = document.getElementById("modalIsActive"); // 新增開關
    const confirmBtn = document.getElementById("confirmMetaBtn");

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

    // === 啟用/停用按鈕 ===
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
            const res = await fetch(`/SYS/UploadTest/UpdateMeta`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    FileId: fileId,
                    IsActive: newState
                })
            });

            const data = await res.json();

            if (data.success) {
                const item = btn.closest(".img-item");
                const img = item.querySelector("img");

                // 更新畫面狀態
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
            Swal.fire("❌ 錯誤", "伺服器連線失敗或回傳格式錯誤", "error");
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

                // 移除預覽圖按鈕
                wrapper.querySelector(".btn-close-custom").addEventListener("click", () => {
                    wrapper.remove();
                    showReset();
                    if (preview.children.length === 0) togglePreviewArea(false);
                });

                preview.appendChild(wrapper);

                // 隱藏 input 存檔案
                const dataTransfer = new DataTransfer();
                dataTransfer.items.add(file);

                const fileInput = document.createElement("input");
                fileInput.type = "file";
                fileInput.name = `Meta[${index}].File`;
                fileInput.files = dataTransfer.files;
                fileInput.hidden = true;
                hiddenInputs.appendChild(fileInput);
            };

            reader.readAsDataURL(file);
        });

        if (files.length > 0) {
            togglePreviewArea(true);
            showReset();
        }
    }

    // === 拖拉區塊 ===
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

    // === 選擇檔案 ===
    if (selectBtn && fileInput) {
        selectBtn.addEventListener("click", () => fileInput.click());
        fileInput.addEventListener("change", () => {
            if (fileInput.files.length > 0) {
                showPreview(fileInput.files);
                fileInput.value = ""; // 允許重選相同檔案
            }
        });
    }

    // === 編輯圖片資訊 (開啟 Modal) ===
    document.querySelectorAll(".thumb-clickable").forEach(img => {
        const parentButton = img.closest(".img-item")?.querySelector(".btn-close-custom");
        const fileId = parentButton?.getAttribute("onclick")?.match(/\d+/)?.[0];
        img.dataset.fileId = fileId || "";

        img.addEventListener("click", function () {
            modalImg.src = this.src;
            modalImg.alt = this.alt;
            modalAlt.value = this.dataset.alt || "";
            modalCaption.value = this.dataset.caption || "";
            modalIsActive.checked = this.dataset.isActive === "true";
            modalImg.dataset.fileId = this.dataset.fileId;
            modal.show();
        });
    });

    // === 點擊 Modal 內的圖片 → 直接另開原圖網址 ===
    modalImg.addEventListener("click", function (e) {
        e.preventDefault();
        e.stopPropagation();

        const imgUrl = this.src;
        if (!imgUrl) return;

        // 直接開新分頁顯示原圖
        window.open(imgUrl, "_blank");
    });

    // === 確認更新 (Modal 確定) ===
    if (confirmBtn) {
        confirmBtn.addEventListener("click", async function () {
            const fileId = modalImg.dataset.fileId;
            const altText = modalAlt.value.trim();
            const caption = modalCaption.value.trim();

            if (!fileId) {
                Swal.fire("⚠️ 找不到圖片 ID，無法更新", "", "warning");
                return;
            }

            try {
                const isActive = modalIsActive.checked;

                const res = await fetch(`${window.location.origin}/SYS/UploadTest/UpdateMeta`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({
                        FileId: parseInt(fileId),
                        AltText: altText,
                        Caption: caption,
                        IsActive: isActive
                    })
                });

                const data = await res.json();

                if (data.success) {
                    Swal.fire("✅ 更新成功", "", "success");

                    // ✅ 傳進所有需要更新的資料
                    updateImageState(fileId, altText, caption, isActive);

                    modal.hide();
                } else {
                    Swal.fire("❌ 更新失敗", data.message || "", "error");
                }
            } catch (err) {
                Swal.fire("❌ 錯誤", "無法連線至伺服器或回傳格式錯誤", "error");
                console.error("更新錯誤：", err);
            }
        });
    }

    // ✅ 共用更新函式（可供 Modal 與 toggleActive 共用）
    function updateImageState(fileId, altText, caption, isActive) {
        const img = document.querySelector(`.thumb-clickable[data-file-id="${fileId}"]`);
        if (!img) return;

        img.alt = altText;
        img.dataset.alt = altText;
        img.dataset.caption = caption;
        img.dataset.isActive = isActive.toString();

        // 動態標記出啟用／停用狀態
        const parent = img.closest(".img-item");
        parent.classList.toggle("inactive", !isActive);

        // 更新按鈕文字
        const toggleBtn = parent.querySelector(".toggle-active-btn");
        if (toggleBtn) {
            toggleBtn.textContent = isActive ? "停用" : "啟用";
        }
    }

    // === 刪除圖片 ===
    window.deleteFile = async function (fileId) {
        const confirm = await Swal.fire({
            title: "確定刪除？",
            text: "此圖片將從 Cloudinary 與資料庫永久移除",
            icon: "warning",
            showCancelButton: true,
            confirmButtonText: "刪除",
            cancelButtonText: "取消"
        });
        if (!confirm.isConfirmed) return;

        const res = await fetch(`/SYS/UploadTest/DeleteFile`, {
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

            // 1. 直接從畫面移除該圖片
            const targetImg = document.querySelector(`.thumb-clickable[data-file-id="${fileId}"]`);
            if (targetImg) {
                const parent = targetImg.closest(".img-item");
                if (parent) parent.remove();
            }

            // 2. 若全部圖片都刪完，就隱藏外層區域
            const grid = document.querySelector("form > div.img-grid");
            if (grid && grid.children.length === 0) {
                grid.innerHTML = `<p class="text-muted">目前沒有圖片。</p>`;
            }
        } else {
            Swal.fire("❌ 刪除失敗", data.message || "", "error");
        }
    };
});