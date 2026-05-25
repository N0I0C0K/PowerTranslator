using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PowerTranslatorExtension.Protocol;
using PowerTranslatorExtension.Service.Youdao.Utils;
using PowerTranslatorExtension.Utils;

namespace PowerTranslatorExtension.Service.Youdao.Old;

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

    public IEnumerable<ResultItem>? Transform()
    {
        if (this.errorCode != 0 || this.translateResult == null || this.translateResult.Length == 0)
            return null;
        var res = new List<ResultItem>
        {
            new ResultItem
            {
                Title = this.translateResult[0][0].tgt,
                SubTitle = this.translateResult[0][0].src,
                transType = this.type ?? "Translate",
            }
        };
        if (this.smartResult?.entries != null)
        {
            foreach (var s in this.smartResult.Value.entries)
            {
                var t = s.Replace("\r\n", " ").TrimStart().Replace(" ", "");
                if (string.IsNullOrEmpty(t))
                    continue;
                res.Add(new ResultItem { Title = t, SubTitle = Loc.Get("Tag_SmartResult") });
            }
        }
        foreach (var val in res)
            val.fromApiName = "Youdao Old Web Api";
        return res;
    }
}

public class YoudaoTranslator : ITranslator, IDisposable
{
    private HttpClient? client;
    private readonly Random random = new();
    private readonly MD5 md5 = MD5.Create();
    private string userAgent = UtilsFun.GetRandomUserAgent();
    private bool inited;

    public bool Inited => inited;
    public string Name => "Youdao Old Web Api";

    public void Init() => Reset();

    public void Reset()
    {
        client?.Dispose();
        client = new HttpClient(UtilsFun.httpClientDefaultHandler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromSeconds(10),
        };
        client.DefaultRequestHeaders.Add("User-Agent", userAgent);
        client.DefaultRequestHeaders.Referrer = new Uri("https://fanyi.youdao.com/");
        client.DefaultRequestHeaders.Add("Origin", "https://fanyi.youdao.com");
        var ncoo = $"OUTFOX_SEARCH_USER_ID_NCOO={random.Next(100000000, 999999999)}.{random.Next(100000000, 999999999)}";
        var uid = $"OUTFOX_SEARCH_USER_ID={random.Next(100000000, 999999999)}@{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}";
        client.DefaultRequestHeaders.Add("Cookie", $"{ncoo};{uid}");
        inited = true;
    }

    private string Md5Encrypt(string src)
    {
        var bytes = Encoding.UTF8.GetBytes(src);
        var hash = md5.ComputeHash(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    public ITranslateResult? Translate(string src, string toLan = "AUTO", string fromLan = "AUTO")
    {
        if (!inited || client == null)
            return null;
        toLan = YoudaoUtils.Instance.GetYoudaoLanguageCode(toLan);
        fromLan = YoudaoUtils.Instance.GetYoudaoLanguageCode(fromLan);

        var ts = UtilsFun.GetUtcTimeNow().ToString(System.Globalization.CultureInfo.InvariantCulture);
        var salt = $"{ts}{random.Next(0, 9)}";
        var bv = Md5Encrypt(this.userAgent);
        var sign = Md5Encrypt($"fanyideskweb{src}{salt}Ygy_4c=r#e#4EX^NUGUc5");
        var data = new
        {
            i = src,
            from = fromLan,
            to = toLan,
            smartresult = "dict",
            client = "fanyideskweb",
            salt,
            sign,
            lts = ts,
            bv,
            doctype = "json",
            version = "2.1",
            keyfrom = "fanyi.web",
            action = "FY_BY_REALTlME",
        };
        var res = client.PostAsync(
            "https://fanyi.youdao.com/translate_o?smartresult=dict&smartresult=rule",
            new StringContent(data.toFormDataBodyString(), new MediaTypeHeaderValue("application/x-www-form-urlencoded")))
            .GetAwaiter().GetResult();
        var content = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return UtilsFun.ParseJson<TranslateResponse>(content);
    }

    public void Dispose()
    {
        client?.Dispose();
        md5.Dispose();
        GC.SuppressFinalize(this);
    }
}
