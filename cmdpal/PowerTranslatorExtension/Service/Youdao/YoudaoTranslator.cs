using System;
        using System.Collections.Generic;
        using System.Net.Http;
        using System.Security.Cryptography;
        using System.Text;
        using System.Text.Json;
        using PowerTranslatorExtension.Protocol;
        using PowerTranslatorExtension.Service.Youdao.Utils;
        using PowerTranslatorExtension.Utils;

        namespace PowerTranslatorExtension.Service.Youdao.old;

        public class TranslateResponse : ITranslateResult
        {
            public struct ResStruct
            {
                public string tgt { get; set; }
                public string src { get; set; }
            }

            public struct Entry
            {
                public string[]? entries { get; set; }
                public int type { get; set; }
            }

            public int errorCode { get; set; }
            public ResStruct[][]? translateResult { get; set; }
            public string? type { get; set; }
            public Entry? smartResult { get; set; }

            public IEnumerable<ResultItem>? Transform()
            {
                if (errorCode != 0 || translateResult == null || translateResult.Length == 0 || translateResult[0].Length == 0)
                {
                    return null;
                }

                List<ResultItem> res =
                [
                    new ResultItem
                    {
                        Title = translateResult[0][0].tgt,
                        SubTitle = translateResult[0][0].src,
                        transType = type ?? "Translate",
                        CopyTgt = translateResult[0][0].tgt,
                    },
                ];

                smartResult?.entries?.each(s =>
                {
                    var text = s.Replace("\r\n", " ").TrimStart().Replace(" ", string.Empty);
                    if (string.IsNullOrEmpty(text))
                    {
                        return;
                    }

                    res.Add(new ResultItem
                    {
                        Title = text,
                        SubTitle = "[smart result]",
                    });
                });

                res.each(val => val.fromApiName = "Youdao Old Web Api");
                return res;
            }
        }

        public sealed class YoudaoTranslator : ITranslator
        {
            private HttpClient? _client;
            private readonly Random _random = new();
            private readonly MD5 _md5 = MD5.Create();
            private string _userAgent;

            public bool Inited { get; private set; }
            public string Name => "Youdao Old Web Api";

            public YoudaoTranslator()
            {
                _userAgent = UtilsFun.GetRandomUserAgent();
                _client = CreateClient(_userAgent, _random);
            }

            public void Init()
            {
                Inited = true;
            }

            public ITranslateResult? Translate(string src, string toLan = "AUTO", string fromLan = "AUTO")
            {
                if (!Inited || _client == null)
                {
                    return null;
                }

                toLan = YoudaoUtils.Instance.GetYoudaoLanguageCode(toLan);
                fromLan = YoudaoUtils.Instance.GetYoudaoLanguageCode(fromLan);

                var ts = UtilsFun.GetUtcTimeNow().ToString();
                var salt = $"{ts}{_random.Next(0, 9)}";
                var bv = Md5Encrypt(_userAgent);
                var sign = Md5Encrypt($"fanyideskweb{src}{salt}Ygy_4c=r#e#4EX^NUGUc5");
                var data = new
                {
                    i = src,
                    from = fromLan,
                    to = toLan,
                    smartresult = "dict",
                    client = "fanyideskweb",
                    salt,
                    sign,
                    lts = ts,
                    bv,
                    doctype = "json",
                    version = "2.1",
                    keyfrom = "fanyi.web",
                    action = "FY_BY_REALTlME",
                };
                var res = _client.PostAsync(
                    "https://fanyi.youdao.com/translate_o?smartresult=dict&smartresult=rule",
                    new StringContent(data.toFormDataBodyString(), new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded")))
                    .GetAwaiter()
                    .GetResult();
                var contentStr = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return JsonSerializer.Deserialize<TranslateResponse>(contentStr);
            }

            public void Reset()
            {
                _client?.Dispose();
                _userAgent = UtilsFun.GetRandomUserAgent();
                _client = CreateClient(_userAgent, _random);
                Inited = true;
            }

            private string Md5Encrypt(string src)
            {
                var bytes = Encoding.UTF8.GetBytes(src);
                var res = _md5.ComputeHash(bytes);
                StringBuilder resStr = new();
                foreach (var item in res)
                {
                    resStr.Append(item.ToString("x2"));
                }

                return resStr.ToString();
            }

            private static HttpClient CreateClient(string userAgent, Random random)
            {
                var client = new HttpClient(UtilsFun.httpClientDefaultHandler)
                {
                    Timeout = TimeSpan.FromSeconds(10),
                };
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                client.DefaultRequestHeaders.Add("Referer", "https://fanyi.youdao.com/");
                client.DefaultRequestHeaders.Add("Origin", "https://fanyi.youdao.com");
                string ncoo = $"OUTFOX_SEARCH_USER_ID_NCOO={random.Next(100000000, 999999999)}.{random.Next(100000000, 999999999)}";
                string userId = $"OUTFOX_SEARCH_USER_ID={random.Next(100000000, 999999999)}@{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}";
                client.DefaultRequestHeaders.Add("Cookie", $"{ncoo};{userId}");
                return client;
            }
        }
