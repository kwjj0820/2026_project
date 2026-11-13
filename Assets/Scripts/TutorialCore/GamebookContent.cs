using System;

// Presentation-only split. The original transcript remains untouched.
public sealed class GamebookContent
{
    public string Introduction { get; private set; }
    public string Body { get; private set; }

    public static GamebookContent Parse(string text)
    {
        text = text ?? "";
        int quote = text.IndexOf('"');
        if (quote < 0) quote = text.IndexOf('“');
        string introduction = quote > 0 ? text.Substring(0, quote).Trim() : "";
        // Long introductions stay in the paginated reader so nothing is clipped.
        if (introduction.Length > 95 || introduction.Split('\n').Length > 2)
            introduction = "";
        return new GamebookContent
        {
            Introduction = introduction,
            Body = introduction.Length > 0 ? text.Substring(quote).Trim() : text
        };
    }
}
