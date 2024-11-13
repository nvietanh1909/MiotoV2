using Mioto.Models;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Mioto.Controllers
{
    public class DetailAccountController : Controller
    {
        DB_MiotoEntities db = new DB_MiotoEntities();
        public bool IsLoggedIn { get => Session["KhachHang"] != null || Session["ChuXe"] != null; }
        private static readonly HttpClient client = new HttpClient();

        List<SelectListItem> tinhThanhPho = new List<SelectListItem>
        {
            new SelectListItem { Text = "TP Hồ Chí Minh", Value = "TP Hồ Chí Minh" },
            new SelectListItem { Text = "Hà Nội", Value = "Hà Nội" },
            new SelectListItem { Text = "Đà Nẵng", Value = "Đà Nẵng" },
            new SelectListItem { Text = "Bình Dương", Value = "Bình Dương" },
            new SelectListItem { Text = "Cần Thơ", Value = "Cần Thơ" },
            new SelectListItem { Text = "Đà Lạt", Value = "Đà Lạt" },
            new SelectListItem { Text = "Nha Trang", Value = "Nha Trang" },
            new SelectListItem { Text = "Quy Nhơn", Value = "Quy Nhơn" },
            new SelectListItem { Text = "Phú Quốc", Value = "Phú Quốc" },
            new SelectListItem { Text = "Hải Phòng", Value = "Hải Phòng" },
            new SelectListItem { Text = "Vũng Tàu", Value = "Vũng Tàu" },
            new SelectListItem { Text = "Thành phố khác", Value = "Thành phố khác" },
        };

        List<SelectListItem> gioitinh = new List<SelectListItem>
        {
         new SelectListItem { Text = "Nam", Value = "Nam" },
         new SelectListItem { Text = "Nữ", Value = "Nữ" }
        };

        List<SelectListItem> TrangThaiXe = new List<SelectListItem>
        {
         new SelectListItem { Text = "Sẵn sàng", Value = "Sẵn sàng" },
         new SelectListItem { Text = "Bảo trì", Value = "Bảo trì" },
         new SelectListItem { Text = "Ngưng cho thuê", Value = "Ngưng cho thuê" }
        };

        // GET: DetailAccount
        public ActionResult InfoAccount()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            var guest = Session["KhachHang"] as KhachHang;
            if (guest == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Khách hàng không tồn tại");
            var kh = db.KhachHang.Where(x => x.IDKH == guest.IDKH);
            return View(kh);
        }

        // GET: EditInfoUser/InfoAccount
        public ActionResult EditInfoUser(int IDKH)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            var id = db.KhachHang.FirstOrDefault(x => x.IDKH == IDKH);
            ViewBag.GioiTinh = gioitinh;
            return View(id);
        }

        // POST: EditInfoUser/InfoAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditInfoUser(KhachHang kh)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Home");
            try
            {
                if (ModelState.IsValid)
                {
                    var guest = Session["KhachHang"] as KhachHang;
                    var chuxe = Session["ChuXe"] as ChuXe;
                    kh.GPLX = guest.GPLX;
                    kh.MatKhau = guest.MatKhau;
                    kh.IDKH = kh.IDKH;
                    kh.CCCD = guest.CCCD;
                    kh.HinhAnh = guest.HinhAnh;
                    db.Entry(kh).State = EntityState.Modified;
                    db.SaveChanges();

                    if (chuxe != null)
                    {
                        var newChuXe = new ChuXe
                        {
                            IDCX = chuxe.IDCX,
                            IDKH = kh.IDKH,
                            HinhAnh = guest.HinhAnh,
                        };
                        db.Entry(newChuXe).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    guest = db.KhachHang.FirstOrDefault(x => x.IDKH == guest.IDKH);
                    chuxe = db.ChuXe.FirstOrDefault(x => x.IDCX == guest.IDKH);

                    // Cập nhật lại Session
                    Session["KhachHang"] = guest;
                    Session["ChuXe"] = chuxe;
                    return RedirectToAction("InfoAccount");
                }
                return View(kh);
            }
            catch
            {
                return View(kh);
            }
        }
        
        [HttpPost]
        public ActionResult ChangeAvatarUser(HttpPostedFileBase avatar)
        {
            if (avatar != null && avatar.ContentLength > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(avatar.FileName).ToLower();

                if (allowedExtensions.Contains(extension))
                {
                    var fileName = Guid.NewGuid().ToString() + extension;

                    var path = Path.Combine(Server.MapPath("~/AvatarUser/"), fileName);

                    avatar.SaveAs(path);

                    var guest = Session["KhachHang"] as KhachHang;
                    var existingKH = db.KhachHang.Find(guest.IDKH);
                    var existingCX = db.ChuXe.FirstOrDefault(x => x.IDKH == guest.IDKH);

                    if (existingKH != null)
                    {
                        existingKH.HinhAnh = fileName;

                        if (existingCX != null)
                        {
                            existingCX.HinhAnh = fileName;
                            db.Entry(existingCX).State = EntityState.Modified;
                            Session["ChuXe"] = existingCX;
                        }
                        Session["KhachHang"] = existingKH;
                        db.Entry(existingKH).State = EntityState.Modified;
                        db.SaveChanges();
                    }

                    return RedirectToAction("InfoAccount");
                }
                else
                {
                    ModelState.AddModelError("", "Định dạng file không hợp lệ. Vui lòng chọn một file ảnh.");
                }
            }
            else
            {
                ModelState.AddModelError("", "Vui lòng chọn một file ảnh.");
            }

            return View("InfoAccount");
        }

        // GET: Detailt/MyCar
        public ActionResult MyCar()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            var guest = Session["KhachHang"] as KhachHang;
            var chuxe = Session["ChuXe"] as ChuXe;
            if (guest == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Khách hàng không tồn tại");

            if (chuxe != null)
            {
                var cars = db.Xe.Where(x => x.IDCX == chuxe.IDCX).ToList();
                return View(cars);
            }
            return View();
        }

        // GET: EditCar/MyCar
        public ActionResult EditCar(string BienSoXe)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            ViewBag.TinhThanhPho = tinhThanhPho;
            ViewBag.TrangThaiXe = TrangThaiXe;
            if (String.IsNullOrEmpty(BienSoXe))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var xe = db.Xe.FirstOrDefault(x => x.BienSo == BienSoXe);
            if (xe == null)
            {
                return HttpNotFound();
            }
            return View(xe);
        }

        // POST: EditCar/MyCar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCar(Xe xe)
        {
            if (!IsLoggedIn)
            {
                return RedirectToAction("Login", "Account");
            }
            ViewBag.TinhThanhPho = tinhThanhPho;
            try
            {
                if (ModelState.IsValid)
                {
                    var guest = Session["ChuXe"] as ChuXe;
                    xe.IDCX = guest.IDCX;
                    db.Entry(xe).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("MyCar");
                }
                return View(xe);
            }
            catch
            {
                return View(xe);
            }
        }

        public ActionResult MyTrip()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            var guest = Session["KhachHang"] as KhachHang;
            var chuxe = Session["ChuXe"] as ChuXe;

            if (guest == null && chuxe == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Khách hàng hoặc chủ xe không tồn tại");

            List<MD_MyTrip> myTrips = new List<MD_MyTrip>();

            if (guest != null)
            {
                myTrips = db.DonThueXe
                    .Where(x => x.IDKH == guest.IDKH)
                    .Select(dtx => new MD_MyTrip
                    {
                        IDDT = dtx.IDTX,
                        BienSoXe = dtx.BienSo,
                        NgayThue = dtx.NgayThue,
                        NgayTra = dtx.NgayTra,
                        TongTien = dtx.TongTien,
                        TrangThai = dtx.TrangThaiThanhToan,
                        ChuXe = db.ChuXe.FirstOrDefault(cx => cx.IDCX == dtx.Xe.IDCX),
                    }).ToList();
            }

            if (chuxe != null)
            {
                myTrips = db.DonThueXe
                    .Where(x => x.IDKH == chuxe.IDKH)
                    .Select(dtx => new MD_MyTrip
                    {
                        IDDT = dtx.IDTX,
                        BienSoXe = dtx.BienSo,
                        NgayThue = dtx.NgayThue,
                        NgayTra = dtx.NgayTra,
                        TrangThai = dtx.TrangThaiThanhToan,
                        TongTien = dtx.TongTien,
                        ChuXe = db.ChuXe.FirstOrDefault(cx => cx.IDCX == dtx.Xe.IDCX)
                    }).ToList();
            }
            return View(myTrips);
        }

        public ActionResult LongTermOrder()
        {
            return View();
        }
        public ActionResult RequestDeleteAccount()
        {
            return View();
        }
        public ActionResult MyAddress()
        {
            return View();
        }

        public ActionResult ChangePassword()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            return View();
        }

        public ActionResult Logout()
        {
            // Xóa tất cả các session của người dùng
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(MD_ChangePassword model)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid)
                return View(model);

            var guest = Session["KhachHang"] as KhachHang;
            var existingKH = db.KhachHang.Find(guest.IDKH);
            var existingCX = db.ChuXe.Find(guest.IDKH);

            if (guest == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra mật khẩu cũ
            if (VerifyPassword(guest, model.OldPassword))
            {
                if (existingCX == null)
                {
                    // Cập nhật mật khẩu mới cho Khách hàng
                    existingKH.MatKhau = model.NewPassword;
                    db.Entry(existingKH).State = EntityState.Modified;
                    db.SaveChanges();
                    Session["KhachHang"] = existingKH;
                }
                else
                {
                    // Cập nhật mật khẩu mới cho Khách hàng
                    existingKH.MatKhau = model.NewPassword;
                    db.Entry(existingKH).State = EntityState.Modified;
                    db.SaveChanges();
                    Session["KhachHang"] = existingKH;
                    // Cập nhật mật khẩu mới cho Chủ xe
                    Session["ChuXe"] = existingCX;
                }
                return RedirectToAction("InfoAccount");
            }
            else
            {
                // Mật khẩu cũ không đúng
                ViewBag.ErrorMessage = "Mật khẩu hiện tại không đúng. Vui lòng thử lại.";
                return View(model);
            }
        }

        private bool VerifyPassword(KhachHang user, string password)
        {
            return user.MatKhau == password;
        }

        // GET: RentedCar
        public ActionResult RentedCar()
        {
            // Kiểm tra người dùng đã đăng nhập
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            var chuxe = Session["ChuXe"] as ChuXe;
            var khachhang = Session["KhachHang"] as KhachHang;

            if (chuxe != null)
            {
                // Lấy danh sách các xe đang được thuê của chủ xe
                var rentedCars = db.DonThueXe
                    .Where(d => d.Xe.IDCX == chuxe.IDCX)
                    .Select(d => new MD_RentedCar
                    {
                        IDDT = d.IDTX,
                        BienSoXe = d.BienSo,
                        HangXe = d.Xe.HangXe,
                        MauXe = d.Xe.Mau,
                        NgayThue = d.NgayThue,
                        NgayTra = d.NgayTra,
                        TongTien = d.TongTien,
                        TrangThai = d.TrangThaiThanhToan,
                    }).ToList();

                return View(rentedCars);
            }
            return View();
        }

        public ActionResult RevenueChart()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            var chuxe = Session["ChuXe"] as ChuXe;
            var khachhang = Session["KhachHang"] as KhachHang;
            if (chuxe == null)
            {
                var view_kh = new DoanhThu
                {
                    DoanhThuNgay = 0,
                    DoanhThuTuan = 0,
                    DoanhThuThang = 0,
                    DoanhThuNam = 0
                };
                return View(view_kh);
            }

            var doanhThu = db.DoanhThu.FirstOrDefault(x => x.IDCX == chuxe.IDCX);
            if (doanhThu == null)
            {
                doanhThu = new DoanhThu
                {
                    DoanhThuNgay = 0,
                    DoanhThuTuan = 0,
                    DoanhThuThang = 0,
                    DoanhThuNam = 0
                };
            }

            var viewModel = new DoanhThu
            {
                DoanhThuNgay = doanhThu.DoanhThuNgay,
                DoanhThuTuan = doanhThu.DoanhThuTuan,
                DoanhThuThang = doanhThu.DoanhThuThang,
                DoanhThuNam = doanhThu.DoanhThuNam
            };

            return View(viewModel);
        }

        public ActionResult Gift()
        {
            return View();
        }

        public ActionResult DeleteRentedCar(int id)
        {
            // Kiểm tra người dùng đã đăng nhập
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            // Lấy thông tin chuyến thuê xe
            var donThueXe = db.DonThueXe.FirstOrDefault(x => x.IDTX == id);
            if (donThueXe == null)
                return HttpNotFound("Chuyến thuê không tồn tại");

            var currentDateTime = DateTime.Now;
            var bookingDateTime = donThueXe.TGThanhToan;
            var timeDifference = (currentDateTime - bookingDateTime).TotalDays;

            // Xác định người dùng hủy chuyến
            var guest = Session["KhachHang"] as KhachHang;
            var chuxe = Session["ChuXe"] as ChuXe;

            decimal denTien = 0;

            if (chuxe != null)
            {
                // Chủ xe hủy chuyến
                if (currentDateTime <= bookingDateTime.AddHours(1))
                {
                    // <= 1 giờ sau giữ chỗ
                    denTien = 0; // Không mất phí
                }
                else if (timeDifference > 7)
                {
                    // Hủy trước chuyến đi > 7 ngày
                    denTien = donThueXe.TongTien * 30 / 100; // Đền tiền 30%
                }
                else
                {
                    // Hủy <= 7 ngày trước chuyến đi
                    denTien = donThueXe.TongTien; // Đền tiền 100%
                }

                // Lưu thông tin phí hủy chuyến cho chủ xe
                var phiHuyChuyen = new PhiHuyChuyen
                {
                    IDDT = id,
                    LoaiHuyChuyen = 2, // Chủ xe
                    SoTienHoan = denTien,
                    ThoiGianHuy = DateTime.Now,
                    MoTa = "Hủy chuyến do chủ xe yêu cầu."
                };

                db.PhiHuyChuyen.Add(phiHuyChuyen);
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Người dùng không hợp lệ");
            }

            var tokenFile = "C:\\Program Files\\IIS Express\\Json\\tokens.json";
            var tokens = JObject.Parse(System.IO.File.ReadAllText(tokenFile));

            RestClient restClient = new RestClient();
            RestRequest request = new RestRequest("https://www.googleapis.com/calendar/v3/calendars/primary/events", Method.Get);

            request.AddQueryParameter("key", "AIzaSyDs2PE3cSuieWJalZMbSmoiC0v1NefPvhU");
            request.AddHeader("Authorization", "Bearer " + tokens["access_token"]);
            request.AddHeader("Accept", "application/json");

            var response = restClient.Execute(request);
            string identify = "";

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                JObject calendarEvents = JObject.Parse(response.Content);
                var allEvents = calendarEvents["items"]
                    .Where(eventItem => eventItem["start"]?["dateTime"] != null && eventItem["end"]?["dateTime"] != null)
                    .ToList()
                    .Select(eventItem => eventItem.ToObject<Mioto.Models.Events>())
                    .ToList();

                foreach (var item in allEvents)
                {
                    if (item.Summary == donThueXe.BienSo)
                    {
                        identify = item.Id;
                        break;
                    }
                }
            }

            // Xóa sự kiện trên Google Calendar
            var _tokenFile = "C:\\Program Files\\IIS Express\\Json\\tokens.json";
            var _tokens = JObject.Parse(System.IO.File.ReadAllText(_tokenFile));
            RestClient _restClient = new RestClient("https://www.googleapis.com/calendar/v3/calendars/primary/events/" + identify);
            RestRequest _request = new RestRequest();
            _request.AddQueryParameter("key", "AIzaSyDs2PE3cSuieWJalZMbSmoiC0v1NefPvhU");
            _request.AddHeader("Authorization", "Bearer " + _tokens["access_token"]);
            _request.AddHeader("Accept", "application/json");

            var _response = _restClient.Delete(_request);

            if (_response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Lỗi khi xóa sự kiện trên Google Calendar");
            }

            // Cập nhật trạng thái thanh toán sau khi hủy chuyến
            donThueXe.TrangThaiThanhToan = 2; // Đánh dấu là đã hủy
            db.Entry(donThueXe).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("RentedCar");
        }

        public ActionResult DeleteTrip(int id)
        {
            // Kiểm tra người dùng đã đăng nhập
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            // Lấy thông tin chuyến thuê xe
            var donThueXe = db.DonThueXe.FirstOrDefault(x => x.IDTX == id);
            if (donThueXe == null)
                return HttpNotFound("Chuyến thuê không tồn tại");

            var currentDateTime = DateTime.Now;
            var bookingDateTime = donThueXe.TGThanhToan;
            var timeDifference = (currentDateTime - donThueXe.TGThanhToan).TotalDays;

            // Xác định người dùng hủy chuyến
            var guest = Session["KhachHang"] as KhachHang;

            decimal hoanTien = 0;
            if (guest != null)
            {
                // Khách hàng hủy chuyến
                if (currentDateTime <= bookingDateTime.AddHours(1))
                {
                    // <= 1 giờ sau giữ chỗ
                    hoanTien = donThueXe.TongTien; // Hoàn tiền 100%
                }
                else if (timeDifference > 7)
                {
                    // Hủy trước chuyến đi > 7 ngày
                    hoanTien = donThueXe.TongTien * 70 / 100; // Hoàn tiền 70%
                }
                else
                {
                    // Hủy <= 7 ngày trước chuyến đi
                    hoanTien = 0; // Không hoàn tiền
                }

                // Lưu thông tin phí hủy chuyến cho khách hàng
                var phiHuyChuyen = new PhiHuyChuyen
                {
                    IDDT = id,
                    LoaiHuyChuyen = 1, // Khách hàng
                    ThoiGianHuy = DateTime.Now,
                    SoTienHoan = hoanTien,
                    MoTa = "Hủy chuyến do khách hàng yêu cầu."
                };

                db.PhiHuyChuyen.Add(phiHuyChuyen);
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Người dùng không hợp lệ");
            }

            // Lấy access_token từ file
            var tokenFile = "C:\\Program Files\\IIS Express\\Json\\tokens.json";
            var tokens = JObject.Parse(System.IO.File.ReadAllText(tokenFile));

            RestClient restClient = new RestClient();
            RestRequest request = new RestRequest("https://www.googleapis.com/calendar/v3/calendars/primary/events", Method.Get);

            request.AddQueryParameter("key", "AIzaSyDs2PE3cSuieWJalZMbSmoiC0v1NefPvhU");
            request.AddHeader("Authorization", "Bearer " + tokens["access_token"]);
            request.AddHeader("Accept", "application/json");

            var response = restClient.Execute(request);
            string identify = "";

            // Tìm sự kiện trên Google Calendar
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                JObject calendarEvents = JObject.Parse(response.Content);
                var allEvents = calendarEvents["items"]
                    .Where(eventItem => eventItem["start"]?["dateTime"] != null && eventItem["end"]?["dateTime"] != null)
                    .ToList()
                    .Select(eventItem => eventItem.ToObject<Mioto.Models.Events>())
                    .ToList();

                foreach (var item in allEvents)
                {
                    if (item.Summary == donThueXe.BienSo)
                    {
                        identify = item.Id;
                        break;
                    }
                }
            }

            // Nếu tìm thấy sự kiện, xóa sự kiện trên Google Calendar
            if (!string.IsNullOrEmpty(identify))
            {
                RestClient _restClient = new RestClient("https://www.googleapis.com/calendar/v3/calendars/primary/events/" + identify);
                RestRequest _request = new RestRequest();
                _request.AddQueryParameter("key", "AIzaSyDs2PE3cSuieWJalZMbSmoiC0v1NefPvhU");
                _request.AddHeader("Authorization", "Bearer " + tokens["access_token"]);
                _request.AddHeader("Accept", "application/json");

                var _response = _restClient.Delete(_request);

                if (_response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Lỗi khi xóa sự kiện trên Google Calendar");
                }
            }

            // Cập nhật trạng thái thanh toán sau khi hủy chuyến
            donThueXe.TrangThaiThanhToan = 2; // Đánh dấu là đã hủy
            db.Entry(donThueXe).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("MyTrip");
        }
    }
}