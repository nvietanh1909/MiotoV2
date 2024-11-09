using Mioto.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace Mioto.Controllers
{
    public class PaymentController : Controller
    {
        private readonly DB_MiotoEntities db = new DB_MiotoEntities();
        public bool IsLoggedIn => Session["KhachHang"] != null || Session["ChuXe"] != null;

        public ActionResult InfoCar(string BienSoXe)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            var khachHang = Session["KhachHang"] as KhachHang;

            if (string.IsNullOrEmpty(BienSoXe))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var xe = db.Xe
                .Include(x => x.ChuXe.KhachHang)
                .FirstOrDefault(x => x.BienSo == BienSoXe);

            if (xe == null)
            {
                return HttpNotFound();
            }

            var donThueXe = db.DonThueXe.FirstOrDefault(d => d.BienSo == BienSoXe);
            var kt_kh_thuexe = db.DonThueXe.FirstOrDefault(x => x.IDKH == khachHang.IDKH);
            var danhGiaList = donThueXe != null
                ? db.DanhGia.Where(d => d.IDDT == donThueXe.IDTX).ToList()
                : new List<DanhGia>();

            MD_InfoCar model;
            if (kt_kh_thuexe == null)
            {
                model = new MD_InfoCar
                {
                    Xe = xe,
                    DonThueXe = donThueXe,
                    DanhGia = danhGiaList,
                    ChuXe = xe.ChuXe.KhachHang,
                    KhachHang = false
                };
            }
            else
            {
                model = new MD_InfoCar
                {
                    Xe = xe,
                    DonThueXe = donThueXe,
                    DanhGia = danhGiaList,
                    ChuXe = xe.ChuXe.KhachHang,
                    KhachHang = true
                };
            }

            ViewBag.ErrorMessage = TempData["ErrorMessage"];
            return View(model);
        }

        public ActionResult Alert()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            return View();
        }

        public ActionResult BookingCar(string BienSoXe)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            var khachHang = Session["KhachHang"] as KhachHang;
            var xe = db.Xe.FirstOrDefault(x => x.BienSo == BienSoXe);
            var chuXe = db.ChuXe.FirstOrDefault(x => x.IDCX == xe.IDCX);
            var _chuXe = db.KhachHang.FirstOrDefault(x => x.IDKH == chuXe.IDKH);

            if (xe == null || chuXe == null)
                return HttpNotFound();
            if (khachHang != null)
            {
                if (khachHang.CCCD == "No" || khachHang.GPLX == "No")
                {
                    return RedirectToAction("Alert", "Payment");
                }
            }
            else if (chuXe != null)
            {
                if (_chuXe.CCCD == "No" || _chuXe.GPLX == "No")
                {
                    return RedirectToAction("Alert", "Payment");
                }
            }
            var bookingCarModel = new MD_BookingCar
            {
                Xe = xe,
                KhachHang = _chuXe,
            };
            return View(bookingCarModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BookingCar(MD_BookingCar bookingCar)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            var khachHang = Session["KhachHang"] as KhachHang;
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                return View(bookingCar);
            }

            // Kiểm tra tính khả dụng của xe
            bool isAvailableXe = !db.DonThueXe.Any(d => d.BienSo == bookingCar.Xe.BienSo &&
                ((d.NgayThue < bookingCar.NgayTra && d.NgayTra > d.NgayThue) ||
                 (d.NgayThue < bookingCar.NgayThue && d.NgayTra > bookingCar.NgayThue) ||
                 (d.NgayThue < bookingCar.NgayTra && d.NgayTra > bookingCar.NgayTra)) && d.TrangThaiThanhToan == 1);

            if (!isAvailableXe)
            {
                ModelState.AddModelError("", "Xe không còn khả dụng trong khoảng thời gian này.");
                return View(bookingCar);
            }

            int soNgayThue = Math.Max((bookingCar.NgayTra - bookingCar.NgayThue).Days, 1);

            // Kiểm tra mã giảm giá
            decimal discountPercentage = 0;
            MaGiamGia discount = null;

            if (!string.IsNullOrEmpty(bookingCar.MaGiamGia?.MaGG))
            {
                discount = db.MaGiamGia.FirstOrDefault(m => m.MaGG == bookingCar.MaGiamGia.MaGG);
                if (discount == null || discount.SoLuong <= 0 || discount.NgayKetThuc < DateTime.Now)
                {
                    ModelState.AddModelError("", "Mã giảm giá không hợp lệ hoặc đã hết hạn.");
                    return View(bookingCar);
                }

                discountPercentage = discount.PhanTramGiam;
                discount.SoLuong--;
                db.Entry(discount).State = EntityState.Modified;
                db.SaveChanges();
            }

            // Tính tổng tiền với giảm giá
            decimal tongTien = bookingCar.Xe.GiaThue * soNgayThue;
            decimal discountedAmount = tongTien * (1 - discountPercentage / 100);

            // Lưu đơn hàng vào cơ sở dữ liệu
            var donThueXe = new DonThueXe
            {
                IDKH = khachHang.IDKH,
                IDMGG = discount?.IDMGG ?? 0,
                BienSo = bookingCar.Xe.BienSo,
                NgayThue = bookingCar.NgayThue,
                NgayTra = bookingCar.NgayTra,
                TrangThaiThanhToan = 0, // Chưa thanh toán
                PhanTramHoaHong = 10,
                TongTien = discountedAmount
            };

            db.DonThueXe.Add(donThueXe);
            db.SaveChanges();

            return RedirectToAction("CongratulationPaymentDone", "Payment");
        }

        public ActionResult CongratulationPaymentDone()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            return View();
        }


        [HttpPost]
        public JsonResult ApplyDiscount(string code, decimal SoTien)
        {
            var khachhang = Session["KhachHang"] as KhachHang;
            if (khachhang == null)
            {
                return Json(new { success = false, message = "Phiên làm việc không hợp lệ. Vui lòng đăng nhập lại." });
            }

            var donThueXe = db.DonThueXe.FirstOrDefault(t => t.IDKH == khachhang.IDKH);
            if (donThueXe == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn thuê xe cho khách hàng này." });
            }

            var discount = db.MaGiamGia.FirstOrDefault(m => m.MaGG == code);
            if (discount == null)
            {
                return Json(new { success = false, message = "Mã giảm giá không tồn tại." });
            }
            else if (discount.SoLuong <= 0)
            {
                return Json(new { success = false, message = "Mã giảm giá đã hết lần sử dụng." });
            }
            else if (discount.NgayKetThuc < DateTime.Now)
            {
                return Json(new { success = false, message = "Mã giảm giá đã hết hạn." });
            }
            else
            {
                var hasUsedCode = db.DonThueXe.Any(t => t.IDMGG == discount.IDMGG && t.IDTX == donThueXe.IDTX);
                if (hasUsedCode)
                {
                    return Json(new { success = false, message = "Bạn đã sử dụng mã giảm giá này." });
                }

                discount.SoLuong--;
                db.Entry(discount).State = EntityState.Modified;
                db.SaveChanges();

                var discountedAmount = SoTien - (SoTien * discount.PhanTramGiam / 100);
                if (discountedAmount < 0) discountedAmount = 0;

                return Json(new { success = true, discountedAmount = discountedAmount.ToString("N0") });
            }
        }
    }
}