using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CmdPalTranslator = PowerTranslatorExtension.Protocol.ITranslator;
using CmdPalUtils = PowerTranslatorExtension.Utils.UtilsFun;
using LegacyTranslator = Translator.Protocol.ITranslator;
using LegacyUtils = Translator.Utils.UtilsFun;

namespace PowerTranslatorExtension.Tests;

// These tests call public services. Run explicitly with TestCategory=EngineHealth.
[TestClass]
[TestCategory("EngineHealth")]
[DoNotParallelize]
public class EngineHealthTests
{
    public TestContext TestContext { get; set; } = null!;

    // Discover implementations in both assemblies so new engines are checked too,
    // including Alibaba, which is currently outside the default fallback chain.
    public static IEnumerable<object[]> Engines()
    {
        foreach (var contract in new[] { typeof(LegacyTranslator), typeof(CmdPalTranslator) })
        {
            foreach (var type in contract.Assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && contract.IsAssignableFrom(type))
                .OrderBy(type => type.FullName))
            {
                yield return [type.FullName!];
            }
        }
    }

    [TestMethod]
    [DynamicData(nameof(Engines), DynamicDataSourceType.Method)]
    [Timeout(210_000)]
    public async Task Engine_ReturnsEnglishTranslation(string engineType)
    {
        // Keep ordinary local test runs offline, even without a category filter.
        if (Environment.GetEnvironmentVariable("RUN_ENGINE_HEALTH_CHECKS") != "1")
        {
            Assert.Inconclusive("Set RUN_ENGINE_HEALTH_CHECKS=1 to call live translation services.");
        }

        var type = typeof(LegacyTranslator).Assembly.GetType(engineType)
            ?? typeof(CmdPalTranslator).Assembly.GetType(engineType)
            ?? throw new InvalidOperationException($"Unknown engine: {engineType}");
        var failures = new List<string>();

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            // Some engines dispose their shared handler. Give every attempt its own
            // handler, and dispose it on timeout to cancel outstanding HTTP requests.
            using var handler = new HttpClientHandler { UseProxy = false };
            var isLegacy = typeof(LegacyTranslator).IsAssignableFrom(type);
            var previousHandler = isLegacy ? LegacyUtils.httpClientDefaultHandler : CmdPalUtils.httpClientDefaultHandler;
            if (isLegacy)
                LegacyUtils.httpClientDefaultHandler = handler;
            else
                CmdPalUtils.httpClientDefaultHandler = handler;

            try
            {
                var translation = await Task.Run(() => Translate(type))
                    .WaitAsync(TimeSpan.FromSeconds(60));
                Assert.IsFalse(string.IsNullOrWhiteSpace(translation), "No translation returned.");
                Assert.IsTrue(Regex.IsMatch(translation, @"\bapples?\b", RegexOptions.IgnoreCase),
                    $"Expected an English translation of 苹果, received: {translation}");
                TestContext.WriteLine($"{engineType}: 苹果 -> {translation} (attempt {attempt})");
                return;
            }
            catch (Exception exception)
            {
                // WaitAsync only stops waiting; cancel HTTP before the next attempt.
                handler.Dispose();
                var failure = $"Attempt {attempt}: {exception.GetBaseException().Message}";
                failures.Add(failure);
                TestContext.WriteLine(failure);
            }
            finally
            {
                if (isLegacy)
                    LegacyUtils.httpClientDefaultHandler = previousHandler;
                else
                    CmdPalUtils.httpClientDefaultHandler = previousHandler;
            }

            if (attempt < 3)
                await Task.Delay(TimeSpan.FromSeconds(3));
        }

        Assert.Fail($"{engineType} failed all three live translation attempts.\n{string.Join("\n", failures)}");
    }

    private static string? Translate(Type type)
    {
        var engine = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Cannot create {type.FullName}");
        try
        {
            // Call the same positional contract as TranslateHelper, with no fallback.
            if (engine is LegacyTranslator legacy)
                return legacy.Translate("苹果", "en", "auto")?.Transform()?.FirstOrDefault()?.Title;

            var cmdpal = (CmdPalTranslator)engine;
            cmdpal.Init();
            Assert.IsTrue(cmdpal.Inited, "Engine did not initialize.");
            return cmdpal.Translate("苹果", "en", "auto")?.Transform()?.FirstOrDefault()?.Title;
        }
        finally
        {
            (engine as IDisposable)?.Dispose();
        }
    }
}
