using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Translater.Utils;

namespace Translater.Youdao.Backup;

public class TranslateResult : ITranslateResult
{
    public struct Basic
    {
        public string[]? exam_type { get; set; }
        public string? phonetic { get; set; }
        public string[] explains { get; set; }
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

    public override IEnumerable<ResultItem>? Transform()
    {
        if (this.errorCode != "0")
            return null;
        List<ResultItem> res = new List<ResultItem>();
        res.Add(new ResultItem
        {
            Title = String.Join(",", this.translation!),
            SubTitle = $"{this.query}({this.basic?.phonetic ?? "-"}) [{this.tranType}] backup"
        });
        if (this.web != null)
        {
            foreach (var web in this.web)
            {
                res.Add(new ResultItem
                {
                    Title = String.Join(" | ", web.value),
                    SubTitle = $"{web.key} [smart result]"
                });
            }
        }
        res.each((val) =>
        {
            val.fromApiName = "Backup Youdao Api";
        });
        return res;
    }
}

public class BackUpTranslater : ITranslater
{
    private HttpClient client;
    private const string userAgent = "Mozilla/5.0 (X11; CrOS i686 3912.101.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.116 Safari/537.36";
    public BackUpTranslater()
    {
        client = new HttpClient();
        client.Timeout = TimeSpan.FromMilliseconds(500);
        client.DefaultRequestHeaders.Add("User-Agent", userAgent);
        client.DefaultRequestHeaders.Add("Referer", "https://ai.youdao.com/");
        client.DefaultRequestHeaders.Add("Origin", "https://ai.youdao.com");
    }

    public override void Reset()
    {

    }

    public override TranslateResult? Translate(string src, string fromLan = "Auto", string toLan = "Auto")
    {
        var data = new
        {
            q = src,
            from = fromLan,
            to = toLan
        }.toFormDataBodyString();
        var response = this.client.PostAsync("https://aidemo.youdao.com/trans", new StringContent(
                                            data,
                                            new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded")))
                                            .GetAwaiter()
                                            .GetResult();
        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        Console.WriteLine(content);
        return JsonSerializer.Deserialize<TranslateResult>(content);
    }
}
