//圖片模組

// =======================================================
// UploadImageModal 模組化版本
// =======================================================
window.UploadImageModal = (function () {
    // === 初始化 ===
    function init(modalId = "uploadImageModal") {
        const modal = document.getElementById(modalId);
        if (!modal) return;

        // ✅ 改為綁定在 modal 自身，避免作用域錯亂
        if (modal.dataset.initialized === "true") {
            console.warn(`${modalId} 已初始化過，略過重複綁定`);
            return;
        }
        modal.dataset.initialized = "true";

        // ✅ 一定要初始化 allFiles（關鍵修正）
        modal.allFiles = [];
        const allFiles = modal.allFiles;

        const previewArea = modal.querySelector(`#previewArea_${modalId}`);
        const preview = modal.querySelector(`#preview_${modalId}`);
        const dropArea = modal.querySelector(`#dropArea_${modalId}`);
        const fileInput = modal.querySelector(`#fileInput_${modalId}`);
        const selectBtn = modal.querySelector(`#selectBtn_${modalId}`);
        const hiddenInputs = modal.querySelector(`#hiddenInputs_${modalId}`);
        const btnConfirm = modal.querySelector(`#btnConfirmUpload_${modalId}`);

        // === 綁定事件 ===
        bindFileSelect(selectBtn, fileInput, previewArea, preview, hiddenInputs, allFiles, modalId);
        bindDropArea(dropArea, previewArea, preview, hiddenInputs, allFiles, modalId);
        bindUploadButton(btnConfirm, preview, allFiles, modal, modalId);
    }

    // =======================================================
    // 綁定：檔案選擇按鈕
    // =======================================================
    function bindFileSelect(selectBtn, fileInput, previewArea, preview, hiddenInputs, allFiles, modalId) {
        if (!selectBtn || !fileInput) return;

        // 🔧 移除舊的綁定，確保只有一個 listener
        selectBtn.replaceWith(selectBtn.cloneNode(true));
        const newSelectBtn = document.getElementById(`selectBtn_${modalId}`);
        fileInput.replaceWith(fileInput.cloneNode(true));
        const newFileInput = document.getElementById(`fileInput_${modalId}`);

        newSelectBtn.addEventListener("click", () => newFileInput.click());
        newFileInput.addEventListener("change", () => {
            if (newFileInput.files.length > 0) {
                showPreview(newFileInput.files, previewArea, preview, hiddenInputs, allFiles, modalId);
                newFileInput.value = "";
            }
        });
    }

    // =======================================================
    // 綁定：拖拉區域事件
    // =======================================================
    function bindDropArea(dropArea, previewArea, preview, hiddenInputs, allFiles, modalId) {
        if (!dropArea) return;

        ["dragenter", "dragover", "dragleave", "drop"].forEach(evt =>
            dropArea.addEventListener(evt, e => {
                e.preventDefault();
                e.stopPropagation();
            })
        );

        ["dragenter", "dragover"].forEach(evt =>
            dropArea.addEventListener(evt, () => dropArea.classList.add("bg-info", "bg-opacity-25"))
        );
        ["dragleave", "drop"].forEach(evt =>
            dropArea.addEventListener(evt, () => dropArea.classList.remove("bg-info", "bg-opacity-25"))
        );

        dropArea.addEventListener("drop", e => {
            const files = e.dataTransfer.files;
            if (files.length > 0)
                showPreview(files, previewArea, preview, hiddenInputs, allFiles, modalId);
        });
    }

    // =======================================================
    // 綁定：Form Submit（使用 fetch）
    // =======================================================
    function bindUploadButton(btnConfirm, preview, allFiles, modal, modalId) {
        if (!btnConfirm) return;

        btnConfirm.addEventListener("click", async (e) => {
            e.preventDefault();

            if (allFiles.length === 0) {
                Swal.fire("提示", "請先選擇至少一個檔案", "info");
                return;
            }

            const MAX_SIZE = 100 * 1024 * 1024; // 100MB
            const bigFiles = allFiles.filter(f => f.size > MAX_SIZE);
            if (bigFiles.length > 0) {
                Swal.fire({
                    title: "⚠️ 檔案過大",
                    text: `有 ${bigFiles.length} 個檔案超過 100MB，請壓縮或改用本地上傳`,
                    icon: "warning"
                });
                return;
            }

            const form = document.getElementById(`form_${modalId}`);
            const apiUrl = form?.dataset?.action || "/SYS/UploadTest/SaveFiles";
            const moduleId = form?.querySelector("input[name='ModuleId']")?.value || "";
            const progId = form?.querySelector("input[name='ProgId']")?.value || "";
            const isExternal = form?.querySelector("input[name='IsExternal']:checked")?.value || "false";

            const formData = new FormData();
            formData.append("ModuleId", moduleId);
            formData.append("ProgId", progId);
            formData.append("IsExternal", isExternal);

            allFiles.forEach((file, i) => {
                formData.append(`Meta[${i}].File`, file);
                const alt = preview.querySelector(`input[name='Meta[${i}].AltText']`)?.value || "";
                const caption = preview.querySelector(`textarea[name='Meta[${i}].Caption']`)?.value || "";
                formData.append(`Meta[${i}].AltText`, alt);
                formData.append(`Meta[${i}].Caption`, caption);
            });

            // === 🌀 顯示全域 Loading ===
            showGlobalLoading("正在上傳檔案...");

            try {
                const res = await fetch(apiUrl, { method: "POST", body: formData });
                let result = {};
                try {
                    result = await res.json();
                } catch {
                    throw new Error("伺服器未回傳有效資料");
                }
                if (!res.ok || !result.success) throw new Error(result.message || "上傳失敗");

                modal.dataset.justUploaded = "true"; // 🟡 標記成功

                Swal.fire({ icon: "success", title: "上傳完成", timer: 1000, showConfirmButton: false });
                modal.querySelector(".btn-close")?.click();
                document.dispatchEvent(new CustomEvent("upload-success", {
                    detail: {
                        moduleId,
                        progId
                    }
                }));
            } catch (err) {
                Swal.fire("錯誤", err.message || "上傳失敗", "error");
            } finally {
                hideGlobalLoading();
            }
        });
    }

    // =======================================================
    // 建立預覽（圖片 / 影片 / 文件通用版本）
    // =======================================================
    function showPreview(files, previewArea, preview, hiddenInputs, allFiles, modalId) {
        const startIndex = allFiles.length;

        [...files].forEach((file, i) => {
            const index = startIndex + i;
            allFiles.push(file);

            const mime = file.type || "";
            const ext = file.name.split('.').pop().toLowerCase();
            const wrapper = document.createElement("div");
            wrapper.className = "img-item text-center p-2 border rounded position-relative shadow-sm me-2 mb-2";
            wrapper.style.width = "150px";
            wrapper.style.minHeight = "200px";
            wrapper.style.background = "linear-gradient(to bottom, #f8f9fa, #ffffff)";
            wrapper.style.transition = "all .2s ease";

            const renderPreview = (content) => {
                wrapper.innerHTML = `
                <div class="position-absolute top-0 end-0 m-1 z-3">
                  <button type="button"
                      class="btn btn-sm border-0 p-0 btn-close-custom"
                      aria-label="Close"
                      style="
                          width: 26px;
                          height: 26px;
                          border-radius: 50%;
                          background-color: rgba(255,255,255,0.85);
                          display: flex;
                          align-items: center;
                          justify-content: center;
                          box-shadow: 0 1px 3px rgba(0,0,0,0.3);
                      ">
                      <i class="bi bi-x-lg text-dark" style="font-size:0.9rem;"></i>
                  </button>
                </div>
                ${content}
                <p class="small text-muted text-break mt-1">${file.name}</p>
                <input type="text" class="form-control form-control-sm mt-2"
                       name="Meta[${index}].AltText" placeholder="AltText (可選)">
                <textarea class="form-control form-control-sm mt-2"
                          name="Meta[${index}].Caption" placeholder="Caption (可選)"></textarea>
            `;
                preview.appendChild(wrapper);
                addRemoveHandler(wrapper, preview, hiddenInputs, allFiles, previewArea);
            };

            // === 🖼 圖片預覽 ===
            if (mime.startsWith("image/")) {
                const reader = new FileReader();
                reader.onload = (e) => renderPreview(`
                <div class="d-flex justify-content-center">
                    <img src="${e.target.result}" 
                         class="img-thumbnail" 
                         style="width:130px;height:130px;object-fit:cover;border-radius:6px;">
                </div>
            `);
                reader.readAsDataURL(file);
            }

            // === 🎥 影片預覽 ===
            else if (mime.startsWith("video/")) {
                const videoUrl = URL.createObjectURL(file);
                renderPreview(`
                <div class="d-flex justify-content-center">
                    <video src="${videoUrl}" controls
                        style="width:130px;height:100px;border-radius:6px;object-fit:cover;background:#000;"></video>
                </div>
            `);
            }

            // === 📄 PDF / 文件預覽 ===
            else if (ext === "pdf") {
                const fileUrl = URL.createObjectURL(file);
                renderPreview(`
                <div class="d-flex justify-content-center align-items-center flex-column">
                    <i class="bi bi-file-earmark-pdf text-danger" style="font-size:3rem;"></i>
                    <a href="${fileUrl}" target="_blank" class="small text-decoration-none text-primary">預覽 PDF</a>
                </div>
            `);
            }

            // === 📘 其他檔案類型（txt, docx, zip...） ===
            else {
                renderPreview(`
                <div class="d-flex justify-content-center align-items-center flex-column">
                    <i class="bi bi-file-earmark-text text-secondary" style="font-size:3rem;"></i>
                    <p class="small text-muted">${ext.toUpperCase()} 檔案</p>
                </div>
            `);
            }
        });

        if (files.length > 0)
            previewArea.classList.remove("d-none");
    }

    // =======================================================
    // 刪除預覽與同步移除 hidden input
    // =======================================================
    function addRemoveHandler(wrapper, preview, hiddenInputs, allFiles, previewArea) {
        wrapper.querySelector(".btn-close-custom")?.addEventListener("click", () => {
            const pos = Array.from(preview.children).indexOf(wrapper);
            allFiles.splice(pos, 1);
            wrapper.remove();
            rebuildHiddenInputs(hiddenInputs, allFiles);

            const hidden = hiddenInputs.querySelector(`input[name='Meta[${pos}].File']`);
            if (hidden) hidden.remove();

            if (preview.children.length === 0)
                previewArea.classList.add("d-none");
        });
    }

    function rebuildHiddenInputs(hiddenInputs, allFiles) {
        // 不再建立 <input type="file">，直接清空容器即可
        hiddenInputs.innerHTML = "";
        // 若你未來想要顯示已選檔案名稱，可選擇保留資訊在 hidden text input
        allFiles.forEach((file, i) => {
            const info = document.createElement("input");
            info.type = "hidden";
            info.name = `Meta[${i}].FileName`;
            info.value = file.name;
            hiddenInputs.appendChild(info);
        });
    }


    // 對外導出方法
    return {
        init
    };

})();

