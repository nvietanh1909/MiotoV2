using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mioto.Models
{
    public class Events
    {
        public Events()
        {
            this.Start = new EventDateTime()
            {
                TimeZone = "UTC"
            };
            this.End = new EventDateTime()
            {
                TimeZone = "UTC"
            };
        }

        public string Id { get; set; }
        public string Summary { get; set; }
        public string Desciption { get; set; }
        public EventDateTime Start { get; set; }
        public EventDateTime End { get; set; }

        public class EventDateTime
        {
            public string DateTime { get; set; }
            public string TimeZone { get; set; }
        }
    }
}