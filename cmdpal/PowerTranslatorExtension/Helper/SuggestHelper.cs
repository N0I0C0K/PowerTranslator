using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using PowerTranslatorExtension.Protocol;
using PowerTranslatorExtension.Utils;

namespace PowerTranslatorExtension.Suggest;

public class SuggestInterface
{
    public struct SuggestItem
    {
        public string k { get; set; }
        public string v { get; set; }
    }
    public int errno { get; set; }
    public SuggestItem[]? data { get; set; }
}

public class SuggestHelper
{
    private HttpClient client;

    public SuggestHelper()
    {
        client = new HttpClient(UtilsFun.httpClientDefaultHandler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromSeconds(5),
        };
        UtilsFun.onHttpDefaultHandlerChange += () =>
        {
            client = new HttpClient(UtilsFun.httpClientDefaultHandler, disposeHandler: false)
            {
                Timeout = TimeSpan.FromSeconds(5),
            };
        };
    }

    public List<ResultItem> QuerySuggest(string query)
    {
        var body = new { kw = query }.toFormDataBodyString();
        try
        {
            var res = client.PostAsync(
                "https://fanyi.baidu.com/sug",
                new StringContent(body, new MediaTypeHeaderValue("application/x-www-form-urlencoded")))
                .GetAwaiter().GetResult();
            var raw = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var sug = UtilsFun.ParseJson<SuggestInterface>(raw);
            if (sug == null || sug.data == null || sug.data.Length == 0)
                return new List<ResultItem>();
            var result = new List<ResultItem>(sug.data.Length);
            foreach (var item in sug.data)
            {
                result.Add(new ResultItem
                {
                    Title = item.v,
                    SubTitle = item.k,
                    fromApiName = "Baidu",
                    transType = "[Suggest]",
                });
            }
            return result;
        }
        catch (Exception err)
        {
            UtilsFun.LogMessage($"Suggest failed: {err.Message}");
            return new List<ResultItem>();
        }
    }
}
