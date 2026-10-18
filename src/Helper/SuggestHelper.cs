using System.Net.Http;
using Wox.Plugin;
using Translator.Utils;
namespace Translator.Suggest;

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
public class SuggestHelper : IDisposable
{
    private HttpClient client;
    private readonly Func<HttpClient> createClient;
    private bool disposed;

    public SuggestHelper(IPublicAPI api)
        : this(() => new HttpClient(UtilsFun.httpClientDefaultHandler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromSeconds(5),
        })
    {
    }

    internal SuggestHelper(Func<HttpClient> createClient)
    {
        this.createClient = createClient;
        client = createClient();
        UtilsFun.onHttpDefaultHandlerChange += Reload;
    }

    private void Reload()
    {
        Interlocked.Exchange(ref client, createClient()).Dispose();
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        UtilsFun.onHttpDefaultHandlerChange -= Reload;
        client.Dispose();
    }

    public List<ResultItem> QuerySuggest(string query)
    {

        try
        {
            using var body = new FormUrlEncodedContent(new Dictionary<string, string> { ["kw"] = query });
            using var res = client.PostAsync("https://fanyi.baidu.com/sug", body)
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
                    Title = item.v,
                    SubTitle = $"{item.k}",
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
