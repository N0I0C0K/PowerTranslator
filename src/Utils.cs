using Wox.Plugin.Logger;
using System.Windows;
using System.Text.Json;

namespace Translater.Utils
{
    public static class UtilsFun
    {
        /// <summary>
        /// It is used only to determine whether the contents of the clipboard need to be translated
        /// </summary>
        /// <param name="s">string</param>
        /// <returns></returns>
        public static bool WhetherTranslate(string? s)
        {
            return s != null && s.Length > 0 && !s.Contains("\\") && !s.Contains("/");
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
            Clipboard.SetDataObject(s);
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
            var time = new DateTime(ticks * TimeSpan.TicksPerMillisecond);
            return String.Format("{0:yyyy/MM/dd hh:mm:ss}:{1}", time, time.Millisecond);
        }
        public static string? GetClipboardText()
        {
            try
            {
                string? res = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Clipboard.ContainsText())
                        res = Clipboard.GetText();
                });
                return res;
            }
            catch (Exception err)
            {
                Log.Error(err.Message, typeof(UtilsFun));
                return string.Empty;
            }
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
        public static List<Wox.Plugin.Result> ToResultList(this IEnumerable<ResultItem> src, string iconPath)
        {
            return src.Select((item, idx) =>
            {
                return new Wox.Plugin.Result
                {
                    Title = item.Title,
                    SubTitle = item.SubTitle,
                    Action = item.Action != null ? item.Action :
                    (e) =>
                    {
                        UtilsFun.SetClipboardText(item.Title);
                        return true;
                    },
                    IcoPath = iconPath
                };
            }).ToList();
        }
    }
}