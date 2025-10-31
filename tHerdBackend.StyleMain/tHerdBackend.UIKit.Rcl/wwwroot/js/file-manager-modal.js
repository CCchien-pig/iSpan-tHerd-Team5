window.FileManagerModal = (function () {
    let _modal, _callback;

    async function open(onConfirm) {
        _callback = onConfirm;
        const modalEl = document.getElementById("fileManagerModal");
        _modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        _modal.show();
    }

    document.addEventListener("DOMContentLoaded", function () {
        const btn = document.getElementById("btnConfirmFileSelect");
        if (btn) {
            btn.addEventListener("click", function () {
                const fmObj = document.getElementById("fileManager_fileManagerModal").ej2_instances[0];
                const files = fmObj.getSelectedFiles(); // Syncfusion API
                if (typeof _callback === "function") {
                    _callback(files);
                }
                _modal.hide();
            });
        }
    });

    return { open };
})();