// === 🔹 統一回到圖片選擇器的函式 ===
async function returnToImageSelector() {
    // === 💡 統一使用 showGlobalLoading / hideGlobalLoading ===
    const showLoading = window.showGlobalLoading;
    const hideLoading = window.hideGlobalLoading;

    const uploadModalEl = document.getElementById("uploadImageModal");
    const selectorModalEl = document.getElementById("imageSelectorModal");

    // 🔹 先關閉「上傳 Modal」
    const uploadModal = bootstrap.Modal.getInstance(uploadModalEl);
    if (uploadModal) {
        uploadModal.hide();
        await new Promise(r => setTimeout(r, 350)); // 等動畫結束
    }

    // 🔹 如果沒有 selectorModal，就直接結束（不做任何切換）
    if (!selectorModalEl) {
        return;
    }

    // === 🌀 顯示 Loading ===
    if (typeof showGlobalLoading === "function") {
        showGlobalLoading("正在更新圖片清單...");
    }

    // 🔹 顯示圖片選擇器 Modal
    const selectorModal = bootstrap.Modal.getOrCreateInstance(selectorModalEl);
    selectorModal.show();

    // 延遲執行刷新（避免 modal 動畫重疊）
    setTimeout(async () => {
        try {
            if (typeof loadModuleImages === "function") {
                await loadModuleImages(_currentModuleId, _currentProgId);
            }
        } catch (err) {
            console.error("❌ 刷新圖片清單失敗：", err);
        } finally {
            // === ✅ 確保 Loading 一定會關掉 ===
            if (typeof hideLoading === "function") {
                hideLoading();
            }
        }
    }, 250);
}


// 🟢 成功與取消都刷新
document.addEventListener("upload-success", () => returnToImageSelector());
document.addEventListener("hidden.bs.modal", e => {
    if (e.target.id === "uploadImageModal" && !e.target.dataset.justUploaded)
        returnToImageSelector();
});