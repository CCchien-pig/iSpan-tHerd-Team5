// folder-tree.js
export async function openFolderTreeSelector({
    title = "選擇資料夾",
    apiUrl = "/SYS/Images/GetTreeData",
    defaultId = null
}) {
    return await Swal.fire({
        title,
        html: `
      <div class="text-start">
        <label class="form-label fw-semibold">選擇資料夾</label>
        <div id="swal-tree-container" 
             class="border rounded" 
             style="max-height:300px; overflow-y:auto; padding:0.5rem;">
        </div>
      </div>
    `,
        focusConfirm: false,
        showCancelButton: true,
        confirmButtonText: "確認",
        cancelButtonText: "取消",
        didOpen: async () => {
            $("#swal-tree-container").jstree({
                core: {
                    data: async function (node, cb) {
                        const url = node.id === "#" ? apiUrl : `${apiUrl}?parentId=${node.id}`;
                        const res = await fetch(url);
                        const data = await res.json();
                        cb(node.id === "#" ? [{ id: "0", parent: "#", text: "📁 根目錄" }, ...data] : data);
                    },
                    themes: { responsive: false },
                },
                plugins: ["wholerow", "search"],
            });

            // 搜尋功能
            const $tree = $("#swal-tree-container");
            const searchBox = $(`<input type="text" id="treeSearchBox"
         class="form-control form-control-sm mb-2" 
         placeholder="🔍 搜尋資料夾..." />`);
            $tree.before(searchBox);

            let to = false;
            searchBox.on("keyup", function () {
                if (to) clearTimeout(to);
                to = setTimeout(() => {
                    const v = $(this).val();
                    $tree.jstree(true).search(v);
                }, 300);
            });

            $tree.on("loaded.jstree", function (e, data) {
                const nodeId = defaultId ? defaultId.toString() : "0";
                data.instance.select_node(nodeId);
                data.instance.open_node(nodeId);
            });
        },
        willClose: () => {
            try {
                $("#swal-tree-container").jstree("destroy").empty();
            } catch { }
            $("#treeSearchBox").remove();
        },
        preConfirm: () => {
            const tree = $("#swal-tree-container").jstree(true);
            const selected = tree.get_selected()[0] || "0";
            return selected === "0" ? null : parseInt(selected);
        },
    });
}