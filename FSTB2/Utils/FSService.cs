using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FSTB2.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FSTB2.Utils
{
    public static class FSService
    {
        public static List<Event>? GetTickets()
        {
            try
            {
                var webRequest = WebRequest.Create("https://api.kassir.ru/api/widget-page-kit?type=E&key=0d043285-33ff-bbbb-d1f0-4d379a98d494&domain=spb.kassir.ru&id=1814635&widgetKey=0d043285-33ff-bbbb-d1f0-4d379a98d494");
                webRequest.Method = "GET";
                var response = (HttpWebResponse)webRequest.GetResponse();
                var str      = "";
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                    str = streamReader.ReadToEnd();
                var q = JObject.Parse(str);
                return JsonConvert.DeserializeObject<FsReply>(str).Kit.eventBuckets[0].events;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
