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
                if (discount == null || discount.SoLuong <= 0 || discount.NgayKeyThuc < DateTime.Now)
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
                IDKH = bookingCar.KhachHang.IDKH,
                IDMGG = discount?.IDMGG ?? 0,
                BienSo = bookingCar.Xe.BienSo,
                NgayThue = bookingCar.NgayThue,
                NgayTra = bookingCar.NgayTra,
                TrangThaiThanhToan = 1,
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
        public JsonResult ApplyDiscount(string discountCode, decimal totalPrice)
        {
            var discount = db.MaGiamGia.FirstOrDefault(m => m.MaGG == discountCode && m.NgayKeyThuc >= DateTime.Now);

            if (discount != null)
            {
                discount.SoLuong--;
                db.Entry(discount).State = EntityState.Modified;
                db.SaveChanges();

                decimal discountAmount = totalPrice * (discount.PhanTramGiam / 100);
                decimal newTotalPrice = totalPrice - discountAmount;
                return Json(new { success = true, newTotalPrice = newTotalPrice });
            }
            else
            {
                return Json(new { success = false });
            }

        }
    }
}