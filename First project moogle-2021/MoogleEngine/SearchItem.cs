using System.IO;
namespace MoogleEngine;

public class SearchItem
{
    public string Title { get; private set; }
    public string Snippet { get; private set; }
    public float Score { get; private set; }

    public SearchItem(string title, string snippet, float score)
    {

        this.Title = title;
        this.Snippet = snippet;
        this.Score = score;
    }
    


}
