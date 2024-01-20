using System.Net.Http;
using Wox.Plugin;
using Translater.Utils;
namespace Translater.Suggest;

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
public class SuggestHelper
{
    private IPublicAPI api;
    private HttpClient client;
    public SuggestHelper(IPublicAPI api)
    {
        client = new HttpClient(UtilsFun.httpClientDefaultHandler);
        this.api = api;
    }
    public List<ResultItem> QuerySuggest(string query)
    {

        var data = new { kw = query };
        try
        {
            var res = client.PostAsync("https://fanyi.baidu.com/sug",
                                        new StringContent(data.toFormDataBodyString(),
                                        new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded")))
                                        .GetAwaiter()
                                        .GetResult();
            var sug = UtilsFun.ParseJson<SuggestInterface>(res.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            if (sug == null || sug.data == null || sug.data.Length == 0)
                return new List<ResultItem>();
            var result = new List<ResultItem>();
            foreach (var item in sug.data)
            {
                result.Add(new ResultItem
                {
                    Title = item.k,
                    SubTitle = $"{item.v}",
                    fromApiName = "Baidu",
                    transType = "[Suggest]"

                });

            }
            return result;
        }
        catch (Exception err)
        {
            return new List<ResultItem>{
                    new ResultItem{
                        Title = "some err happen in suggest",
                        SubTitle = err.Message
                    }
                };
        }
    }
}
