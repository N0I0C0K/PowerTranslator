using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
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
        public string[]? value { get; set; }
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
        if (errorCode != "0")
        {
            return null;
        }

        List<ResultItem> res =
        [
            new ResultItem
            {
                Title = string.Join(",", translation ?? []),
                SubTitle = $"{query}({basic?.phonetic ?? "-"}) [{tranType}] backup",
                CopyTgt = string.Join(",", translation ?? []),
            },
        ];

        if (web != null)
        {
            foreach (var item in web)
            {
                res.Add(new ResultItem
                {
                    Title = string.Join(" | ", item.value ?? []),
                    SubTitle = $"{item.key} [smart result]",
                });
            }
        }

        res.each(val => val.fromApiName = "Backup Youdao Api");
        return res;
    }
}

public sealed class BackUpTranslator : ITranslator
{
    private const string UserAgent = "Mozilla/5.0 (X11; CrOS i686 3912.101.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.116 Safari/537.36";
    private HttpClient _client;

    public bool Inited { get; private set; }
    public string Name => "Youdao Backup Api";

    public BackUpTranslator()
    {
        _client = CreateClient();
    }

    public void Init()
    {
        Inited = true;
    }

    public ITranslateResult? Translate(string src, string toLan = "Auto", string fromLan = "Auto")
    {
        if (!Inited)
        {
            return null;
        }

        fromLan = YoudaoUtils.Instance.GetYoudaoLanguageCode(fromLan);
        toLan = YoudaoUtils.Instance.GetYoudaoLanguageCode(toLan);

        var data = new
        {
            q = src,
            from = fromLan,
            to = toLan,
        }.toFormDataBodyString();
        var response = _client.PostAsync(
            "https://aidemo.youdao.com/trans",
            new StringContent(data, new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded")))
            .GetAwaiter()
            .GetResult();
        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return JsonSerializer.Deserialize<TranslateResult>(content);
    }

    public void Reset()
    {
        _client.Dispose();
        _client = CreateClient();
        Inited = true;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient(UtilsFun.httpClientDefaultHandler)
        {
            Timeout = TimeSpan.FromMilliseconds(500),
        };
        client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        client.DefaultRequestHeaders.Add("Referer", "https://ai.youdao.com/");
        client.DefaultRequestHeaders.Add("Origin", "https://ai.youdao.com");
        return client;
    }
}
