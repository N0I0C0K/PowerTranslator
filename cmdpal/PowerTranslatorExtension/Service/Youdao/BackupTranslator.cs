using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using PowerTranslatorExtension.Protocol;
using PowerTranslatorExtension.Service.Youdao.Utils;
using PowerTranslatorExtension.Utils;

namespace PowerTranslatorExtension.Service.Youdao.Backup;

public class TranslateResult : ITranslateResult
{
    public struct Basic
    {
        public string[]? exam_type { get; set; }
        public string? phonetic { get; set; }
        public string[]? explains { get; set; }
    }

    public struct Web
    {
        public string[] value { get; set; }
        public string key { get; set; }
    }

    public string errorCode { get; set; } = "1";

    [JsonPropertyName("l")]
    public string? tranType { get; set; }
    public string[]? translation { get; set; }
    public string? query { get; set; }
    public Basic? basic { get; set; }
    public Web[]? web { get; set; }

    public IEnumerable<ResultItem>? Transform()
    {
        if (this.errorCode != "0" || this.translation == null)
            return null;
        var res = new List<ResultItem>
        {
            new ResultItem
            {
                Title = string.Join(",", this.translation),
                SubTitle = $"{this.query}({this.basic?.phonetic ?? "-"}) [{this.tranType}] backup",
            }
        };
        if (this.web != null)
        {
            foreach (var w in this.web)
            {
                res.Add(new ResultItem
                {
                    Title = string.Join(" | ", w.value),
                    SubTitle = $"{w.key} [smart result]",
                });
            }
        }
        foreach (var val in res)
            val.fromApiName = "Backup Youdao Api";
        return res;
    }
}

public class BackUpTranslator : ITranslator, IDisposable
{
    private HttpClient client;
    private const string userAgent = "Mozilla/5.0 (X11; CrOS i686 3912.101.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.116 Safari/537.36";
    private bool inited;

    public bool Inited => inited;
    public string Name => "Backup Youdao Api";

    public BackUpTranslator()
    {
        client = BuildClient();
    }

    private static HttpClient BuildClient()
    {
        var c = new HttpClient(UtilsFun.httpClientDefaultHandler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromMilliseconds(500),
        };
        c.DefaultRequestHeaders.Add("User-Agent", userAgent);
        c.DefaultRequestHeaders.Referrer = new Uri("https://ai.youdao.com/");
        c.DefaultRequestHeaders.Add("Origin", "https://ai.youdao.com");
        return c;
    }

    public void Init()
    {
        inited = true;
    }

    public void Reset()
    {
        client.Dispose();
        client = BuildClient();
    }

    public ITranslateResult? Translate(string src, string toLan = "Auto", string fromLan = "Auto")
    {
        fromLan = YoudaoUtils.Instance.GetYoudaoLanguageCode(fromLan);
        toLan = YoudaoUtils.Instance.GetYoudaoLanguageCode(toLan);

        var data = new { q = src, from = fromLan, to = toLan }.toFormDataBodyString();
        var response = client.PostAsync(
            "https://aidemo.youdao.com/trans",
            new StringContent(data, new MediaTypeHeaderValue("application/x-www-form-urlencoded")))
            .GetAwaiter().GetResult();
        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return UtilsFun.ParseJson<TranslateResult>(content);
    }

    public void Dispose()
    {
        client.Dispose();
        GC.SuppressFinalize(this);
    }
}
