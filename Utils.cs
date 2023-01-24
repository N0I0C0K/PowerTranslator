using Wox.Plugin.Logger;
using System.Windows;

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
        public static void SetClipboardText(string s)
        {
            Clipboard.SetDataObject(s);
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
                    else
                        Log.Error("can not find any text in clipboard", typeof(UtilsFun));
                });
                return res;
            }
            catch (Exception err)
            {
                Log.Error(err.Message, typeof(UtilsFun));
                return string.Empty;
            }
        }
    }
}