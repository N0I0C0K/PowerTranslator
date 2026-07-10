using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PowerTranslatorExtension.Protocol;
using PowerTranslatorExtension.Utils;

namespace PowerTranslatorExtension.Service.DeepL;

public class DeepLTranslateResult : ITranslateResult
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("result")]
    public InnerResult? Result { get; set; }

    public bool IsSuccess() => Result != null;

    public IEnumerable<ResultItem>? Transform()
    {
        if (!IsSuccess())
            return null;
        var res = new List<ResultItem>();
        foreach (var translation in this.Result!.Translations)
        {
            foreach (var beam in translation.Beams)
            {
                foreach (var sentence in beam.Sentences)
                {
                    res.Add(new ResultItem
                    {
                        Title = sentence.Text,
                        SubTitle = $"{this.Result!.SourceLang} -> {this.Result!.TargetLang}",
                        CopyTgt = sentence.Text,
                        fromApiName = "DeepL",
                    });
                }
            }
        }
        return res;
    }

    public class InnerResult
    {
        [JsonPropertyName("source_lang")]
        public required string SourceLang { get; set; }

        [JsonPropertyName("target_lang")]
        public required string TargetLang { get; set; }

        [JsonPropertyName("source_lang_is_confident")]
        public bool SourceLangIsConfident { get; set; }

        [JsonPropertyName("translations")]
        public required Translation[] Translations { get; set; }
    }

    public class Sentence
    {
        [JsonPropertyName("text")]
        public required string Text { get; set; }
    }

    public class Beams
    {
        [JsonPropertyName("num_symbols")]
        public int NumSymbols { get; set; }

        [JsonPropertyName("sentences")]
        public required Sentence[] Sentences { get; set; }
    }

    public class Translation
    {
        [JsonPropertyName("beams")]
        public required Beams[] Beams { get; set; }

        [JsonPropertyName("quality")]
        public string? Quality { get; set; }
    }
}

public class DeepLTranslateRequestBody
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("method")]
    public string Method { get; set; } = "LMT_handle_jobs";

    [JsonPropertyName("params")]
    public Params Params { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    public DeepLTranslateRequestBody(Params p)
    {
        this.Params = p;
        this.Id = (int)(p.Timestamp % 100000000);
    }
}

public class Params
{
    public class Job
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "default";

        [JsonPropertyName("sentences")]
        public List<JobSentence> Sentences { get; set; }

        [JsonPropertyName("preferred_num_beams")]
        public int PreferredNumBeams { get; set; } = 4;

        public Job(string text)
        {
            this.Sentences = new List<JobSentence>
            {
                new JobSentence { Id = 1, Prefix = "", Text = text },
            };
        }
    }

    public class JobSentence
    {
        [JsonPropertyName("text")]
        public required string Text { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("prefix")]
        public string? Prefix { get; set; }
    }

    public class Language
    {
        [JsonPropertyName("target_lang")]
        public required string TargetLang { get; set; }

        [JsonPropertyName("source_lang_computed")]
        public string? SourceLangComputed { get; set; }
    }

    public class CommonJobParams
    {
        [JsonPropertyName("quality")]
        public required string Quality { get; set; }

        [JsonPropertyName("mode")]
        public required string Mode { get; set; }

        [JsonPropertyName("browserType")]
        public int BrowserType { get; set; }

        [JsonPropertyName("textType")]
        public required string TextType { get; set; }
    }

    [JsonPropertyName("jobs")]
    public List<Job> Jobs { get; set; }

    [JsonPropertyName("lang")]
    public Language Lang { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; } = -1;

    [JsonPropertyName("commonJobParams")]
    public CommonJobParams commonJobParams { get; set; } = new CommonJobParams
    {
        Quality = "fast",
        Mode = "translate",
        BrowserType = 1,
        TextType = "plaintext",
    };

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public Params(string targetLan, string? srcLan, string text)
    {
        this.Lang = new Language { TargetLang = targetLan, SourceLangComputed = srcLan };
        this.Jobs = new List<Job> { new Job(text) };
    }
}

public class DeepLTranslator : ITranslator, IDisposable
{
    private HttpClient client;
    private const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0";
    private const string apiEndPoint = "https://www2.deepl.com/jsonrpc?client=chrome-extension,1.33.0";
    private bool inited;

    public bool Inited => inited;
    public string Name => "DeepL";

    public DeepLTranslator()
    {
        client = BuildClient();
    }

    private static HttpClient BuildClient()
    {
        var c = new HttpClient(UtilsFun.httpClientDefaultHandler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromSeconds(10),
        };
        c.DefaultRequestHeaders.Host = "www2.deepl.com";
        c.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        c.DefaultRequestHeaders.Connection.Add("keep-alive");
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

    public ITranslateResult? Translate(string src, string toLan, string fromLan)
    {
        if (string.Equals(toLan, "auto", StringComparison.OrdinalIgnoreCase))
            toLan = "en";
        string? from = string.Equals(fromLan, "auto", StringComparison.OrdinalIgnoreCase) ? null : fromLan;
        var body = new DeepLTranslateRequestBody(new Params(toLan, from, src));
        var bodyStr = JsonSerializer.Serialize(body);
        var response = client.PostAsync(apiEndPoint, new StringContent(bodyStr, Encoding.UTF8, "application/json"))
            .GetAwaiter().GetResult();
        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var res = UtilsFun.ParseJson<DeepLTranslateResult>(content);
        return res != null && res.IsSuccess() ? res : null;
    }

    public void Dispose()
    {
        client.Dispose();
        GC.SuppressFinalize(this);
    }
}
