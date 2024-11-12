using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json.Linq;
using RestSharp;
namespace Mioto.Controllers
{
    public class OAuthController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public void Callback(string code, string error, string state)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                this.GetTokens(code);
            }
        }

        public ActionResult GetTokens(string code)
        {
            var tokenFile = "C:\\Program Files\\IIS Express\\Json\\tokens.json";
            var credentialsFile = "C:\\Program Files\\IIS Express\\Json\\api_calendar.json";
            var credentials = JObject.Parse(System.IO.File.ReadAllText(credentialsFile));

            RestClient restClient = new RestClient("https://oauth2.googleapis.com");

            RestRequest request = new RestRequest("token", Method.Post);

            request.AddParameter("client_id", credentials["web"]["client_id"].ToString());
            request.AddParameter("client_secret", credentials["web"]["client_secret"].ToString());
            request.AddParameter("code", code);
            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("redirect_uri", "https://localhost:44380/oauth/callback");

            var response = restClient.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                System.IO.File.WriteAllText(tokenFile, response.Content);
                return RedirectToAction("Index", "Home");
            }

            return View("Error");
        }

        public ActionResult RefreshToken()
        {
            var tokenFile = "C:\\Program Files\\IIS Express\\Json\\tokens.json";
            var credentialsFile = "C:\\Program Files\\IIS Express\\Json\\api_calendar.json";
            var credentials = JObject.Parse(System.IO.File.ReadAllText(credentialsFile));
            var tokens = JObject.Parse(System.IO.File.ReadAllText(tokenFile));

            RestClient restClient = new RestClient("https://oauth2.googleapis.com");
            RestRequest request = new RestRequest("token", Method.Post);

            request.AddParameter("client_id", credentials["web"]["client_id"].ToString());
            request.AddParameter("client_secret", credentials["web"]["client_secret"].ToString());
            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("refresh_token", tokens["refresh_token"].ToString());

            var response = restClient.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                JObject newTokens = JObject.Parse(response.Content);
                newTokens["refresh_token"] = tokens["refresh_token"].ToString();
                System.IO.File.WriteAllText(tokenFile, newTokens.ToString());
                return RedirectToAction("Index", "Home", new { status = "success" });
            }

            return View("Error");
        }

        public ActionResult RevokeToken()
        {
            var tokenFile = "C:\\Program Files\\IIS Express\\Json\\tokens.json";
            var tokens = JObject.Parse(System.IO.File.ReadAllText(tokenFile));

            RestClient restClient = new RestClient("https://oauth2.googleapis.com");
            RestRequest request = new RestRequest("revoke", Method.Post);

            request.AddParameter("token", tokens["access_token"].ToString());

            var response = restClient.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return RedirectToAction("Index", "Home", new { status = "success" });
            }
            return View("Error");
        }


    }
}