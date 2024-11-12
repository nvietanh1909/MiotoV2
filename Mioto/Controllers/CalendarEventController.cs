using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Google.Apis.Calendar.v3.Data;
using Mioto.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RestSharp;
using static Google.Apis.Requests.BatchRequest;

namespace Mioto.Controllers
{
    public class CalendarEventController : Controller
    {
        [HttpPost]
        public ActionResult CreateEvent(Mioto.Models.Events calendarEvent)
        {
            var tokenFile = "C:\\Program Files\\IIS Express\\Json\\tokens.json";
            var tokens = JObject.Parse(System.IO.File.ReadAllText(tokenFile));

            RestClient restClient = new RestClient();
            RestRequest request = new RestRequest("https://www.googleapis.com/calendar/v3/calendars/primary/events", Method.Post);

            calendarEvent.Start.DateTime = DateTime.Parse(calendarEvent.Start.DateTime).ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
            calendarEvent.End.DateTime = DateTime.Parse(calendarEvent.End.DateTime).ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");

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
                return RedirectToAction("Index", "Home", new { status = "success" });
            }
            return View("Error");
        }

        public ActionResult AllEvents()
        {
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
                    .ToList()
                    .Select(eventItem => eventItem.ToObject<Mioto.Models.Events>())
                    .ToList(); 

                return View(allEvents);
            }
            return View("Error");
        }

        [HttpGet]
        public ActionResult UpdateEvent(string identifier)
        {
            var tokenFile = "C:\\Program Files\\IIS Express\\Json\\tokens.json";
            var tokens = JObject.Parse(System.IO.File.ReadAllText(tokenFile));

            RestClient restClient = new RestClient("https://www.googleapis.com/calendar/v3/calendars/primary/events/" + identifier);
            RestRequest request = new RestRequest();

            request.AddQueryParameter("key", "AIzaSyDs2PE3cSuieWJalZMbSmoiC0v1NefPvhU");
            request.AddHeader("Authorization", "Bearer " + tokens["access_token"]);
            request.AddHeader("Accept", "application/json");

            var response = restClient.Get(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                JObject calendarEvents = JObject.Parse(response.Content);
                return View(calendarEvents.ToObject<Mioto.Models.Events>());
            }
            return View("Error");
        }


        [HttpPost]
        public ActionResult UpdateEvent(string identifier, Mioto.Models.Events calendarEvent)
        {
            var tokenFile = "C:\\Program Files\\IIS Express\\Json\\tokens.json";
            var tokens = JObject.Parse(System.IO.File.ReadAllText(tokenFile));

            RestClient restClient = new RestClient("https://www.googleapis.com/calendar/v3/calendars/primary/events/" + identifier);
            RestRequest request = new RestRequest();

            calendarEvent.Start.DateTime = DateTime.Parse(calendarEvent.Start.DateTime).ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
            calendarEvent.End.DateTime = DateTime.Parse(calendarEvent.End.DateTime).ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");

            var model = JsonConvert.SerializeObject(calendarEvent, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            request.AddQueryParameter("key", "AIzaSyDs2PE3cSuieWJalZMbSmoiC0v1NefPvhU");
            request.AddHeader("Authorization", "Bearer " + tokens["access_token"]);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("application/json", model, ParameterType.RequestBody);

            var response = restClient.Patch(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return RedirectToAction("AllEvents", "CalendarEvent", new { status = "updated" });
            }

            return View("Error");
        }

        public ActionResult DeleteEvent(string identifier)
        {
            var tokenFile = "C:\\Program Files\\IIS Express\\Json\\tokens.json";
            var tokens = JObject.Parse(System.IO.File.ReadAllText(tokenFile));

            RestClient restClient = new RestClient("https://www.googleapis.com/calendar/v3/calendars/primary/events/" + identifier);
            RestRequest request = new RestRequest();

            request.AddQueryParameter("key", "AIzaSyDs2PE3cSuieWJalZMbSmoiC0v1NefPvhU");
            request.AddHeader("Authorization", "Bearer " + tokens["access_token"]);
            request.AddHeader("Accept", "application/json");

            var response = restClient.Delete(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return RedirectToAction("AllEvents", "CalendarEvent", new { status = "deleted" });
            }

            return View("Error");
        }


    }
}