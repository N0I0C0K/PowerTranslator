using System.Net.Http;
using Wox.Plugin;
using Translater.Utils;
namespace Translater.Suggest
{
    public class SuggestHelper
    {
        public class SuggestInterface
        {
            public struct SuggestItem
            {
                public string k { get; set; }
                public string v { get; set; }
            }
            public int errno { get; set; }
            public SuggestItem[]? data { get; set; }

        }
        private HttpClient client;
        public SuggestHelper()
        {
            client = new HttpClient();
        }
        public void QuerySuggest(string query, List<Result> results)
        {
            var data = new { kw = query };
            var res = client.PostAsync("https://fanyi.baidu.com/sug",
                                        new StringContent(data.toFormDataBodyString(),
                                        new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded")))
                                        .GetAwaiter()
                                        .GetResult();
            var sug = UtilsFun.ParseJson<SuggestInterface>(res.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            if (sug == null || sug.data == null || sug.data.Length == 0)
                return;
            foreach (var item in sug.data)
            {
                results.Add(new Result
                {
                    Title = item.k,
                    SubTitle = item.v
                });
            }
        }
    }
}