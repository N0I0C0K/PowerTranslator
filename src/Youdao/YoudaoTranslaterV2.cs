using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Translater.Utils;
using Wox.Plugin.Logger;

namespace Translater.Youdao.V2;

public class KeyResponse
{
    public class Data
    {
        public string? secretKey { get; set; }
    }
    public int code { get; set; }
    public Data? data { get; set; }
}

public class TranslateResponse : ITranslateResult
{
    public struct TranDictResult
    {
        public struct Ce
        {
            public struct Word
            {
                public struct Trs
                {
                    public string? voice { get; set; }
                    [JsonPropertyName("#text")]
                    public string? text { get; set; }
                    [JsonPropertyName("#tran")]
                    public string? tran { get; set; }
                }
                public Trs[]? trs { get; set; }
                public string? phone { get; set; }
                [JsonPropertyName("return-phrase")]
                public string return_phrase { get; set; }
            }
            public Word? word { get; set; }
        }
        public struct Ec
        {
            public struct Word
            {
                public struct Trs
                {
                    public string? pos { get; set; }
                    public string? tran { get; set; }
                }
                public Trs[]? trs { get; set; }
                public string? usphone { get; set; }
                [JsonPropertyName("return-phrase")]
                public string return_phrase { get; set; }
            }
            public string[]? exam_type { get; set; }
            public Word? word { get; set; }
        }
        public Ce? ce { get; set; }
        public Ec? ec { get; set; }
    }

    public struct TranslateResult
    {
        public string tgt { get; set; }
        public string src { get; set; }
        public string? srcPronounce { get; set; }
        public string? tgtPronounce { get; set; }
    }

    public int code { get; set; }
    public TranDictResult? dictResult { get; set; }
    public TranslateResult[][]? translateResult { get; set; }
    public string? type { get; set; }

    public override IEnumerable<ResultItem>? Transform()
    {
        if (this.code != 0)
            return null;
        List<ResultItem> res = new List<ResultItem>();
        foreach (var tres in this.translateResult![0])
        {
            string? srcpron = tres.srcPronounce ?? dictResult?.ec?.word?.usphone;
            string? tgtpron = tres.tgtPronounce;
            res.Add(new ResultItem
            {
                Title = tres.tgt + (tres.tgt.Length < 10 && tgtpron != null ? $" ({tgtpron})" : ""),
                SubTitle = tres.src + (srcpron != null ? $" ({srcpron})" : ""),
                transType = this.type ?? "unknow type",
                CopyTgt = tres.tgt,
                Description = $"{tres.tgt} {(tgtpron != null ? $"({tgtpron})" : "")}\n\n{tres.src} {(srcpron != null ? $" ({srcpron})" : "")}"
            });
        }
        if (this.dictResult != null)
        {
            if (this.dictResult?.ce != null)
            {
                var ce = this.dictResult?.ce;
                if (ce?.word?.trs != null)
                {
                    foreach (var trs in ce?.word?.trs!)
                    {
                        res.Add(new ResultItem
                        {
                            Title = trs.text ?? "[None]",
                            SubTitle = trs.tran ?? "",
                            transType = "[Smart Result]"
                        });
                    }
                }
            }
            else if (this.dictResult?.ec != null)
            {
                var ec = this.dictResult?.ec;
                if (ec?.word?.trs != null)
                {
                    foreach (var trs in ec?.word?.trs!)
                    {
                        res.Add(new ResultItem
                        {
                            Title = trs.tran ?? "[None]",
                            SubTitle = trs.pos ?? ""
                        });
                    }
                }
                if (ec?.exam_type != null)
                {
                    res.Add(new ResultItem
                    {
                        Title = String.Join(" | ", ec?.exam_type!),
                        SubTitle = "exam",
                        transType = "exam"
                    });
                }
            }
        }
        res.each((val) =>
        {
            val.fromApiName = "Youdao Web Api";
        });
        return res;
    }
}

public class YoudaoTranslater : ITranslater
{
    private HttpClient client;
    private Random random;
    private string? secretKey;
    private MD5 md5;
    private byte[]? encryptKey;
    private byte[]? iv;
    private string userAgent;

