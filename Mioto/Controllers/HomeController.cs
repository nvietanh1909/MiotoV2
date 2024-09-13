﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Mioto.Models;

namespace Mioto.Controllers
{
    public class HomeController : Controller
    {
        DB_MiotoEntities db = new DB_MiotoEntities();
        public bool IsLoggedIn { get => Session["KhachHang"] != null || Session["ChuXe"] != null; }
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
        public ActionResult Home()
        {
            ViewBag.TinhThanhPho = tinhThanhPho;
            return View();
        }

        public ActionResult Car(string khuvuc)
        {
            var KhachHang = Session["KhachHang"] as KhachHang;
            var ChuXe = Session["ChuXe"] as ChuXe;
            var xe = db.Xe.Where(x => x.IDCX == ChuXe.IDCX);
            var ds_xe = db.Xe.Where(x => x.KhuVuc == khuvuc && x.TrangThai == "Sẵn sàng").ToList();

            if (xe == null || KhachHang == null) return View(ds_xe);

            if(ChuXe != null)
            {
                ds_xe = db.Xe.Where(x => x.KhuVuc == khuvuc && x.TrangThai == "Sẵn sàng" && x.IDCX != ChuXe.IDCX).ToList();
                return View(ds_xe);
            }
            else return View(ds_xe);

        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult IntroOwnerCar()
        {
            return View();
        }

    }
}