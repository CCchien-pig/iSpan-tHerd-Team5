let currentRmaId = null;

function openRmaDetail(id) {
    currentRmaId = id;

    $.getJSON(`/ORD/Rma/GetReturnDetail/${id}`, function (res) {
        if (!res || !res.ok) {
            Swal.fire("錯誤", res?.message || "資料讀取失敗");
            return;
        }
        const r = res.rma;

        // 訂單商品
        let orderRows = "";
        (r.orderItems || []).forEach(it => {
            orderRows += `<tr>
        <td>${it.productName ?? "-"}</td>
        <td>${it.skuSpec ?? "-"}</td>
        <td>$${Number(it.unitPrice ?? 0).toLocaleString()}</td>
        <td>${it.qty ?? 0}</td>
        <td>$${Number(it.subTotal ?? 0).toLocaleString()}</td>
      </tr>`;
        });
        $("#orderItemsBody").html(orderRows);

        // 摘要
        $("#coupon").text(r.orderSummary?.coupon ?? "-");
        $("#discount").text("$" + Number(r.orderSummary?.discount ?? 0).toLocaleString());
        $("#shippingFee").text("$" + Number(r.orderSummary?.shippingFee ?? 0).toLocaleString());
        $("#orderTotal").text("$" + Number(r.orderSummary?.total ?? 0).toLocaleString());

        // RMA 區塊
        $("#statusName").text(r.statusName ?? "-");
        $("#createdDate").text(r.createdDate ? new Date(r.createdDate).toLocaleString() : "-");
        $("#reasonText").text(r.reasonText ?? "-");

        // 退貨項目
        let rmaRows = "";
        (r.rmaItems || []).forEach(it => {
            rmaRows += `<tr>
        <td>${it.productName ?? "-"}</td>
        <td>${it.skuSpec ?? "-"}</td>
        <td>${it.originalQty ?? "-"}</td>
        <td>${it.qty ?? "-"}</td>
        <td>${it.approvedQty ?? "-"}</td>
        <td>${it.refundQty ?? "-"}</td>
        <td>${it.reshipQty ?? "-"}</td>
        <td>${it.refundUnitAmount != null ? "$" + Number(it.refundUnitAmount).toLocaleString() : "-"}</td>
      </tr>`;
        });
        $("#rmaItemsBody").html(rmaRows);

        // 狀態控制按鈕
        const s = (r.statusName || "").toLowerCase();
        $("#btnApproveRefund, #btnApproveReship, #btnReject, #btnComplete").addClass("d-none");
        if (s === "pending" || s === "review") {
            $("#btnApproveRefund, #btnApproveReship, #btnReject").removeClass("d-none");
        } else if (s === "refunding") {
            $("#btnComplete").removeClass("d-none");
        }

        $("#rmaDetailModal").modal("show");
    });
}

// 列表上的操作
function approveRefund(id) {
    $.post(`/ORD/Rma/Approve`, { id, nextStatus: "refunding" }, function (res) {
        if (res.ok) Swal.fire("成功", "已批准退款", "success").then(() => location.reload());
        else Swal.fire("錯誤", res.message || "失敗", "error");
    });
}
function approveReship(id) {
    $.post(`/ORD/Rma/Approve`, { id, nextStatus: "reshipping" }, function (res) {
        if (res.ok) Swal.fire("成功", "已批准補寄", "success").then(() => location.reload());
        else Swal.fire("錯誤", res.message || "失敗", "error");
    });
}
function rejectRma(id) {
    Swal.fire({
        title: "確定要駁回嗎？",
        input: "text",
        inputLabel: "駁回原因",
        showCancelButton: true,
        confirmButtonText: "駁回",
        cancelButtonText: "取消"
    }).then(r => {
        if (!r.isConfirmed) return;
        $.post(`/ORD/Rma/Reject`, { id, reason: r.value || "" }, function (res) {
            if (res.ok) Swal.fire("已駁回", "已更新狀態", "info").then(() => location.reload());
            else Swal.fire("錯誤", res.message || "失敗", "error");
        });
    });
}
function completeRma(id) {
    $.post(`/ORD/Rma/Complete`, { id }, function (res) {
        if (res.ok) Swal.fire("完成", "已結單", "success").then(() => location.reload());
        else Swal.fire("錯誤", res.message || "失敗", "error");
    });
}

// Modal 內按鈕（使用目前開啟的 id）
$(document).on("click", "#btnApproveRefund", () => approveRefund(currentRmaId));
$(document).on("click", "#btnApproveReship", () => approveReship(currentRmaId));
$(document).on("click", "#btnReject", () => rejectRma(currentRmaId));
$(document).on("click", "#btnComplete", () => completeRma(currentRmaId));
