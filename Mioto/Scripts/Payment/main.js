document.addEventListener("DOMContentLoaded", () => { });



document.addEventListener("DOMContentLoaded", () => {
    let carRenderUrl = "";

});

let my_bank = {
    BANK_ID: "TCB",
    ACCOUNT_NO: "19071843431017",
    ACCOUNT_NAME: "NGUYEN VIET ANH"
}

const btn = document.getElementById("btn_paid");
const paid_price = document.getElementById("GiaThue");
const number_car = document.getElementById("number_car");
const customerName = document.getElementById("customerName");
const customerID = document.getElementById("customerID");
const qrImage = document.querySelector(".course_qr_img");

btn.addEventListener("click", function () {
    // Lấy giá trị từ các thẻ input
    const price = paid_price.value;
    const carNumber = number_car.value;
    const name = customerName.value;
    const id = customerID.value;

    // Nội dung chuyển khoản
    const paid_content = `${id} ${name} ${carNumber}`;

    // Tạo URL QR code
    let QR = `https://img.vietqr.io/image/${my_bank.BANK_ID}-${my_bank.ACCOUNT_NO}-compact2.png?amount=${price}&addInfo=${paid_content}&accountName=${my_bank.ACCOUNT_NAME}`;

    // Cập nhật src của ảnh để hiển thị QR code
    qrImage.src = QR;
});


async function checkPaid(price, content) {
    try {
        const response = await fetch(
            "https://script.google.com/macros/s/AKfycbxqKrSsb2jqpFur0lGIX86-9nHkZ-OSKYLSRTtPVPv7CHGCBbe3ofKzDLWKXCslRMtT/exec"
        );
        const data = await response.json()
        const last_paid = data.data[data.data.length - 1];
        last_prices = last_paid["Giá trị"]
        last_content = last_paid["Mô tả"]
        if (last_prices >= price && last_content.includes(content)) {
            alert("Thanh toán thành công")
        }
        else {
            console.log("Không thành công")
        }
    }
    catch {
        console.error("Lỗi")
    }
}