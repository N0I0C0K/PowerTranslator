using System.Net.Http;
using System.Text.Json;
using Translator.Protocol;


namespace Translator.Service.Aliyun;

public class AliyunTranslateResult : ITranslateResult
{
    public struct Data
    {
        public string detectLanguage { get; set; }
        public string translateText { get; set; }
    }
    public string code { get; set; }
    public Data data { get; set; }
    public bool success { get; set; }

    public override IEnumerable<ResultItem>? Transform()
    {
        if (!success)
            return null;
        return [
            new ResultItem{
                Title=data.translateText,
                SubTitle=data.detectLanguage,
            }
        ];
    }
}

internal class AliyunCsrfResp
{
    public string token { get; set; }
    public string parameterName { get; set; }
    public string headerName { get; set; }
}

public class AliyunTranslator : ITranslator
{
    private HttpClient client;
    private const string userAgent = "Mozilla/5.0 (X11; CrOS i686 3912.101.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.116 Safari/537.36";
    private const string apiEndPoint = "https://translate.alibaba.com/api/translate/text";
    private const string csrfEndPoint = "https://translate.alibaba.com/api/translate/csrftoken";
    private string _csrf;
    public AliyunTranslator()
    {
        client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", userAgent);
        client.DefaultRequestHeaders.Add("Referer", "https://translate.alibaba.com/");
        client.DefaultRequestHeaders.Add("Origin", "https://translate.alibaba.com");
        _csrf = "";
        _refreshCsrf();
    }

    private void _refreshCsrf()
    {
        var response = this.client.GetAsync(csrfEndPoint)
                                            .GetAwaiter()
                                            .GetResult();
        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        Console.WriteLine(content);
        var data = JsonSerializer.Deserialize<AliyunCsrfResp>(content);

        _csrf = data!.token;
        if (client.DefaultRequestHeaders.Contains(data.headerName))
            client.DefaultRequestHeaders.Remove(data.headerName);
        client.DefaultRequestHeaders.Add(data.headerName, _csrf);
    }

    public override AliyunTranslateResult? Translate(string src, string fromLan, string toLan)
    {
        var multiPartData = new MultipartFormDataContent()
        {
            { new StringContent(fromLan), "srcLang" },
            { new StringContent(toLan), "tgtLang" },
            { new StringContent("general"), "domain" },
            { new StringContent(src), "query" },
            { new StringContent(_csrf), "_csrf" },
        };
        var response = this.client.PostAsync(apiEndPoint, multiPartData)
                                            .GetAwaiter()
                                            .GetResult();
        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        Console.WriteLine(content);
        var res = JsonSerializer.Deserialize<AliyunTranslateResult>(content);
        return res;
    }

    public override void Reset()
    {
        _refreshCsrf();
    }
}
