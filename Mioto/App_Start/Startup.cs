using Microsoft.Owin;
using Microsoft.Owin.Security.Google;
using Newtonsoft.Json.Linq;
using Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

[assembly: OwinStartup(typeof(Mioto.App_Start.Startup))]

namespace Mioto.App_Start
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Đọc thông tin client từ file JSON
            //var json = File.ReadAllText("~/Json/client_secret.json");
            //var jsonObj = JObject.Parse(json);
            //var clientId = jsonObj["installed"]["client_id"].ToString();
            //var clientSecret = jsonObj["installed"]["client_secret"].ToString();

            //app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
            //{
            //    ClientId = clientId,
            //    ClientSecret = clientSecret,
            //    CallbackPath = new PathString("/auth/callback")
            //});
        }
    }
}