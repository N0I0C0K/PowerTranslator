namespace Translater.History;

public class HistoryHelper
{
    private Queue<ResultItem> history;
    private const int MAX_HISTORY_NUM = 15;
    private uint mark = 0;
    public HistoryHelper()
    {
        this.history = new Queue<ResultItem>();
    }

    public void Push(ResultItem item)
    {
        uint _t = ++mark;
        Task.Delay(1000).ContinueWith((task) =>
        {
            if (_t == mark)
            {
                this.history.Enqueue(item);
                while (this.history.Count >= MAX_HISTORY_NUM)
                    this.history.Dequeue();
            }
        });

    }

    public IEnumerable<ResultItem> query()
    {
        return this.history;
    }
}
