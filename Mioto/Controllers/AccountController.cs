using Mioto.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using BCrypt.Net;
using System.Web.Security;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Mioto.Controllers
{
    public class AccountController : Controller
    {
        DB_MiotoEntities db = new DB_MiotoEntities();
        List<SelectListItem> gioitinh = new List<SelectListItem>
        {
         new SelectListItem { Text = "Nam", Value = "Nam" },
         new SelectListItem { Text = "Nữ", Value = "Nữ" }
        };

        // GET: Account/Login
        public ActionResult Login()
        {
            if (Session["KhachHang"] != null || Session["NhanVien"] != null)
                return Logout();
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(MD_Login _user)
        {
            var IsGuest = db.KhachHang.SingleOrDefault(s => s.Email == _user.Email && s.MatKhau == _user.MatKhau);
            var IsNhanVien = db.NhanVien
                .SingleOrDefault(s => s.Email == _user.Email
                                      && s.MatKhau == _user.MatKhau
                                      && s.ChucVu.Equals("Nhân viên", StringComparison.OrdinalIgnoreCase));
            var IsQuanLy = db.NhanVien
                .SingleOrDefault(s => s.Email == _user.Email
                                      && s.MatKhau == _user.MatKhau
                                      && s.ChucVu.Equals("Quản lý", StringComparison.OrdinalIgnoreCase));

            // Khách hàng
            if (IsGuest != null)
            {
                var IsChuXe = db.ChuXe.SingleOrDefault(x => x.IDKH == IsGuest.IDKH);
                if(IsChuXe != null)
                {
                    Session["KhachHang"] = IsGuest;
                    Session["ChuXe"] = IsChuXe;
                    Session["NhanVien"] = IsNhanVien;
                    Session["QuanLy"] = IsQuanLy;
                    return RedirectToAction("Home", "Home");
                }
                Session["KhachHang"] = IsGuest;
                return RedirectToAction("Home", "Home");
            }

            // Nhân viên
            if(IsNhanVien != null)
            {
                Session["NhanVien"] = IsNhanVien;
                return RedirectToAction("Home", "Home");
            }

            // Quản lý
            if (IsQuanLy != null)
            {
                Session["QuanLy"] = IsQuanLy;
                return RedirectToAction("Home", "Home");
            }

            ViewBag.ErrorLogin = "Email hoặc mật khẩu không chính xác";
            return View();
        }

        // GET: Account/Register
        public ActionResult Register()
        {
            ViewBag.GioiTinh = gioitinh;
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(MD_Register kh)
        {
            ViewBag.GioiTinh = gioitinh;
            try
            {
                if (ModelState.IsValid)
                {
                    if (db.KhachHang.Any(x => x.Email == kh.Email))
                    {
                        ModelState.AddModelError("Email", "Email đã tồn tại. Vui lòng sử dụng email khác.");
                        return View(kh);
                    }

                    var otp = new Random().Next(100000, 999999).ToString();
                    Session["OTP"] = otp; 
                    Session["RegisterInfo"] = kh;

                    SendOtpEmail(kh.Email, otp);

                    return RedirectToAction("VerifyOtp");
                }
                return View(kh);
            }
            catch
            {
                ViewBag.ErrorRegister = "Đăng ký không thành công. Vui lòng thử lại.";
                return View(kh);
            }
        }

        private void SendOtpEmail(string email, string otp)
        {
            var fromAddress = new MailAddress("nvietanh.work.1909@gmail.com", "Mioto");
            var toAddress = new MailAddress(email);
            const string fromPassword = "xxdo zayw fupq zjcb"; 
            const string subject = "Mã xác thực đăng ký tài khoản";
            string body = $"Mã xác thực của bạn là: {otp}";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }
        }

        public ActionResult VerifyOtp()
        {
            

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyOtp(string otp)
        {
            var sessionOtp = Session["OTP"]?.ToString();
            var registerInfo = Session["RegisterInfo"] as MD_Register;

            if (otp == sessionOtp && registerInfo != null)
            {
                var newKhachHang = new KhachHang
                {
                    Ten = registerInfo.Ten,
                    Email = registerInfo.Email,
                    GioiTinh = registerInfo.GioiTinh,
                    DiaChi = registerInfo.DiaChi,
                    SDT = registerInfo.SDT,
                    GPLX = "0",
                    NgaySinh = registerInfo.NgaySinh,
                    MatKhau = registerInfo.MatKhau,
                    CCCD = "0"
                };
                db.KhachHang.Add(newKhachHang);
                db.SaveChanges();

                TempData["Message"] = "Đăng ký thành công!";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("OTP", "Mã xác thực không đúng.");
            return View();
        }

        //GET : Home/Logout
        public ActionResult Logout()
        {
            Session.Abandon();
            FormsAuthentication.SignOut();
            return RedirectToAction("Home");
        }
    }

}

