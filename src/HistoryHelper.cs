namespace Translater.History;

public class HistoryHelper
{
    private Queue<ResultItem> history;
    private const int MAX_HISTORY_NUM = 15;
    public HistoryHelper()
    {
        this.history = new Queue<ResultItem>();
    }

    public void Push(ResultItem item)
    {
        this.history.Enqueue(item);
        while (this.history.Count >= MAX_HISTORY_NUM)
            this.history.Dequeue();
    }

    public IEnumerable<ResultItem> query()
    {
        return this.history;
    }
}
