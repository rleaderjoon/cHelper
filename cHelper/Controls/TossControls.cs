using System.Drawing.Drawing2D;
using cHelper.Utils;

namespace cHelper.Controls;

// ── Rounded card panel ────────────────────────────────────────────────────────
public class RoundedCard : Panel
{
    public int CornerRadius { get; set; } = 10;

    public RoundedCard()
    {
        DoubleBuffered = true;
        BackColor = TossTheme.CardBackground;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Fill corners with parent background first, then draw the rounded card
        e.Graphics.Clear(TossTheme.Background);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(1, 1, Width - 2, Height - 2);
        TossTheme.DrawCard(e.Graphics, bounds, CornerRadius);
    }
}

// ── Toss-style button ─────────────────────────────────────────────────────────
public enum TossButtonStyle { Primary, Ghost }

public class TossButton : Button
{
    private TossButtonStyle _style = TossButtonStyle.Primary;

    public TossButtonStyle Style
    {
        get => _style;
        set { _style = value; ApplyColors(); }
    }

    public TossButton()
    {
        FlatStyle = FlatStyle.Flat;
        Cursor = Cursors.Hand;
        Height = 28;
        Font = TossTheme.Body(9f);
        ApplyColors();
    }

    private void ApplyColors()
    {
        if (_style == TossButtonStyle.Primary)
        {
            BackColor = TossTheme.PrimaryBlue;
            ForeColor = Color.White;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#1D6AE5");
            FlatAppearance.MouseDownBackColor = ColorTranslator.FromHtml("#1259CC");
        }
        else
        {
            BackColor = TossTheme.Surface;
            ForeColor = TossTheme.TextSecondary;
            FlatAppearance.BorderSize = 1;
            FlatAppearance.BorderColor = TossTheme.Border;
            FlatAppearance.MouseOverBackColor = TossTheme.Background;
            FlatAppearance.MouseDownBackColor = TossTheme.Border;
        }
    }
}