    public YoudaoTranslater()
    {

        this.userAgent = UtilsFun.GetRandomUserAgent();

        this.random = new Random();
        this.md5 = MD5.Create();

        client = new HttpClient(UtilsFun.httpClientDefaultHandler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        client.DefaultRequestHeaders.Add("User-Agent", userAgent);
        client.DefaultRequestHeaders.Add("Referer", "https://fanyi.youdao.com/");
        client.DefaultRequestHeaders.Add("Origin", "https://fanyi.youdao.com");

        SetCookies();

        var res = this.client.GetAsync("https://dict.youdao.com/webtranslate/key".addQueryParameters(new
        {
            keyid = "webfanyi-key-getter"
        },
        this.BaseBody("asdjnjfenknafdfsdfsd"))).GetAwaiter().GetResult();
        var keyRes = JsonSerializer.Deserialize<KeyResponse>(res.Content.ReadAsStringAsync().GetAwaiter().GetResult());

        if (keyRes?.code != 0)
        {
            Log.Error($"err in get secretKey, {keyRes?.ToString()}", typeof(YoudaoTranslater));
            throw new Exception("err in get secretKey");
        }

        this.secretKey = keyRes.data!.secretKey;
        this.InitEncrypt();

    }

    private void InitEncrypt()
    {
        var GetKeyByte = (string key, byte[] buf) =>
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            var res = md5.ComputeHash(bytes);
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = res[i];
            }
        };
        this.encryptKey = new byte[16];
        this.iv = new byte[16];
        GetKeyByte("ydsecret://query/key/B*RGygVywfNBwpmBaZg*WT7SIOUP2T0C9WHMZN39j^DAdaZhAnxvGcCY6VYFwnHl", this.encryptKey);
        GetKeyByte("ydsecret://query/iv/C@lZe2YzHtZ2CYgaXKSVfsb7Y4QWHjITPPZ0nQp87fBeJ!Iv6v^6fvi2WN@bYpJ4", this.iv);
    }

    public override TranslateResponse? Translate(string src, string toLan = "auto", string fromLan = "auto")
    {
        var data = new
        {
            i = src,
            from = fromLan,
            to = toLan,
            dictResult = true,
            keyid = "webfanyi",
        }.toFormDataBodyString();
        var baseBody = this.BaseBody(this.secretKey!).toFormDataBodyString();
        var res = this.client.PostAsync("https://dict.youdao.com/webtranslate",
                                        new StringContent($"{data}&{baseBody}",
                                        new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded")))
                                        .GetAwaiter()
                                        .GetResult();

        var content = this.Decrypt(res.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        return JsonSerializer.Deserialize<TranslateResponse>(content);
    }

    private long UtcNow()
    {
        return DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
    private void SetCookies()
    {
        string OUTFOX_SEARCH_USER_ID_NCOO = $"OUTFOX_SEARCH_USER_ID_NCOO={random.Next(100000000, 999999999)}.{random.Next(100000000, 999999999)}";
        string OUTFOX_SEARCH_USER_ID = $"OUTFOX_SEARCH_USER_ID={random.Next(100000000, 999999999)}@{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}";
        client.DefaultRequestHeaders.Add("Cookie", $"{OUTFOX_SEARCH_USER_ID_NCOO};{OUTFOX_SEARCH_USER_ID}");
    }

    private string md5Encrypt(string src)
    {
        var bytes = Encoding.UTF8.GetBytes(src);
        var res = md5.ComputeHash(bytes);
        var resStr = new StringBuilder();
        foreach (var item in res)
        {
            resStr.Append(item.ToString("x2"));
        }
        return resStr.ToString();
    }

    private string sign(string t, string key)
    {
        return this.md5Encrypt($"client=fanyideskweb&mysticTime={t}&product=webfanyi&key={key}");
    }

    private Object BaseBody(string key)
    {
        var now = this.UtcNow().ToString();
        return new
        {
            sign = sign(now, key),
            client = "fanyideskweb",
            product = "webfanyi",
            appVersion = "1.0.0",
            vendor = "web",
            pointParam = "client,mysticTime,product",
            mysticTime = now,
            keyfrom = "fanyi.web",
        };
    }
    private string Decrypt(string src)
    {
        if (this.encryptKey == null || this.iv == null)
            throw new Exception("No key was initialized");
        string res;
        using (Aes cryptor = Aes.Create())
        {
            cryptor.Key = this.encryptKey;
            cryptor.IV = this.iv;
            var decryptor = cryptor.CreateDecryptor(this.encryptKey!, this.iv);
            using (MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(src.Replace("-", "+").Replace("_", "/"))))
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader reader = new StreamReader(cryptoStream))
                    {
                        res = reader.ReadToEnd();
                    }
                }
            }
        }
        return res;
    }

    public override void Reset()
    {
        string OUTFOX_SEARCH_USER_ID_NCOO = $"OUTFOX_SEARCH_USER_ID_NCOO={random.Next(100000000, 999999999)}.{random.Next(100000000, 999999999)}";
        string OUTFOX_SEARCH_USER_ID = $"OUTFOX_SEARCH_USER_ID={random.Next(100000000, 999999999)}@{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}";
        client.DefaultRequestHeaders.Add("Cookie", $"{OUTFOX_SEARCH_USER_ID_NCOO};{OUTFOX_SEARCH_USER_ID}");
    }

}
