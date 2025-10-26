// =======================================================
// UploadImageModal 模組化版本
// =======================================================
window.UploadImageModal = (function () {

    // === 初始化 ===
    function init(modalId) {
        const modal = document.getElementById(modalId);
        if (!modal) {
            console.warn("❌ 找不到 modal:", modalId);
            return;
        }

        const form = modal.querySelector("form");
        const previewArea = modal.querySelector(`#previewArea_${modalId}`);
        const preview = modal.querySelector(`#preview_${modalId}`);
        const dropArea = modal.querySelector(`#dropArea_${modalId}`);
        const fileInput = modal.querySelector(`#fileInput_${modalId}`);
        const selectBtn = modal.querySelector(`#selectBtn_${modalId}`);
        const hiddenInputs = modal.querySelector(`#hiddenInputs_${modalId}`);

        const allFiles = [];

        // === 綁定事件 ===
        bindFileSelect(selectBtn, fileInput, previewArea, preview, hiddenInputs, allFiles, modalId);
        bindDropArea(dropArea, previewArea, preview, hiddenInputs, allFiles, modalId);
        bindFormSubmit(form, preview, allFiles, modal, modalId);

        console.log("✅ UploadImageModal initialized:", modalId);
    }

    // =======================================================
    // 綁定：檔案選擇按鈕
    // =======================================================
    function bindFileSelect(selectBtn, fileInput, previewArea, preview, hiddenInputs, allFiles, modalId) {
        if (!selectBtn || !fileInput) return;

        selectBtn.addEventListener("click", () => fileInput.click());

        fileInput.addEventListener("change", () => {
            if (fileInput.files.length > 0) {
                showPreview(fileInput.files, previewArea, preview, hiddenInputs, allFiles, modalId);
                fileInput.value = ""; // 清空避免重複上傳同檔
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
    function bindFormSubmit(form, preview, allFiles, modal, modalId) {
        if (!form) return;

        form.addEventListener("submit", async (e) => {
            e.preventDefault();

            // === 前端預檢查 ===
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

            const formData = new FormData();

            formData.append("IsExternal", form.querySelector("input[name='IsExternal']:checked").value);
            formData.append("ModuleId", form.querySelector("input[name='ModuleId']").value);
            formData.append("ProgId", form.querySelector("input[name='ProgId']").value);

            allFiles.forEach((file, i) => {
                formData.append(`Meta[${i}].File`, file);
                const alt = preview.querySelector(`input[name='Meta[${i}].AltText']`)?.value || "";
                const caption = preview.querySelector(`textarea[name='Meta[${i}].Caption']`)?.value || "";
                formData.append(`Meta[${i}].AltText`, alt);
                formData.append(`Meta[${i}].Caption`, caption);
            });

            console.log("📤 準備上傳:", allFiles.length, "個檔案");

            try {
                const response = await fetch(form.action, { method: "POST", body: formData });

                // 🔍 檢查伺服器是否真的有回應內容
                const text = await response.text();
                if (!response.ok || !text.trim()) {
                    throw new Error("伺服器拒絕請求或回應無效");
                }

                Swal.fire({
                    title: "✅ 上傳成功",
                    text: "檔案已成功送出",
                    icon: "success"
                });
                modal.querySelector(".btn-close")?.click();
            } catch (err) {
                console.error("❌ 上傳錯誤:", err);
                Swal.fire({
                    title: "❌ 上傳失敗",
                    text: err.message || "網路或伺服器異常，可能是檔案太大",
                    icon: "error"
                });
            }
        });
    }

    // =======================================================
    // 建立預覽（包含圖片、影片與文字欄位）
    // =======================================================
    function showPreview(files, previewArea, preview, hiddenInputs, allFiles, modalId) {
        const startIndex = allFiles.length;

        [...files].forEach((file, i) => {
            const index = startIndex + i;
            allFiles.push(file);

            const mime = file.type;
            const wrapper = document.createElement("div");
            wrapper.className = "img-item text-center p-2 border rounded position-relative";

            const renderPreview = (content) => {
                wrapper.innerHTML = content;
                preview.appendChild(wrapper);
                addRemoveHandler(wrapper, preview, hiddenInputs, allFiles, previewArea);
            };

            if (mime.startsWith("image/")) {
                const reader = new FileReader();
                reader.onload = (e) => renderPreview(`
                    <div class="d-flex flex-column align-items-center position-relative">
                        <img src="${e.target.result}" class="img-thumbnail mb-2" 
                             style="width:120px;height:120px;object-fit:cover;border-radius:6px;">
                        <p class="small text-muted text-break">${file.name}</p>
                        <input type="text" class="form-control form-control-sm mt-2"
                            name="Meta[${index}].AltText" placeholder="AltText (可選)">
                        <textarea class="form-control form-control-sm mt-2"
                            name="Meta[${index}].Caption" placeholder="Caption (可選)"></textarea>
                    </div>
                `);
                reader.readAsDataURL(file);
            } else if (mime.startsWith("video/")) {
                const videoUrl = URL.createObjectURL(file);
                renderPreview(`
                    <video src="${videoUrl}" controls 
                        style="width:160px;height:120px;border-radius:6px;object-fit:cover;margin-bottom:6px;"></video>
                    <p class="small text-muted text-break">${file.name}</p>
                    <input type="text" class="form-control form-control-sm mt-2"
                        name="Meta[${index}].AltText" placeholder="AltText (可選)">
                    <textarea class="form-control form-control-sm mt-2"
                        name="Meta[${index}].Caption" placeholder="Caption (可選)"></textarea>
                `);
            }

            // 為每個 File 建立對應的 hidden input
            const input = document.createElement("input");
            input.type = "file";
            input.name = `Meta[${index}].File`;
            input.hidden = true;
            Object.defineProperty(input, "files", { value: [file], writable: false });
            hiddenInputs.appendChild(input);
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

            const hidden = hiddenInputs.querySelector(`input[name='Meta[${pos}].File']`);
            if (hidden) hidden.remove();

            if (preview.children.length === 0)
                previewArea.classList.add("d-none");
        });
    }

    // 對外導出方法
    return {
        init
    };

})();
