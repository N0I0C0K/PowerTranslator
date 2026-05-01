using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslatorExtension.Protocol;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace PowerTranslatorExtension.Utils
{
    public static partial class UtilsFun
    {
        private static readonly object MediaLock = new();
        private static MediaPlayer? _mediaPlayer;
        private static bool _isSpeaking;
        private const string ReadAloudEndpoint = "https://dict.youdao.com/dictvoice?audio={0}&le=zh";
        private const string NonWordAndNonChineseCharactersPattern = @"[^\w\s\p{IsCJKUnifiedIdeographs}]";

        public static HttpClientHandler httpClientDefaultHandler = new()
        {
            UseProxy = false,
        };

        public static Action? onHttpDefaultHandlerChange;

        public static string[] user_agents =
        {
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.71 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:34.0) Gecko/20100101 Firefox/34.0",
            "Opera/9.80 (Macintosh; Intel Mac OS X 10.6.8; U; en) Presto/2.8.131 Version/11.11",
        };

        public static string GetRandomUserAgent()
        {
            return user_agents[DateTime.Now.Millisecond % user_agents.Length];
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
                return JsonSerializer.Deserialize<T>(src);
            }
            catch (JsonException)
            {
                return default;
            }
        }

        public static void SetClipboardText(string s)
        {
            DataPackage package = new();
            package.SetText(s);
            Clipboard.SetContent(package);
            Clipboard.Flush();
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
            return $"{time:yyyy/MM/dd HH:mm:ss}:{time.Millisecond}";
        }

        public static string? GetClipboardText()
        {
            try
            {
                var package = Clipboard.GetContent();
                if (package.Contains(StandardDataFormats.Text))
                {
                    return package.GetTextAsync().AsTask().GetAwaiter().GetResult();
                }
            }
            catch (Exception err)
            {
                LogMessage(err.Message);
            }

            return string.Empty;
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
            var tar = string.Join("&", objs.Select(obj => obj.toFormDataBodyString()));
            return $"{src}?{tar}";
        }

        public static string toFormDataBodyString(this object src)
        {
            List<string> res = [];
            foreach (var key in src.GetType().GetProperties())
            {
                res.Add($"{key.Name}={src.GetType().GetProperty(key.Name)?.GetValue(src)}");
            }

            return string.Join("&", res);
        }

        public static List<ListItem> ToResultList(this IEnumerable<ResultItem> src, IconInfo? icon, SettingHelper? settingHelper = null, bool copyOnlyFirstOption = false)
        {
            return src.Select(item =>
            {
                ICommand command;
                if (item.Action != null)
                {
                    command = new AnonymousCommand(item.Action);
                }
                else
                {
                    command = new CopyTextCommand(NormalizeCopyTarget(item.CopyTgt ?? item.Title, copyOnlyFirstOption));
                }

                ListItem listItem = new(command)
                {
                    Title = item.Title,
                    Subtitle = item.SubTitle,
                    Icon = item.icon ?? icon,
                    TextToSuggest = item.TextToSuggest ?? string.Empty,
                };

                var moreCommands = new List<CommandContextItem>();
                if (!string.IsNullOrWhiteSpace(item.SubTitle))
                {
                    moreCommands.Add(new CommandContextItem(new CopyTextCommand(item.SubTitle) { Name = "Copy subtitle" }));
                }

                if (!string.IsNullOrWhiteSpace(item.Title))
                {
                    moreCommands.Add(new CommandContextItem("Read", action: () => ReadText(item.Title), result: CommandResult.KeepOpen()));
                }

                if (settingHelper?.enableJumpToDict == true && !string.IsNullOrWhiteSpace(item.Title))
                {
                    var url = string.Format(settingHelper.dictUtlPattern, Uri.EscapeDataString(item.Title));
                    moreCommands.Add(new CommandContextItem("Go to dictionary", action: () => OpenInShell(url), result: CommandResult.KeepOpen()));
                }

                if (moreCommands.Count > 0)
                {
                    listItem.MoreCommands = moreCommands.ToArray();
                }

                var detailLines = new List<string>();
                if (!string.IsNullOrWhiteSpace(item.Description))
                {
                    detailLines.Add(item.Description!);
                }
                else if (!string.IsNullOrWhiteSpace(item.SubTitle))
                {
                    detailLines.Add(item.SubTitle);
                }

                var footer = string.Join("-", new[] { item.transType, item.fromApiName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (!string.IsNullOrWhiteSpace(footer))
                {
                    detailLines.Add(string.Empty);
                    detailLines.Add(footer);
                }

                if (detailLines.Count > 0)
                {
                    listItem.Details = new Details
                    {
                        Title = item.Title,
                        Body = string.Join(Environment.NewLine, detailLines),
                    };
                }

                return listItem;
            }).ToList();
        }

        public static T? FirstNotNoneCast<S, T>(this IEnumerable<S> src, Func<S, T?> func)
        {
            foreach (var item in src)
            {
                var t = func(item);
                if (t != null)
                {
                    return t;
                }
            }

            return default;
        }

        public static IEnumerable<(int idx, S item)> Enumerate<S>(this IEnumerable<S> src)
        {
            var idx = 0;
            foreach (var item in src)
            {
                yield return (idx, item);
                idx++;
            }
        }

        public static void fixChinese(this Uri obj)
        {
            string url = obj.OriginalString;
            var type = obj.GetType();
            var property = type?.GetField("_info", BindingFlags.NonPublic | BindingFlags.Instance);
            var info = property?.GetValue(obj);
            if (info == null)
            {
                return;
            }

            var infoType = info.GetType();
            var offset = infoType.GetField("Offset")?.GetValue(info);
            var offsetType = offset?.GetType();

            offsetType?.GetField("End")?.SetValue(offset, (ushort)url.Length);
            offsetType?.GetField("Fragment")?.SetValue(offset, (ushort)url.Length);

            infoType.GetField("Offset")?.SetValue(info, offset);
            infoType.GetField("String")?.SetValue(info, url);

            type?.GetField("_string", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(obj, url);
            type?.GetField("_originalUnicodeString", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(obj, null);
        }

        public static string removeSpecialCharacter(this string str)
        {
            return Regex.Replace(str, NonWordAndNonChineseCharactersPattern, " ");
        }

        public static void ChangeDefaultHttpHandlerProxy(bool useSystemProxy, bool callEvent = true)
        {
            httpClientDefaultHandler = useSystemProxy
                ? new HttpClientHandler
                {
                    UseProxy = true,
                    Proxy = WebRequest.GetSystemWebProxy(),
                }
                : new HttpClientHandler
                {
                    UseProxy = false,
                };

            if (callEvent)
            {
                onHttpDefaultHandlerChange?.Invoke();
            }
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

        public static void LogMessage(string message)
        {
            ExtensionHost.LogMessage(new LogMessage { Message = message });
        }

        public static void OpenInShell(string target)
        {
            try
            {
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogMessage(ex.Message);
            }
        }

        public static void ReadText(string? txt)
        {
            if (string.IsNullOrWhiteSpace(txt))
            {
                return;
            }

            lock (MediaLock)
            {
                if (_isSpeaking)
                {
                    return;
                }

                _isSpeaking = true;
                _mediaPlayer?.Dispose();
                _mediaPlayer = new MediaPlayer();
                _mediaPlayer.MediaEnded += OnMediaPlaybackCompleted;
                _mediaPlayer.MediaFailed += OnMediaPlaybackFailed;
                _mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(string.Format(ReadAloudEndpoint, Uri.EscapeDataString(txt.removeSpecialCharacter()))));
                _mediaPlayer.Play();
            }
        }

        [GeneratedRegex(@"([A-Z])", RegexOptions.Compiled)]
        private static partial Regex CamelRegex();

        private static string NormalizeCopyTarget(string textToCopy, bool copyOnlyFirstOption)
        {
            if (copyOnlyFirstOption && (textToCopy.Contains(';') || textToCopy.Contains('；')))
            {
                return textToCopy.Split([';', '；'], StringSplitOptions.None)[0].Trim();
            }

            return textToCopy;
        }

        private static void OnMediaPlaybackCompleted(MediaPlayer sender, object args)
        {
            ResetSpeaker(sender);
        }

        private static void OnMediaPlaybackFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            LogMessage(args.ErrorMessage);
            ResetSpeaker(sender);
        }

        private static void ResetSpeaker(MediaPlayer sender)
        {
            lock (MediaLock)
            {
                sender.MediaEnded -= OnMediaPlaybackCompleted;
                sender.MediaFailed -= OnMediaPlaybackFailed;
                sender.Dispose();
                if (ReferenceEquals(_mediaPlayer, sender))
                {
                    _mediaPlayer = null;
                }

                _isSpeaking = false;
            }
        }
    }
}
