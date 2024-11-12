using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Mioto.Models;
using Newtonsoft.Json.Linq;
using RestSharp;

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

        public ActionResult Car(string khuvuc, DateTime? startTime, DateTime? endTime)
        {
            var KhachHang = Session["KhachHang"] as KhachHang;
            var ChuXe = Session["ChuXe"] as ChuXe;

            if (startTime == null || endTime == null)
            {
                return View();
            }
            else
            {
                Session["StartDateTime"] = startTime;
                Session["EndDateTime"] = endTime;
            }
            var xe = db.Xe.Where(x => x.IDCX == ChuXe.IDCX);
            var ds_xe = db.Xe.Where(x => x.KhuVuc == khuvuc && x.TrangThaiThue == "Sẵn sàng").ToList();

            var tokenFile = "C:\\Program Files\\IIS Express\\Json\\tokens.json";
            var tokens = JObject.Parse(System.IO.File.ReadAllText(tokenFile));

            RestClient restClient = new RestClient();
            RestRequest request = new RestRequest("https://www.googleapis.com/calendar/v3/calendars/primary/events", Method.Get);

            request.AddQueryParameter("key", "AIzaSyDs2PE3cSuieWJalZMbSmoiC0v1NefPvhU");
            request.AddHeader("Authorization", "Bearer " + tokens["access_token"]);
            request.AddHeader("Accept", "application/json");

            var response = restClient.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                JObject calendarEvents = JObject.Parse(response.Content);
                var allEvents = calendarEvents["items"]
                    .Where(eventItem => eventItem["start"]?["dateTime"] != null && eventItem["end"]?["dateTime"] != null)
                    .Select(eventItem => eventItem.ToObject<Mioto.Models.Events>())
                    .ToList();

                var filteredEvents = allEvents.Where(e =>
                {
                    DateTime eventStart = DateTime.Parse(e.Start.DateTime);
                    DateTime eventEnd = DateTime.Parse(e.End.DateTime);

                    return (eventStart < endTime && eventEnd > startTime); 
                }).ToList();

                var rentedCars = filteredEvents.Select(e => e.Summary).ToList();
                ds_xe = ds_xe.Where(x => !rentedCars.Contains(x.BienSo)).ToList(); 

                return View(ds_xe);
            }
            return View("Error");
        }
        
        public ActionResult About()
        {
            return View();
        }

        public ActionResult IntroOwnerCar()
        {
            return View();
        }

        public ActionResult Index()
        {
            return View();
        }
    }
}