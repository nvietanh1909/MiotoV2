using Mioto.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;

namespace Mioto.Controllers
{
    public class AdminController : Controller
    {

        DB_MiotoEntities db = new DB_MiotoEntities();
        public bool IsLoggedIn { get => Session["NhanVien"] != null || Session["QuanLy"] != null; }

        List<SelectListItem> gioitinh = new List<SelectListItem>
        {
         new SelectListItem { Text = "Nam", Value = "Nam" },
         new SelectListItem { Text = "Nữ", Value = "Nữ" }
        };

        List<SelectListItem> chucvu = new List<SelectListItem>
        {
         new SelectListItem { Text = "Nhân Viên", Value = "Nhân viên" },
         new SelectListItem { Text = "Quản lý", Value = "Quản lý" }
        };
        // GET: Admin
        public ActionResult AdminPaymentVerification()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            var ds_thanhtoan = db.DonThueXe.ToList();
            return View(ds_thanhtoan);
        }


        // GET: DetailAccount
        public ActionResult InfoAccount()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            var nhanvien = Session["NhanVien"] as NhanVien;
            var quanly = Session["QuanLy"] as NhanVien;
            
            if(nhanvien != null)
            {
                var nv = db.NhanVien.FirstOrDefault(x => x.IDNV == nhanvien.IDNV);
                Session["NhanVien"] = nhanvien;
                return View(nhanvien);
            }

