using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Translator.Protocol;
using Translator.Utils;


namespace Translator.Service.DeepL;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Wox.Plugin.Logger;

public class DeepLTranslateResult : ITranslateResult
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("result")]
    public _Result? Result { get; set; }

    public bool IsSuccess()
    {
        return Result != null;
    }

    public override IEnumerable<ResultItem>? Transform()
    {
        if (!IsSuccess())
            return null;
        List<ResultItem> res = new List<ResultItem>();
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

    public class _Result
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

    public DeepLTranslateRequestBody(Params _params)
    {
        this.Params = _params;
        this.Id = (int)(_params.Timestamp % 100000000);
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
            this.Sentences = new List<Sentence>{
                new Sentence{
                    Id = 1,
                    Prefix = "",
                    Text = text
                }
            };
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
    public CommonJobParams commonJobParams { get; set; } = new CommonJobParams
    {
        Quality = "fast",
        Mode = "translate",
        BrowserType = 1,
        TextType = "plaintext"
    };

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // 使用当前时间戳

    public Params(string targetLan, string? srcLan, string text)
    {
        this.Lang = new Language
        {
            TargetLang = targetLan,
            SourceLangComputed = srcLan
        };
        this.Jobs = new List<Job>{
            new Job(text)
        };
    }
}

public class SplitTextResp
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("result")]
    public _Result Result { get; set; }

    public class _Result
    {
        [JsonPropertyName("lang")]
        public Language Lang { get; set; }

        [JsonPropertyName("texts")]
        public List<Text> Texts { get; set; }

        public class Language
        {
            [JsonPropertyName("detected")]
            public string Detected { get; set; }

            [JsonPropertyName("isConfident")]
            public bool IsConfident { get; set; }
        }

        public class Text
        {
            [JsonPropertyName("chunks")]
            public List<Chunk> Chunks { get; set; }

            public class Chunk
            {
                [JsonPropertyName("sentences")]
                public List<Sentence> Sentences { get; set; }

                public class Sentence
                {
                    [JsonPropertyName("prefix")]
                    public string Prefix { get; set; }

                    [JsonPropertyName("text")]
                    public string Text { get; set; }
                }
            }
        }
    }
}

public class SplitTextReqParams
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; }

    [JsonPropertyName("params")]
    public RequestParams Params { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    // 构造函数
    public SplitTextReqParams(List<string> texts)
    {
        JsonRpc = "2.0";
        Method = "LMT_split_text";
        Id = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 100000000);
        Params = new RequestParams
        {
            Texts = texts,
            CommonJobParams = new _CommonJobParams
            {
                Mode = "translate",
                TextType = "plaintext"
            },
            Lang = new Language
            {
                LangUserSelected = "auto",
            }
        };
    }
    public class _CommonJobParams
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; }

        [JsonPropertyName("textType")]
        public required string TextType { get; set; }
    }
    public class Language
    {
        [JsonPropertyName("lang_user_selected")]
        public string LangUserSelected { get; set; }

        [JsonPropertyName("preference")]
        public Preference? Preference { get; set; }
    }
    public class Preference
    {
        [JsonPropertyName("weight")]
        public Dictionary<string, double> Weight { get; set; }

        [JsonPropertyName("default")]
        public string Default { get; set; }
    }
    public class RequestParams
    {
        [JsonPropertyName("texts")]
        public List<string> Texts { get; set; }

        [JsonPropertyName("commonJobParams")]
        public required _CommonJobParams CommonJobParams { get; set; }

        [JsonPropertyName("lang")]
        public required Language Lang { get; set; }
    }
}

public class DeepLTranslator : ITranslator
{
    private HttpClient client;
    private const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0";
    private const string apiEndPoint = "https://www2.deepl.com/jsonrpc?client=chrome-extension,1.33.0";
    public DeepLTranslator()
    {
        client = new HttpClient(UtilsFun.httpClientDefaultHandler);
        client.DefaultRequestHeaders.Host = "www2.deepl.com";
        client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        client.DefaultRequestHeaders.Connection.Add("keep-alive");
    }

    public SplitTextResp? SplitText(string rawText)
    {
        var body = new SplitTextReqParams(new List<string>{
            rawText
        });
        var response = this.client.PostAsJsonAsync(apiEndPoint, body, new JsonSerializerOptions
        {
            WriteIndented = true
        }).GetAwaiter().GetResult();
        var respDecode = response.Content.ReadFromJsonAsync<SplitTextResp>().GetAwaiter().GetResult();

        return respDecode;
    }


    public override DeepLTranslateResult? Translate(string src, string toLan, string? fromLan)
    {
        if (toLan == "auto")
            toLan = "en";
        if (fromLan == "auto")
            fromLan = null;
        var body = new DeepLTranslateRequestBody(new Params(toLan, fromLan, src));
        var bodyStr = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        var response = this.client.PostAsync(apiEndPoint, new StringContent(bodyStr, Encoding.Default, "application/json"))
                                            .GetAwaiter()
                                            .GetResult();
        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        Log.Info(src, typeof(DeepLTranslator), "translate raw");
        Log.Info(content, typeof(DeepLTranslator), "translate");
        var res = JsonSerializer.Deserialize<DeepLTranslateResult>(content);
        if (res != null && res.IsSuccess())
            return res;
        return null;
    }

    public override void Reset()
    {
        throw new NotImplementedException();
    }
}
