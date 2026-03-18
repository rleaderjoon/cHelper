using System.Drawing.Drawing2D;

namespace cHelper.Utils;

public static class TossTheme
{
    // ── Color Palette ──────────────────────────────────────────────
    public static readonly Color Background    = Color.White;                          // 전체 배경: 순백
    public static readonly Color CardBackground = ColorTranslator.FromHtml("#F2F4F6"); // 카드(통계 박스) 배경: 연회색
    public static readonly Color Surface       = Color.White;
    public static readonly Color PrimaryBlue   = ColorTranslator.FromHtml("#3182F6");
    public static readonly Color TextPrimary   = ColorTranslator.FromHtml("#191F28");
    public static readonly Color TextSecondary = ColorTranslator.FromHtml("#4E5968");
    public static readonly Color TextTertiary  = ColorTranslator.FromHtml("#8B95A1");
    public static readonly Color Border        = ColorTranslator.FromHtml("#E5E8EB");
    public static readonly Color Separator     = ColorTranslator.FromHtml("#F2F4F6"); // 구분선
    public static readonly Color Success       = ColorTranslator.FromHtml("#00C08B");
    public static readonly Color Error         = ColorTranslator.FromHtml("#F04452");
    public static readonly Color Warning       = ColorTranslator.FromHtml("#FF8B00");

    // ── Typography ─────────────────────────────────────────────────
    // Malgun Gothic for Korean, falls back to Segoe UI
    public static Font Body(float size = 9f, FontStyle style = FontStyle.Regular)
        => TryFont(["Malgun Gothic", "Segoe UI"], size, style);

    public static Font StatValue()     => TryFont(["Malgun Gothic", "Segoe UI"], 15f, FontStyle.Bold);
    public static Font StatLabel()     => TryFont(["Malgun Gothic", "Segoe UI"], 8f,  FontStyle.Regular);
    public static Font SectionHeader() => TryFont(["Malgun Gothic", "Segoe UI"], 9f,  FontStyle.Bold);
    public static Font Monospace(float size = 9f) => new Font("Consolas", size);

    private static Font TryFont(string[] families, float size, FontStyle style)
    {
        foreach (var name in families)
            try { return new Font(name, size, style); } catch { }
        return SystemFonts.DefaultFont;
    }

    // ── Drawing Helpers ─────────────────────────────────────────────
    // 테두리 없는 카드: 연회색 배경만, 경계선 없음 (Toss 스타일)
    public static void DrawCard(Graphics g, Rectangle bounds, int radius = 10)
    {
        using var path = RoundedRect(bounds, radius);
        using var fill = new SolidBrush(CardBackground);
        g.FillPath(fill, path);
    }

    public static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X,         r.Y,          d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
        path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
        path.CloseFigure();
        return path;
    }
}
