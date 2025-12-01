using System.Text.Json;
using System.Reflection;
using System.Net.Http;
using System.Net;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using PowerTranslatorExtension.Protocol;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace PowerTranslatorExtension.Utils
{
    public static partial class UtilsFun
    {
        public static HttpClientHandler httpClientDefaultHandler = new()
        {
            UseProxy = false
        };
        public static Action? onHttpDefaultHandlerChange;
        public static string[] user_agents = {
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.71 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:34.0) Gecko/20100101 Firefox/34.0",
            "Opera/9.80 (Macintosh; Intel Mac OS X 10.6.8; U; en) Presto/2.8.131 Version/11.11"
        };
        public static string GetRandomUserAgent()
        {
            return UtilsFun.user_agents[DateTime.Now.Millisecond % UtilsFun.user_agents.Length];
        }
        public static bool WhetherTranslate(string? s)
        {
            return s != null && s.Length > 0 && !s.Contains('\\') && !s.Contains('/');
        }
        public static string ToEnPunctuation(this string src)
        {
            return src.Replace('，', ',').Replace('；', ';').Replace('（', '(').Replace('）', ')');
        }
        public static T? ParseJson<T>(string src)
        {
            try
            {
                var res = JsonSerializer.Deserialize<T>(src);
                return res;
            }
            catch (JsonException)
            {
                return default(T);
            }
        }
        public static void SetClipboardText(string s)
        {
            throw new NotImplementedException();
        }
        public static long GetUtcTimeNow()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
        public static long GetNowTicksMilliseconds()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }
        public static string ToFormateTime(this long ticks)
        {
            throw new NotImplementedException();
        }
        public static string? GetClipboardText()
        {
            throw new NotImplementedException();
        }

        public static void each<T>(this IEnumerable<T> src, Action<T> action)
        {
            foreach (var item in src)
            {
                action(item);
            }
        }
        public static string addQueryParameters(this string src, params object[] objs)
        {
            string tar = string.Join("&", objs.Select((obj) =>
            {
                return obj.toFormDataBodyString();
            }));

            return $"{src}?{tar}";
        }
        public static string toFormDataBodyString(this object src)
        {
            var res = new List<string>();
            foreach (var key in src.GetType().GetProperties())
            {
                res.Add($"{key.Name}={src.GetType().GetProperty(key.Name)?.GetValue(src)}");
            }
            return string.Join("&", res);
        }
        public static List<ListItem> ToResultList(this IEnumerable<ResultItem> src, IconInfo? icon)
        {
            return src.Select((item, idx) =>
            {
                return new ListItem
                {
                    Title = item.Title,
                    Subtitle = item.SubTitle,
                    Icon = item.icon ?? icon,
                    Command = new CopyTextCommand(item.CopyTgt ?? item.Title),
                };
            }).ToList();
        }

        public static T? FirstNotNoneCast<S, T>(this IEnumerable<S> src, Func<S, T?> func)
        {
            foreach (var item in src)
            {
                var t = func(item);
                if (t != null)
                    return t;
            }
            return default(T);
        }

        public static IEnumerable<(int idx, S item)> Enumerate<S>(this IEnumerable<S> src)
        {
            int idx = 0;
            foreach (var item in src)
            {
                yield return (idx, item);
                idx++;
            }
        }

        /// <summary>
        /// Fixed Chinese address problems caused by URIs
        /// In the Uri, the Chinese is automatically decoded and MediaPlayer cannot load the correct url
        /// </summary>
        /// <param name="obj"></param>
        public static void fixChinese(this Uri obj)
        {
            string url = obj.OriginalString;
            var type = obj.GetType();
            var property = type?.GetField("_info", BindingFlags.NonPublic | BindingFlags.Instance);
            var info = property?.GetValue(obj);
            if (info == null)
                return;
            var infoType = info.GetType();
            var offset = infoType?.GetField("Offset")?.GetValue(info);
            var offsetType = offset?.GetType();

            offsetType?.GetField("End")?.SetValue(offset, (ushort)url.Length);
            offsetType?.GetField("Fragment")?.SetValue(offset, (ushort)url.Length);

            infoType?.GetField("Offset")?.SetValue(info, offset);
            infoType?.GetField("String")?.SetValue(info, url);

            type?.GetField("_string", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(obj, url);
            type?.GetField("_originalUnicodeString", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(obj, null);
        }
        public static string removeSpecialCharacter(this string str)
        {
            string pattern = @"[^\w\s\u4e00-\u9fa5]";
            return Regex.Replace(str, pattern, " ");
        }

        public static void ChangeDefaultHttpHandlerProxy(bool useSystemProxy, bool callEvent = true)
        {
            if (useSystemProxy)
            {
                UtilsFun.httpClientDefaultHandler = new HttpClientHandler
                {
                    UseProxy = true,
                    Proxy = WebRequest.GetSystemWebProxy()
                };
            }
            else
            {
                UtilsFun.httpClientDefaultHandler = new HttpClientHandler
                {
                    UseProxy = false,
                };
            }
            if (callEvent)
                onHttpDefaultHandlerChange?.Invoke();
        }

        public static string ConvertSnakeCaseOrCamelCaseToNormalSpace(string src)
        {
            if (src.Contains(' '))
            {
                return src;
            }
            if (src.Contains('_'))
            {
                return src.Replace("_", " ");
            }
            return CamelRegex().Replace(src, " $1").TrimStart();
        }

        [GeneratedRegex(@"([A-Z])", RegexOptions.Compiled)]
        private static partial Regex CamelRegex();

        public static void LogMessage(string message)
        {
            ExtensionHost.LogMessage(new LogMessage
            {
                Message = message,
            });
        }
    }
}