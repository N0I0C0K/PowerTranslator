using System.Security.Cryptography;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Translater.Utils;

namespace Translater.Youdao.old;
public class TranslateResponse : ITranslateResult
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

    public override IEnumerable<ResultItem>? Transform()
    {
        if (this.errorCode != 0)
            return null;
        List<ResultItem> res = new List<ResultItem>();
        res.Add(new ResultItem
        {
            Title = this.translateResult![0][0].tgt,
            SubTitle = $"{this.translateResult![0][0].src}",
            transType = this.type ?? "Translate"
        });
        if (this.smartResult != null)
        {
            this.smartResult?.entries.each((s) =>
            {
                string t = s.Replace("\r\n", " ").TrimStart().Replace(" ", "");
                if (string.IsNullOrEmpty(t))
                    return;
                res.Add(new ResultItem
                {
                    Title = t,
                    SubTitle = "[smart result]"
                });
            });
        }
        res.each((val) =>
        {
            val.fromApiName = "Youdao Old Web Api";
        });
        return res;
    }
}

public class YoudaoTranslater : ITranslater
{
    private HttpClient client;
    private Random random;
    private MD5 md5;
    private string userAgent;
    public YoudaoTranslater()
    {
        this.userAgent = UtilsFun.GetRandomUserAgent();
        this.random = new Random();
        this.md5 = MD5.Create();
        client = new HttpClient(UtilsFun.httpClientDefaultHandler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        this.Reset();
    }
    public override void Reset()
    {
        this.client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("User-Agent", userAgent);
        client.DefaultRequestHeaders.Add("Referer", "https://fanyi.youdao.com/");
        client.DefaultRequestHeaders.Add("Origin", "https://fanyi.youdao.com");
        string OUTFOX_SEARCH_USER_ID_NCOO = $"OUTFOX_SEARCH_USER_ID_NCOO={random.Next(100000000, 999999999)}.{random.Next(100000000, 999999999)}";
        string OUTFOX_SEARCH_USER_ID = $"OUTFOX_SEARCH_USER_ID={random.Next(100000000, 999999999)}@{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}";
        client.DefaultRequestHeaders.Add("Cookie", $"{OUTFOX_SEARCH_USER_ID_NCOO};{OUTFOX_SEARCH_USER_ID}");
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

    public override TranslateResponse? Translate(string src, string toLan = "AUTO", string fromLan = "AUTO")
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
