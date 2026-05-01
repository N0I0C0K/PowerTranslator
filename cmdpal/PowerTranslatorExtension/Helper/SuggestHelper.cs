using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using PowerTranslatorExtension.Protocol;
using PowerTranslatorExtension.Utils;

namespace PowerTranslatorExtension;

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

public sealed class SuggestHelper
{
    private HttpClient _client;

    public SuggestHelper()
    {
        _client = new HttpClient(UtilsFun.httpClientDefaultHandler);
    }

    public List<ResultItem> QuerySuggest(string query)
    {
        var data = new { kw = query };
        try
        {
            var res = _client.PostAsync(
                "https://fanyi.baidu.com/sug",
                new StringContent(data.toFormDataBodyString(), new MediaTypeHeaderValue("application/x-www-form-urlencoded")))
                .GetAwaiter()
                .GetResult();
            var sug = UtilsFun.ParseJson<SuggestInterface>(res.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            if (sug?.data == null || sug.data.Length == 0)
            {
                return [];
            }

            var result = new List<ResultItem>();
            foreach (var item in sug.data)
            {
                result.Add(new ResultItem
                {
                    Title = item.v,
                    SubTitle = item.k,
                    fromApiName = "Baidu",
                    transType = "[Suggest]",
                    CopyTgt = item.v,
                    TextToSuggest = item.k,
                });
            }

            return result;
        }
        catch (Exception err)
        {
            return
            [
                new ResultItem
                {
                    Title = "some err happen in suggest",
                    SubTitle = err.Message,
                },
            ];
        }
    }

    public void Reload()
    {
        _client.Dispose();
        _client = new HttpClient(UtilsFun.httpClientDefaultHandler);
    }
}
