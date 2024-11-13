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
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using RestSharp;
using System.Security.Policy;

namespace Mioto.Controllers
{
    public class PaymentController : Controller
    {
        private readonly DB_MiotoEntities db = new DB_MiotoEntities();
        public bool IsLoggedIn => Session["KhachHang"] != null || Session["ChuXe"] != null;

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
            var kh = Session["KhachHang"] as KhachHang;
            var startDateTime = Session["StartDateTime"];
            var endDateTime = Session["EndDateTime"];

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
                db.Entry(discount).State = EntityState.Modified;
                db.SaveChanges();
            }

            // Tính tổng tiền với giảm giá
            decimal tongTien = bookingCar.TongTien;
            // Lưu đơn hàng vào cơ sở dữ liệu
            var donThueXe = new DonThueXe
            {
                IDKH = kh.IDKH,
                IDMGG = discount?.IDMGG,
                BienSo = bookingCar.Xe.BienSo,
                NgayThue = bookingCar.NgayThue,
                NgayTra = bookingCar.NgayTra,
                TGThanhToan = DateTime.Now,
                TrangThaiThanhToan = 0,
                PhanTramHoaHong = 10,
                TongTien = tongTien
            };
            Session["DonThueXe"] = donThueXe;
            Session["StartDateTime"] = startDateTime;
            Session["EndDateTime"] = endDateTime;
            db.DonThueXe.Add(donThueXe);
            db.SaveChanges();

            return RedirectToAction("QRPayment", "Payment");
        }

        [HttpPost]
        public JsonResult ApplyDiscount(string discountCode, decimal totalPrice)
        {
            var discount = db.MaGiamGia.FirstOrDefault(m => m.MaGG == discountCode && m.NgayKetThuc >= DateTime.Now);

            if (discount != null)
            {
                decimal discountPercentage = Convert.ToDecimal(discount.PhanTramGiam);
                decimal discountAmount = totalPrice * (discountPercentage / 100);

                decimal newTotalPrice = totalPrice - discountAmount;
                discount.SoLuong--;
                db.Entry(discount).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = true, newTotalPrice = newTotalPrice, discount = discount.PhanTramGiam }); 
            }
            else
            {
                return Json(new { success = false });
            }
        }

        public ActionResult QRPayment()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            var donthuexe = Session["DonThueXe"] as DonThueXe;
            var info = new Dictionary<string, string> 
            {
                {"BANK_ID", "MB" },
                {"ACCOUNT_NO", "0932175716" },
                {"OWNER", "Nguyen Viet Anh" }
            };
            var paidContent = $"MIOTO{donthuexe.IDTX.ToString()}";

            string QR = $"https://img.vietqr.io/image/{info["BANK_ID"]}-{info["ACCOUNT_NO"]}-compact2.png?amount={donthuexe.TongTien}&addInfo={paidContent}&accountName={Uri.EscapeDataString(info["OWNER"])}";
            ViewBag.QRCodeUrl = QR;
            Session["DonThueXe"] = donthuexe;
            return View();
        }

        public ActionResult CongratulationPaymentDone()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            var donthuexe = Session["DonThueXe"] as DonThueXe;

            var tokenFile = "C:\\Program Files\\IIS Express\\Json\\tokens.json";
            var tokens = JObject.Parse(System.IO.File.ReadAllText(tokenFile));

            RestClient restClient = new RestClient();
            RestRequest request = new RestRequest("https://www.googleapis.com/calendar/v3/calendars/primary/events", Method.Post);

            var formattedNgayThue = donthuexe.NgayThue.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
            var formattedNgayTra = donthuexe.NgayTra.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");

            var calendarEvent = new
            {
                summary = donthuexe.BienSo.ToString(),
                description = $"Booking for car ID: {donthuexe.IDTX}",
                start = new { dateTime = formattedNgayThue, timeZone = "UTC" },
                end = new { dateTime = formattedNgayTra, timeZone = "UTC" }
            };

            var model = JsonConvert.SerializeObject(calendarEvent, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            request.AddQueryParameter("key", "AIzaSyDs2PE3cSuieWJalZMbSmoiC0v1NefPvhU");
            request.AddHeader("Authorization", "Bearer " + tokens["access_token"]);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("application/json", model, ParameterType.RequestBody);

            var response = restClient.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var existingDonThueXe = db.DonThueXe.Find(donthuexe.IDTX);
                if (existingDonThueXe != null)
                {
                    existingDonThueXe.TrangThaiThanhToan = 1;
                    db.Entry(existingDonThueXe).State = EntityState.Modified;
                    db.SaveChanges();
                    return View();
                }
            }
            return View("Error");
        }
    }
}