﻿using Mioto.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Mioto.Controllers
{
    public class AdminController : Controller
    {
        DB_MiotoEntities db = new DB_MiotoEntities();
        public bool IsLoggedIn { get => Session["NhanVien"] != null; }

        // GET: DetailAccount
        public ActionResult InfoAccount()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");
            var guest = Session["NhanVien"] as NhanVien;
            if (guest == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Khách hàng không tồn tại");
            var kh = db.KhachHang.Where(x => x.IDKH == guest.IDNV);
            return View(kh);
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
        public ActionResult AddDiscount(MD_MaGiamGia model)
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
                        MaGG = model.Ma,
                        PhanTramGiam = model.PhanTramGiam,
                        NgayBatDau = model.NgayBatDau,
                        NgayKetThuc = model.NgayKetThuc,
                        SoLuong = model.SolanSuDung
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


    }
}