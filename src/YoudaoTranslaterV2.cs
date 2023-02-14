using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Translater.Utils;

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

public class TranslateResponse
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

}

public class YoudaoTranslater
{
    private HttpClient client;
    private const string userAgent = "Mozilla/5.0 (X11; CrOS i686 3912.101.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.116 Safari/537.36";
    private Random random;
    private string? secretKey;
    private MD5 md5;
    private byte[]? encryptKey;
    private byte[]? iv;

    public YoudaoTranslater()
    {
        this.random = new Random();
        this.md5 = MD5.Create();

        client = new HttpClient();
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
            throw new Exception("err in get secretKey");

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


    public TranslateResponse? Translate(string src, string toLan = "auto", string fromLan = "auto")
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
        var res = client.GetAsync("https://rlogs.youdao.com/rlog.php".addQueryParameters(new
        {
            _npid = "fanyiweb",
            _ncat = "pageview",
            _ncoo = (2147483647 / this.random.Next(1, 10)).ToString(),
            nssn = "NULL",
            _nver = "1.2.0",
            _ntms = this.UtcNow().ToString(),
            _nhrf = "newweb_translate_text"
        })).GetAwaiter().GetResult();
        client.DefaultRequestHeaders.Add("cookies", res.Headers.GetValues("Set-Cookie").First());
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

}
