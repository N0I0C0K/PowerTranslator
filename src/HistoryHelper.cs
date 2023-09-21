namespace Translater.History;
using ManagedCommon;
public class HistoryHelper
{
    private Queue<ResultItem> history;
    private const int MAX_HISTORY_NUM = 15;
    private uint mark = 0;
    private string iconPath = "/Images/history.dark.png";
    public HistoryHelper()
    {
        this.history = new Queue<ResultItem>();
    }

    public void Push(ResultItem item)
    {
        if (item.Title.Length == 0 || item.SubTitle.Length == 0 || item.Title == null || item.SubTitle == null)
        {
            return;
        }
        uint _t = ++mark;
        Task.Delay(1000).ContinueWith((task) =>
        {
            if (_t == mark)
            {
                this.history.Enqueue(new ResultItem
                {
                    Title = item.Title,
                    SubTitle = $"{item.SubTitle} [{string.Format("{0:MM/dd HH:mm:ss}", DateTime.Now)}]",
                    iconPath = this.iconPath,
                    transType = "[History]"
                });
                while (this.history.Count >= MAX_HISTORY_NUM)
                    this.history.Dequeue();
            }
        });

    }
    public void UpdateIconPath(Theme now)
    {
        if (now == Theme.Light || now == Theme.HighContrastWhite)
        {
            iconPath = "Images/history.light.png";
        }
        else
        {
            iconPath = "Images/history.dark.png";
        }
    }
    public IEnumerable<ResultItem> query()
    {
        return this.history;
    }
}
