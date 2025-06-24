using System;
using System.Collections.Generic;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace PowerTranslatorExtension.Protocol;


public class ResultItem
{
    public string Title { get; set; } = default!;
    public string SubTitle { get; set; } = default!;
    public Func<bool>? Action { get; set; }
    public string? CopyTgt { get; set; }
    public IconInfo? icon { get; set; }
    public string? transType { get; set; }
    public string? fromApiName { get; set; }
    public string? Description { get; set; }
}

public interface ITranslateResult
{
    public IEnumerable<ResultItem>? Transform();
}

public interface ITranslator
{
    public ITranslateResult? Translate(string src, string toLan, string fromLan);
    public void Init();
    public void Reset();
    public bool Inited { get; }
    public string Name { get; }
}