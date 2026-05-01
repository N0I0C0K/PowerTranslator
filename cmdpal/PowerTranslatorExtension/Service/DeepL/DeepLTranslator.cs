using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
    public ResultData? Result { get; set; }

    public bool IsSuccess() => Result != null;

    public IEnumerable<ResultItem>? Transform()
    {
        if (!IsSuccess())
        {
            return null;
        }

        List<ResultItem> res = [];
        foreach (var translation in Result!.Translations)
        {
            foreach (var beam in translation.Beams)
            {
                foreach (var sentence in beam.Sentences)
                {
                    res.Add(new ResultItem
                    {
                        Title = sentence.Text,
                        SubTitle = $"{Result.SourceLang} -> {Result.TargetLang}",
                        CopyTgt = sentence.Text,
                        fromApiName = "DeepL",
                    });
                }
            }
        }

        return res;
    }

    public class ResultData
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

    public DeepLTranslateRequestBody(Params parameters)
    {
        Params = parameters;
        Id = (int)(parameters.Timestamp % 100000000);
    }
}

public class Params
{
    public class Job
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "default";

        [JsonPropertyName("sentences")]
        public List<Sentence> Sentences { get; set; }

        [JsonPropertyName("preferred_num_beams")]
        public int PreferredNumBeams { get; set; } = 4;

        public Job(string text)
        {
            Sentences =
            [
                new Sentence
                {
                    Id = 1,
                    Prefix = string.Empty,
                    Text = text,
                },
            ];
        }
    }

    public class Sentence
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
    public CommonJobParams CommonJobParameters { get; set; } = new()
    {
        Quality = "fast",
        Mode = "translate",
        BrowserType = 1,
        TextType = "plaintext",
    };

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public Params(string targetLanguage, string? sourceLanguage, string text)
    {
        Lang = new Language
        {
            TargetLang = targetLanguage,
            SourceLangComputed = sourceLanguage,
        };
        Jobs = [new Job(text)];
    }
}

public sealed class DeepLTranslator : ITranslator
{
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0";
    private const string ApiEndpoint = "https://www2.deepl.com/jsonrpc?client=chrome-extension,1.33.0";
    private HttpClient _client;

    public bool Inited { get; private set; }
    public string Name => "DeepL Web Api";

    public DeepLTranslator()
    {
        _client = CreateClient();
    }

    public void Init()
    {
        Inited = true;
    }

    public ITranslateResult? Translate(string src, string toLan, string fromLan)
    {
        if (!Inited)
        {
            return null;
        }

        if (toLan == "auto")
        {
            toLan = "en";
        }

        if (fromLan == "auto")
        {
            fromLan = null!;
        }

        var body = new DeepLTranslateRequestBody(new Params(toLan, fromLan, src));
        var bodyStr = JsonSerializer.Serialize(body, new JsonSerializerOptions { WriteIndented = true });
        var response = _client.PostAsync(ApiEndpoint, new StringContent(bodyStr, Encoding.Default, "application/json"))
            .GetAwaiter()
            .GetResult();
        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var result = JsonSerializer.Deserialize<DeepLTranslateResult>(content);
        return result != null && result.IsSuccess() ? result : null;
    }

    public void Reset()
    {
        _client.Dispose();
        _client = CreateClient();
        Inited = true;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient(UtilsFun.httpClientDefaultHandler);
        client.DefaultRequestHeaders.Host = "www2.deepl.com";
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        client.DefaultRequestHeaders.Connection.Add("keep-alive");
        return client;
    }
}