            if (quanly != null)
            {
                var ql = db.NhanVien.FirstOrDefault(x => x.IDNV == quanly.IDNV);
                Session["QuanLy"] = quanly;
                return View(quanly);
            }
            return View();
        }

        // Thay đổi avatar user
        [HttpPost]
        public ActionResult ChangeAvatarUser(HttpPostedFileBase avatar)
        {
            // Kiểm tra xem file có tồn tại không
            if (avatar != null && avatar.ContentLength > 0)
            {
                // Kiểm tra định dạng file (chỉ cho phép các định dạng ảnh phổ biến)
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(avatar.FileName).ToLower();

                if (allowedExtensions.Contains(extension))
                {
                    // Tạo tên file duy nhất để tránh trùng lặp
                    var fileName = Guid.NewGuid().ToString() + extension;

                    // Đường dẫn lưu file
                    var path = Path.Combine(Server.MapPath("~/AvatarAdmin/"), fileName);

                    // Lưu file lên server
                    avatar.SaveAs(path);

                    // Cập nhật thông tin hình ảnh của nhân viên trong cơ sở dữ liệu
                    var guest = Session["NhanVien"] as NhanVien;
                    var existingNV = db.NhanVien.Find(guest.IDNV);

                    if (existingNV != null)
                    {
                        existingNV.HinhAnh = fileName;
                        // Cập nhật cơ sở dữ liệu
                        Session["NhanVien"] = existingNV;
                        db.Entry(existingNV).State = EntityState.Modified;
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

            // Trả về view với các lỗi nếu có
            return View("InfoAccount");
        }

        public ActionResult ManagerDiscount()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            var mgg = db.MaGiamGia.ToList();
            return View(mgg);
        }

        // GET: Discount/AddDiscount
        [HttpGet]
        public ActionResult AddDiscount()
        {
            return View();
        }

        // POST: Discount/AddDiscount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddDiscount(MaGiamGia model)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra điều kiện ngày bắt đầu và ngày kết thúc
                    if (model.NgayBatDau >= model.NgayKetThuc)
                    {
                        ModelState.AddModelError("", "Ngày kết thúc phải lớn hơn ngày bắt đầu.");
                        return View(model);
                    }

                    var newDiscount = new MaGiamGia
                    {
                        MaGG = model.MaGG,
                        PhanTramGiam = model.PhanTramGiam,
                        NgayBatDau = model.NgayBatDau,
                        NgayKetThuc = model.NgayKetThuc,
                        SoLuong = model.SoLuong
                    };

                    db.MaGiamGia.Add(newDiscount);
                    db.SaveChanges();

                    return RedirectToAction("ManagerDiscount");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Xảy ra lỗi khi thêm mã giảm giá: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: Discount/EditDiscount/5
        [HttpGet]
        public ActionResult EditDiscount(int idmgg)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            // Tìm mã giảm giá theo id
            var discount = db.MaGiamGia.Find(idmgg);
            if (discount == null)
            {
                return HttpNotFound();
            }

            return View(discount);
        }

        // POST: Discount/EditDiscount/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditDiscount(MaGiamGia model)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra điều kiện ngày
                    if (model.NgayBatDau >= model.NgayKetThuc)
                    {
                        ModelState.AddModelError("", "Ngày kết thúc phải lớn hơn ngày bắt đầu.");
                        return View(model);
                    }

                    // Tìm mã giảm giá theo id
                    var existingDiscount = db.MaGiamGia.Find(model.IDMGG);
                    if (existingDiscount == null)
                    {
                        return HttpNotFound();
                    }

                    // Cập nhật các thuộc tính của mã giảm giá
                    existingDiscount.MaGG = model.MaGG;
                    existingDiscount.PhanTramGiam = model.PhanTramGiam;
                    existingDiscount.NgayBatDau = model.NgayBatDau;
                    existingDiscount.NgayKetThuc = model.NgayKetThuc;
                    existingDiscount.SoLuong = model.SoLuong;

                    db.Entry(existingDiscount).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("ManagerDiscount");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Xảy ra lỗi khi cập nhật mã giảm giá: " + ex.Message);
                }
            }
            return View(model);
        }

        public ActionResult DeleteDiscount(int idmgg)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            try
            {
                var discount = db.MaGiamGia.Find(idmgg);
                if (discount == null)
                {
                    return HttpNotFound();
                }
                db.MaGiamGia.Remove(discount);
                db.SaveChanges();

                return RedirectToAction("ManagerDiscount");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Xảy ra lỗi khi xóa mã giảm giá: " + ex.Message);
                return RedirectToAction("ManagerDiscount");
            }
        }

        public ActionResult ListCarsInSystem()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            var xe = db.Xe.ToList();
            return View();
        }

        public ActionResult RentedCompany()
        {
            // Kiểm tra người dùng đã đăng nhập
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            // Lấy ID từ Session của nhân viên đang đăng nhập hiện tại
            var nhanvien = Session["NhanVien"] as NhanVien;
            var quanly = Session["QuanLy"] as NhanVien;

            if (nhanvien == null) nhanvien = quanly;

            if (nhanvien == null && quanly == null)
            {
                ViewBag.DoanhThuNgay = 0;
                ViewBag.DoanhThuTuan = 0;
                ViewBag.DoanhThuThang = 0;
                ViewBag.DoanhThuNam = 0;
                return View();
            }

            var donthuexe = db.DonThueXe
              .Where(x => x.TrangThaiThanhToan == 1)
              .ToList();

            var currentDate = DateTime.Now;

            // Tính doanh thu theo ngày
            decimal doanhThuNgay = donthuexe
                .Where(x => x.TGThanhToan.Date == currentDate.Date)
                .Sum(x => x.TongTien * x.PhanTramHoaHong/100);

            // Tính doanh thu theo tuần
            var startOfWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek);
            decimal doanhThuTuan = donthuexe
                .Where(x => x.TGThanhToan >= startOfWeek && x.TGThanhToan <= currentDate)
                .Sum(x => x.TongTien * x.PhanTramHoaHong / 100);

            // Tính doanh thu theo tháng
            decimal doanhThuThang = donthuexe
                .Where(x => x.TGThanhToan.Year == currentDate.Year &&
                            x.TGThanhToan.Month == currentDate.Month)
                .Sum(x => x.TongTien * x.PhanTramHoaHong / 100);

            // Tính doanh thu theo năm
            decimal doanhThuNam = donthuexe
                .Where(x => x.TGThanhToan.Year == currentDate.Year)
                .Sum(x => x.TongTien * x.PhanTramHoaHong / 100);

            ViewBag.DoanhThuNgay = doanhThuNgay;
            ViewBag.DoanhThuTuan = doanhThuTuan;
            ViewBag.DoanhThuThang = doanhThuThang;
            ViewBag.DoanhThuNam = doanhThuNam;

            // Kiểm tra nếu doanh thu của công ty đã tồn tại
            var doanhthu = db.DoanhThu.FirstOrDefault(x => x.IDCX == null); // Để trống hoặc đặc biệt cho công ty

            if (doanhthu == null)
            {
                // Tạo mới doanh thu nếu chưa tồn tại
                var newDoanhThu = new DoanhThu
                {
                    DoanhThuNgay = doanhThuNgay,
                    DoanhThuTuan = doanhThuTuan,
                    DoanhThuThang = doanhThuThang,
                    DoanhThuNam = doanhThuNam,
                    NgayCapNhat = DateTime.Now,
                    IDNV = nhanvien.IDNV
                };
                db.DoanhThu.Add(newDoanhThu);
            }
            else
            {
                // Cập nhật doanh thu nếu đã tồn tại
                doanhthu.DoanhThuNgay = doanhThuNgay;
                doanhthu.DoanhThuTuan = doanhThuTuan;
                doanhthu.DoanhThuThang = doanhThuThang;
                doanhthu.DoanhThuNam = doanhThuNam;
                doanhthu.NgayCapNhat = DateTime.Now;
                doanhthu.IDNV = nhanvien.IDNV;

                db.Entry(doanhthu).State = EntityState.Modified;
            }

            db.SaveChanges();
            return View();
        }

        public ActionResult ManagerEmployee()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            return View(db.NhanVien.ToList());
        }

        public ActionResult CreateEmployee()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            ViewBag.GioiTinh = gioitinh;
            ViewBag.ChucVu = chucvu;
            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateEmployee([Bind(Include = "IDNV,Ten,Email,DiaChi,NgaySinh,HinhAnh,ChucVu,GioiTinh,SDT,MatKhau")] NhanVien nhanVien)
        {
            if (ModelState.IsValid)
            {
                db.NhanVien.Add(nhanVien);
                db.SaveChanges();
                return RedirectToAction("ManagerEmployee");
            }

            return View(nhanVien);
        }

        // GET: NhanVien/Edit/5
        public ActionResult EditEmployee(int? id)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NhanVien nhanVien = db.NhanVien.Find(id);
            if (nhanVien == null)
            {
                return HttpNotFound();
            }
            ViewBag.GioiTinh = gioitinh;
            ViewBag.ChucVu = chucvu;
            return View(nhanVien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditEmployee([Bind(Include = "IDNV,Ten,Email,DiaChi,NgaySinh,HinhAnh,ChucVu,GioiTinh,SDT,MatKhau")] NhanVien nhanVien)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            if (ModelState.IsValid)
            {
                db.Entry(nhanVien).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("ManagerEmployee");
            }
            return View(nhanVien);
        }

        // GET: NhanVien/Delete/5
        public ActionResult DeleteEmployee(int? id)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NhanVien nhanVien = db.NhanVien.Find(id);
            if (nhanVien == null)
            {
                return HttpNotFound();
            }
            db.NhanVien.Remove(nhanVien);
            db.SaveChanges();
            return RedirectToAction("ManagerEmployee");
        }

        // GET: ChuXe
        public ActionResult ManagerGuest()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            //Lấy danh sách khách hàng
            var khachhang = db.KhachHang.ToList();
            return View(khachhang);
        }

        public ActionResult ManagerCar()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            //Lấy danh sách xe
            var xe = db.Xe.ToList();
            return View(xe);
        }

        public ActionResult ManagerPaid()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            var donthuexe = db.DonThueXe.ToList();
            return View(donthuexe);
        }
    }
}