using System.Drawing.Drawing2D;

namespace cHelper.Utils;

public static class TossTheme
{
    // ── Layered dark grays (Linear-style elevation) ────────────────────────────
    // 각 레이어가 미세하게 밝아져 깊이감(elevation)을 형성합니다
    public static readonly Color Background    = ColorTranslator.FromHtml("#121214"); // Layer 0 — 순수 블랙 아님
    public static readonly Color Surface       = ColorTranslator.FromHtml("#19191D"); // Layer 1 — 배너, 입력창
    public static readonly Color CardBackground = ColorTranslator.FromHtml("#202026"); // Layer 2 — 카드, 상승된 요소
    public static readonly Color HoverState    = ColorTranslator.FromHtml("#28282F"); // Layer 3 — 호버

    // 선(Border)은 최소한으로만 사용합니다
    public static readonly Color Border        = ColorTranslator.FromHtml("#2E2E35");

    // ── 타이포그래피 위계 — 크기·굵기·색상으로 중요도를 표현 ────────────────
    public static readonly Color TextPrimary   = ColorTranslator.FromHtml("#EFEFF2"); // 핵심 데이터 (토큰 수치)
    public static readonly Color TextSecondary = ColorTranslator.FromHtml("#8A8A98"); // 부제, 보조 정보
    public static readonly Color TextLabel     = ColorTranslator.FromHtml("#B2B2C4"); // 카드 타이틀, 리스트 서브컬럼 — Secondary보다 밝게
    public static readonly Color TextTertiary  = ColorTranslator.FromHtml("#606072"); // 섹션 헤더 — 이전보다 약간 밝게

    // ── 액센트 ────────────────────────────────────────────────────────────────
    public static readonly Color PrimaryBlue   = ColorTranslator.FromHtml("#4D82FF"); // 부드러운 블루
    public static readonly Color Success       = ColorTranslator.FromHtml("#22C55E");
    public static readonly Color Error         = ColorTranslator.FromHtml("#EF4444");
    public static readonly Color Warning       = ColorTranslator.FromHtml("#F59E0B");

    // ── 폰트 — 크기와 굵기로 위계 표현 ──────────────────────────────────────
    public static Font Body(float size = 9f, FontStyle style = FontStyle.Regular)
        => TryFont(["Segoe UI Variable", "Segoe UI", "Malgun Gothic"], size, style);

    // 핵심 수치: 크고 굵게
    public static Font StatValue() => TryFont(["Segoe UI Variable", "Segoe UI"], 17f, FontStyle.Bold);

    // 보조 레이블: 작고 흐리게 (크기 차이가 위계를 만듦)
    public static Font StatLabel()     => TryFont(["Segoe UI Variable", "Segoe UI"], 7.5f, FontStyle.Regular);
    public static Font SectionHeader() => TryFont(["Segoe UI Variable", "Segoe UI"], 8f, FontStyle.Bold);
    public static Font Monospace(float size = 9f) => new Font("Consolas", size);

    private static Font TryFont(string[] families, float size, FontStyle style)
    {
        foreach (var name in families)
            try { return new Font(name, size, style); } catch { }
        return SystemFonts.DefaultFont;
    }

    // ── DrawCard: 테두리 없이 배경색의 미세한 차이로 깊이감 표현 ────────────
    public static void DrawCard(Graphics g, Rectangle bounds, int radius = 12)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedRect(bounds, radius);
        using var fill = new SolidBrush(CardBackground);
        g.FillPath(fill, path);
        // 테두리 없음 — 여백과 배경색 차이만으로 구분
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
