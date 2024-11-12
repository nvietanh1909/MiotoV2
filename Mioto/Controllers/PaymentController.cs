using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Util.Store;
using Google;
using Mioto.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using Google.Apis.Calendar.v3.Data;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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

            var startDateTime = Session["StartDateTime"];
            var endDateTime = Session["EndDateTime"];
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
            Session["StartDateTime"] = startDateTime;
            Session["EndDateTime"] = endDateTime;
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

            var startDateTime = Session["StartDateTime"];
            var endDateTime = Session["EndDateTime"];
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
            Session["StartDateTime"] = startDateTime;
            Session["EndDateTime"] = endDateTime;
            return View(bookingCarModel);
        }

        public ActionResult OauthRedirect()
        {
            var credentialsFile = "C:\\Program Files\\IIS Express\\Json\\api_calendar.json";
            JObject credentials = JObject.Parse(System.IO.File.ReadAllText(credentialsFile));

            var client_id = credentials["web"]["client_id"].ToString();

            var redirectUrl = "https://accounts.google.com/o/oauth2/v2/auth?" +
                              "scope=https://www.googleapis.com/auth/calendar https://www.googleapis.com/auth/calendar.events&" +
                              "access_type=offline&" +
                              "include_granted_scopes=true&" +
                              "state=hellothere&" +
                              "redirect_uri=https://localhost:44380/oauth/callback&" +
                              "response_type=code&" +
                              "client_id=" + client_id;
            return Redirect(redirectUrl);
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

            var startDateTime = Session["StartDateTime"];
            var endDateTime = Session["EndDateTime"];

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
                TrangThaiThanhToan = 0,
                PhanTramHoaHong = 10,
                TongTien = discountedAmount
            };
            Session["StartDateTime"] = startDateTime;
            Session["EndDateTime"] = endDateTime;
            db.DonThueXe.Add(donThueXe);
            db.SaveChanges();

            return RedirectToAction("QRPayment", "Payment");
        }

        public ActionResult CongratulationPaymentDone()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            return View();
        }
        public ActionResult QRPayment()
        {
            return View();
        }

        [HttpPost]
        // Phương thức kiểm tra tính hợp lệ của ngày
        private bool IsValidRentalPeriod(DateTime ngayThue, DateTime ngayTra, string bienSo)
        {
            // Kiểm tra nếu ngày trả không được phép nhỏ hơn ngày thuê
            if (ngayTra <= ngayThue)
            {
                ModelState.AddModelError("", "Ngày trả phải sau ngày thuê.");
                return false;
            }

            // Kiểm tra nếu đã có xe thuê trong khoảng thời gian đó
            var existingBooking = db.DonThueXe
                .FirstOrDefault(d => d.BienSo == bienSo &&
                                     ((d.NgayThue < ngayTra && d.NgayTra > ngayThue) ||
                                      (d.NgayThue < ngayThue && d.NgayTra > ngayThue) ||
                                      (d.NgayThue < ngayTra && d.NgayTra > ngayTra)));

            if (existingBooking != null)
            {
                ModelState.AddModelError("", "Xe đã được thuê trong khoảng thời gian này.");
                return false;
            }

            return true;
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