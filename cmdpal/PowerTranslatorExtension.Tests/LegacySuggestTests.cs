using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LegacyUtils = Translator.Utils.UtilsFun;

namespace PowerTranslatorExtension.Tests;

[TestClass]
public class LegacySuggestTests
{
    [TestMethod]
    public void PublicConstructor_RefreshesClientWhenProxyChanges()
    {
        var helper = new Translator.Suggest.SuggestHelper((Wox.Plugin.IPublicAPI)null!);
        try
        {
            var field = typeof(Translator.Suggest.SuggestHelper).GetField("client", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var before = (HttpClient)field.GetValue(helper)!;
            LegacyUtils.onHttpDefaultHandlerChange?.Invoke();
            var after = (HttpClient)field.GetValue(helper)!;

            Assert.AreNotSame(before, after, "Search suggestions must use a client built with the new proxy handler.");
        }
        finally
        {
            // Also permits this regression to run against the original plugin DLL.
            helper.GetType().GetMethod("Dispose", Type.EmptyTypes)?.Invoke(helper, null);
        }
    }

    [TestMethod]
    public void QuerySuggest_EncodesTextAsOneFormValue()
    {
        var handler = new SuggestHandler();
        using var helper = new Translator.Suggest.SuggestHelper(() => new HttpClient(handler));

        var results = helper.QuerySuggest("fish & chips=two+plus");

        Assert.AreEqual("kw=fish+%26+chips%3Dtwo%2Bplus", handler.Body);
        Assert.HasCount(1, results);
        Assert.AreEqual("鱼", results[0].Title);
    }

    [TestMethod]
    public void PublicConstructor_EncodesSpecialCharactersAsOneQuery()
    {
        var helper = new Translator.Suggest.SuggestHelper((Wox.Plugin.IPublicAPI)null!);
        var field = typeof(Translator.Suggest.SuggestHelper).GetField("client", BindingFlags.Instance | BindingFlags.NonPublic)!;
        using var originalClient = (HttpClient)field.GetValue(helper)!;
        var handler = new SuggestHandler();
        using var client = new HttpClient(handler);
        field.SetValue(helper, client);
        try
        {
            _ = helper.QuerySuggest("fish & chips=two+plus");

            Assert.AreEqual("kw=fish+%26+chips%3Dtwo%2Bplus", handler.Body);
        }
        finally
        {
            helper.GetType().GetMethod("Dispose", Type.EmptyTypes)?.Invoke(helper, null);
        }
    }

    [TestMethod]
    public void QuerySuggest_UsesNewClientAfterProxySettingsChange()
    {
        var handlers = new List<SuggestHandler>();
        using var helper = new Translator.Suggest.SuggestHelper(() =>
        {
            var handler = new SuggestHandler();
            handlers.Add(handler);
            return new HttpClient(handler);
        });

        _ = helper.QuerySuggest("before");
        LegacyUtils.onHttpDefaultHandlerChange?.Invoke();
        _ = helper.QuerySuggest("after");

        Assert.HasCount(2, handlers);
        Assert.IsTrue(handlers[0].Disposed);
        Assert.AreEqual("kw=before", handlers[0].Body);
        Assert.AreEqual("kw=after", handlers[1].Body);
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromProxySettingsChanges()
    {
        var created = 0;
        var helper = new Translator.Suggest.SuggestHelper(() =>
        {
            created++;
            return new HttpClient(new SuggestHandler());
        });

        helper.Dispose();
        helper.Dispose();
        LegacyUtils.onHttpDefaultHandlerChange?.Invoke();

        Assert.AreEqual(1, created);
    }

    private sealed class SuggestHandler : HttpMessageHandler
    {
        public string? Body { get; private set; }
        public bool Disposed { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"errno\":0,\"data\":[{\"k\":\"fish\",\"v\":\"鱼\"}]}"),
            };
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
