// /_content/tHerdBackend.UIKit.Rcl/js/upload-image-modal.js

window.initUploadImageModal = function (modalId) {

    const modal = document.getElementById(modalId);
    if (!modal) {
        console.warn("找不到 modal:", modalId);
        return;
    }

    // 根據 modalId 綁對的元素
    const previewArea = modal.querySelector(`#previewArea_${modalId}`);
    const preview = modal.querySelector(`#preview_${modalId}`);
    const dropArea = modal.querySelector(`#dropArea_${modalId}`);
    const fileInput = modal.querySelector(`#fileInput_${modalId}`);
    const selectBtn = modal.querySelector(`#selectBtn_${modalId}`);
    const hiddenInputs = modal.querySelector(`#hiddenInputs_${modalId}`);

    // 這段直接搬你原本 showPreview(...)、拖拉事件、selectBtn 事件
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

                // 刪除單張預覽
                wrapper.querySelector(".btn-close-custom").addEventListener("click", () => {
                    wrapper.remove();
                    if (preview.children.length === 0) {
                        previewArea.classList.add("d-none");
                    }
                });

                preview.appendChild(wrapper);

                // 隱藏 input 用來真正送檔案
                const dt = new DataTransfer();
                dt.items.add(file);

                const fileInputHidden = document.createElement("input");
                fileInputHidden.type = "file";
                fileInputHidden.name = `Meta[${index}].File`;
                fileInputHidden.files = dt.files;
                fileInputHidden.hidden = true;

                hiddenInputs.appendChild(fileInputHidden);
            };

            reader.readAsDataURL(file);
        });

        if (files.length > 0) {
            previewArea.classList.remove("d-none");
        }
    }

    // Drag & Drop 綁定
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

    // 「選擇圖片」按鈕
    if (selectBtn && fileInput) {
        selectBtn.addEventListener("click", () => fileInput.click());

        fileInput.addEventListener("change", () => {
            if (fileInput.files.length > 0) {
                showPreview(fileInput.files);
                fileInput.value = "";
            }
        });
    }

    console.log("upload modal initialized:", modalId);
};
