using System;

// Character counts come from TMP, not string length, so surrogate pairs are not split.
public class TextReveal
{
    public int Visible { get; private set; }
    public int Total { get; private set; }
    public bool Complete => Visible >= Total;
    private double accumulated;

    public void Begin(int characterCount)
    {
        Total = Math.Max(0, characterCount);
        Visible = 0;
        accumulated = 0;
    }

    public void Tick(float seconds, float charactersPerSecond)
    {
        if (Complete || seconds <= 0) return;
        if (charactersPerSecond <= 0) { Finish(); return; }
        accumulated = Math.Min(Total, accumulated + seconds * charactersPerSecond);
        Visible = (int)Math.Min(Total, Math.Floor(accumulated + 0.000001));
    }

    public void Finish()
    {
        Visible = Total;
        accumulated = Total;
    }
}
