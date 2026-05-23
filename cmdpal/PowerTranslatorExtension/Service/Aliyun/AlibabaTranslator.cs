using System;
using System.Collections.Generic;
using System.Net.Http;
using PowerTranslatorExtension.Protocol;
using PowerTranslatorExtension.Utils;

namespace PowerTranslatorExtension.Service.Aliyun;

public class AliyunTranslateResult : ITranslateResult
{
    public struct Data
    {
        public string detectLanguage { get; set; }
        public string translateText { get; set; }
    }

    public string code { get; set; } = string.Empty;
    public Data data { get; set; }
    public bool success { get; set; }

    public IEnumerable<ResultItem>? Transform()
    {
        if (!success)
            return null;
        return new[]
        {
            new ResultItem
            {
                Title = data.translateText,
                SubTitle = data.detectLanguage,
                fromApiName = "Alibaba",
            },
        };
    }
}

internal class AliyunCsrfResp
{
    public string token { get; set; } = string.Empty;
    public string parameterName { get; set; } = string.Empty;
    public string headerName { get; set; } = string.Empty;
}

public class AliyunTranslator : ITranslator, IDisposable
{
    private HttpClient client;
    private const string userAgent = "Mozilla/5.0 (X11; CrOS i686 3912.101.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.116 Safari/537.36";
    private const string apiEndPoint = "https://translate.alibaba.com/api/translate/text";
    private const string csrfEndPoint = "https://translate.alibaba.com/api/translate/csrftoken";
    private string _csrf = string.Empty;
    private string _csrfHeader = string.Empty;
    private bool inited;

    public bool Inited => inited;
    public string Name => "Alibaba";

    public AliyunTranslator()
    {
        client = BuildClient();
    }

    private static HttpClient BuildClient()
    {
        var c = new HttpClient(UtilsFun.httpClientDefaultHandler, disposeHandler: false);
        c.DefaultRequestHeaders.Add("User-Agent", userAgent);
        c.DefaultRequestHeaders.Referrer = new Uri("https://translate.alibaba.com/");
        c.DefaultRequestHeaders.Add("Origin", "https://translate.alibaba.com");
        return c;
    }

    public void Init()
    {
        RefreshCsrf();
        inited = true;
    }

    public void Reset()
    {
        client.Dispose();
        client = BuildClient();
        RefreshCsrf();
    }

    private void RefreshCsrf()
    {
        var response = client.GetAsync(csrfEndPoint).GetAwaiter().GetResult();
        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var data = UtilsFun.ParseJson<AliyunCsrfResp>(content);
        if (data == null) return;
        _csrf = data.token;
        _csrfHeader = data.headerName;
        if (client.DefaultRequestHeaders.Contains(_csrfHeader))
            client.DefaultRequestHeaders.Remove(_csrfHeader);
        client.DefaultRequestHeaders.Add(_csrfHeader, _csrf);
    }

    public ITranslateResult? Translate(string src, string toLan, string fromLan)
    {
        if (!inited) return null;
        var form = new MultipartFormDataContent
        {
            { new StringContent(fromLan), "srcLang" },
            { new StringContent(toLan), "tgtLang" },
            { new StringContent("general"), "domain" },
            { new StringContent(src), "query" },
            { new StringContent(_csrf), "_csrf" },
        };
        var response = client.PostAsync(apiEndPoint, form).GetAwaiter().GetResult();
        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return UtilsFun.ParseJson<AliyunTranslateResult>(content);
    }

    public void Dispose()
    {
        client.Dispose();
        GC.SuppressFinalize(this);
    }
}
