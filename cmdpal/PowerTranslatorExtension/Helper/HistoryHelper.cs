using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslatorExtension.Protocol;

namespace PowerTranslatorExtension;

public sealed class HistoryHelper
{
    private const int MaxHistoryCount = 15;
    private readonly Queue<ResultItem> _history = [];
    private readonly object _lock = new();
    private readonly IconInfo _historyIcon = IconHelpers.FromRelativePaths("Assets/history.light.png", "Assets/history.dark.png");
    private uint _mark;

    public void Push(ResultItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.SubTitle))
        {
            return;
        }

        uint marker;
        lock (_lock)
        {
            marker = ++_mark;
        }

        Task.Delay(1000).ContinueWith(_ =>
        {
            lock (_lock)
            {
                if (marker != _mark)
                {
                    return;
                }

                _history.Enqueue(new ResultItem
                {
                    Title = item.Title,
                    SubTitle = $"{item.SubTitle} [{DateTime.Now:MM/dd HH:mm:ss}]",
                    icon = _historyIcon,
                    transType = "[History]",
                    CopyTgt = item.Title,
                });

                while (_history.Count > MaxHistoryCount)
                {
                    _history.Dequeue();
                }
            }
        });
    }

    public IReadOnlyCollection<ResultItem> Query()
    {
        lock (_lock)
        {
            return _history.ToArray();
        }
    }
}
