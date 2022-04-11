using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace IkuaiApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MyController : ControllerBase
    {
        private readonly ILogger<MyController> _logger;
        private readonly IOptions<Config> _options;

        public MyController(ILogger<MyController> logger,IOptions<Config> options)
        {
            _logger = logger;
            _options = options;
        }

        [HttpGet("pppoe/{id?}")]
        public string Pppoe(int id = 0)
        {
            Post($"http://{_options.Value.url}/Action/call",
                "{\"func_name\":\"wan\",\"action\":\"link_pppoe_down\",\"param\":{\"id\":" + id + "}}");
            Thread.Sleep(200);
            Post($"http://{_options.Value.url}/Action/call",
                "{\"func_name\":\"wan\",\"action\":\"link_pppoe_up\",\"param\":{\"id\":" + id + "}}");
            Thread.Sleep(200);
            for (var i = 0; i < 20; i++)
            {
                var str = Post($"http://{_options.Value.url}/Action/call",
                    "{\"func_name\": \"wan\", \"action\": \"show\", \"param\": {\"id\": " + id +
                    ", \"TYPE\": \"data\"}}");
                var j = JsonConvert.DeserializeObject<dynamic>(str);
                if (j.ErrMsg != "Success") continue;
                var ip = j.Data.data[0].pppoe_ip_addr.ToString();
                if (!string.IsNullOrEmpty(ip)) return ip;
                Thread.Sleep(500);
            }

            return "";
        }

        [HttpGet("{id?}")]
        public string Get(int id = 0)
        {
            for (var i = 0; i < 3; i++)
            {
                var str = Post($"http://{_options.Value.url}/Action/call",
                    "{\"func_name\": \"wan\", \"action\": \"show\", \"param\": {\"id\": " + id +
                    ", \"TYPE\": \"data\"}}");
                var j = JsonConvert.DeserializeObject<dynamic>(str);
                if (j.ErrMsg != "Success") continue;
                if (id == 0)
                {
                    var ip1 = j.Data.data[0].pppoe_ip_addr.ToString();
                    return $"{ip1}";
                }
            }

            return "";
        }

        private string Post(string url, string data)
        {
            retry:
            using (var webClient = new WebClient())
            {
                webClient.Encoding = Encoding.GetEncoding("utf-8");
                webClient.Headers.Add("Content-Type", "application/json");
                if (!Common.CookieCollection.Any() || Common.isCookieTimeout)
                {
                    var container = new CookieContainer();
                    GetCookie($"http://{_options.Value.url}/Action/login", ref container);
                    var cookies = container.GetCookies(new Uri($"http://{_options.Value.url}"));
                    Common.CookieCollection = cookies;
                    Common.isCookieTimeout = false;
                }

                webClient.Headers.Add(HttpRequestHeader.Cookie,
                    $@"{Common.CookieCollection[0].Name}={Common.CookieCollection[0].Value}");
                var uploadData = Encoding.UTF8.GetBytes(data);
                var responseData = webClient.UploadData(url, "POST", uploadData);
                var re = Encoding.UTF8.GetString(responseData);
                var j = JsonConvert.DeserializeObject<dynamic>(re);
                if (j.Result != "10014") return re;
                Common.isCookieTimeout = true;
                goto retry;
            }
        }

        public  string GetCookie(string requestUrlString, ref CookieContainer cookie)
        {
            var myRequest = (HttpWebRequest) WebRequest.Create(requestUrlString);
            myRequest.Method = "POST";
            myRequest.ContentType = "application/json";
            using (var streamWriter = new StreamWriter(myRequest.GetRequestStream()))
            {
                var json =
                    "{\"username\":\""+_options.Value.username +"\",\"passwd\":\""+_options.Value.passwd+"\",\"pass\":\""+_options.Value.pass+"\",\"remember_password\":\"\"}";
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }

            myRequest.CookieContainer = new CookieContainer();
            var myResponse = (HttpWebResponse) myRequest.GetResponse();
            cookie.Add(myResponse.Cookies);
            var reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}