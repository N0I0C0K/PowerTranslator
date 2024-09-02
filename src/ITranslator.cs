namespace Translator.Protocol;


public abstract class ITranslateResult
{
    public abstract IEnumerable<ResultItem>? Transform();
}

public abstract class ITranslator
{
    public abstract ITranslateResult? Translate(string src, string toLan, string fromLan);
    public abstract void Reset();
}