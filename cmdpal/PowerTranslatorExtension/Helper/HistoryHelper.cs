using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PowerTranslatorExtension.Protocol;

namespace PowerTranslatorExtension.History;

public class HistoryHelper
{
    public const int MAX_HISTORY_NUM = 20;

    private readonly Queue<ResultItem> history = new();
    private readonly object _lock = new();
    private uint mark = 0;

    private static HistoryHelper? _instance;
    public static HistoryHelper Instance => _instance ??= new HistoryHelper();

    private HistoryHelper() { }

    public void Push(ResultItem item)
    {
        if (string.IsNullOrEmpty(item.Title) || string.IsNullOrEmpty(item.SubTitle))
            return;

        uint _t;
        lock (_lock)
        {
            _t = ++mark;
        }
        Task.Delay(1000).ContinueWith(_ =>
        {
            lock (_lock)
            {
                if (_t != mark) return;
                history.Enqueue(new ResultItem
                {
                    Title = item.Title,
                    SubTitle = $"{item.SubTitle} [{DateTime.Now:MM/dd HH:mm:ss}]",
                    transType = "[History]",
                });
                while (history.Count > MAX_HISTORY_NUM)
                    history.Dequeue();
            }
        });
    }

    public IEnumerable<ResultItem> Query()
    {
        lock (_lock)
        {
            return history.Reverse().ToList();
        }
    }
}
