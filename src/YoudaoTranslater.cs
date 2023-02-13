using System.Security.Cryptography;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Translater.Utils;

namespace Translater.Youdao
{
    public class YoudaoTranslater
    {
        public class TranslateResponse
        {
            public struct ResStruct
            {
                public string tgt { get; set; }
                public string src { get; set; }
            }
            public struct Entry
            {
                public string[] entries { get; set; }
                public int type { get; set; }
            }
            public int errorCode { get; set; }
            public ResStruct[][]? translateResult { get; set; }
            public string? type { get; set; }
            public Entry? smartResult { get; set; }
        }

        private HttpClient client;
        private Random random;
        private MD5 md5;
        private string userAgent = "Mozilla/5.0 (X11; CrOS i686 3912.101.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.116 Safari/537.36";
        public YoudaoTranslater()
        {
            this.random = new Random();
            this.md5 = MD5.Create();

            client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            client.DefaultRequestHeaders.Add("Referer", "https://fanyi.youdao.com/");
            client.DefaultRequestHeaders.Add("Origin", "https://fanyi.youdao.com");

            var res = client.GetAsync("https://rlogs.youdao.com/rlog.php".addQueryParameters(new
            {
                _npid = "fanyiweb",
                _ncat = "pageview",
                _ncoo = (2147483647 / this.random.Next(1, 10)).ToString(),
                nssn = "NULL",
                _nver = "1.2.0",
                _ntms = UtilsFun.GetUtcTimeNow().ToString(),
                _nhrf = "newweb_translate_text"
            })).GetAwaiter().GetResult();
            client.DefaultRequestHeaders.Add("cookies", res.Headers.GetValues("Set-Cookie").First());
        }

        private string md5Encrypt(string src)
        {
            var bytes = Encoding.UTF8.GetBytes(src);
            var res = md5.ComputeHash(bytes);
            var resStr = new StringBuilder();
            foreach (var item in res)
            {
                resStr.Append(item.ToString("x2"));
            }
            return resStr.ToString();
        }

        public TranslateResponse? translate(string src, string toLan = "AUTO", string fromLan = "AUTO")
        {
            var ts = UtilsFun.GetUtcTimeNow().ToString();
            var salt = $"{ts}{random.Next(0, 9)}";
            var bv = md5Encrypt(this.userAgent);
            var sign = md5Encrypt($"fanyideskweb{src}{salt}Ygy_4c=r#e#4EX^NUGUc5");
            var data = new
            {
                i = src,
                from = fromLan,
                to = toLan,
                smartresult = "dict",
                client = "fanyideskweb",
                salt = salt,
                sign = sign,
                lts = ts,
                bv = bv,
                doctype = "json",
                version = "2.1",
                keyfrom = "fanyi.web",
                action = "FY_BY_REALTlME"
            };
            var res = client.PostAsync("https://fanyi.youdao.com/translate_o?smartresult=dict&smartresult=rule",
                                        new StringContent(data.toFormDataBodyString(),
                                        new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded")))
                                        .GetAwaiter()
                                        .GetResult();
            string contentStr = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            try
            {
                var translateRes = JsonSerializer.Deserialize<TranslateResponse>(contentStr);
                return translateRes;
            }
            catch (JsonException)
            {
                throw new Exception($"can not find data in response content({contentStr}), http state code:{res.StatusCode}");
            }
        }
    }
}