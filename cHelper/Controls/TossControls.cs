using System.Drawing.Drawing2D;
using cHelper.Utils;

namespace cHelper.Controls;

// ── Rounded card (elevation only, no border) ──────────────────────────────────
public class RoundedCard : Panel
{
    public int CornerRadius { get; set; } = 8;

    public RoundedCard()
    {
        DoubleBuffered = true;
        ResizeRedraw = true; // 리사이즈 시 전체 재렌더링 강제
        BackColor = TossTheme.CardBackground;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(TossTheme.Background);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        TossTheme.DrawCard(e.Graphics, new Rectangle(0, 0, Width, Height), CornerRadius);
    }
}

// ── Rounded surface panel (rounded container, Surface color) ──────────────────
public class RoundedSurface : Panel
{
    public int CornerRadius { get; set; } = 8;

    public RoundedSurface()
    {
        DoubleBuffered = true;
        ResizeRedraw = true; // 리사이즈 시 전체 재렌더링 강제
        BackColor = TossTheme.Surface;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(TossTheme.Background);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = TossTheme.RoundedRect(new Rectangle(0, 0, Width, Height), CornerRadius);
        using var brush = new SolidBrush(TossTheme.Surface);
        e.Graphics.FillPath(brush, path);
    }
}

// ── ListView with dark owner-drawn header ─────────────────────────────────────
public class DarkListView : ListView
{
    public DarkListView()
    {
        OwnerDraw = true;
        DoubleBuffered = true;
        DrawColumnHeader += OnDrawHeader;
        DrawItem        += OnDrawItem;
        DrawSubItem     += OnDrawSubItem;
    }

    // 마지막 컬럼은 ListView 오른쪽 끝까지 배경을 채워 흰 사각형을 제거
    private void OnDrawHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        bool isLast = e.ColumnIndex == Columns.Count - 1;

        // 마지막 컬럼: 클립 해제 후 오른쪽 끝까지 채움
        var fillBounds = e.Bounds;
        if (isLast)
        {
            e.Graphics.ResetClip();
            fillBounds = new Rectangle(e.Bounds.X, e.Bounds.Y,
                ClientRectangle.Width - e.Bounds.X, e.Bounds.Height);
        }

        using var bg = new SolidBrush(TossTheme.Surface);
        e.Graphics.FillRectangle(bg, fillBounds);

        // 하단 구분선만 (컬럼 구분선 없음)
        using var sep = new Pen(TossTheme.Border);
        e.Graphics.DrawLine(sep,
            fillBounds.Left, e.Bounds.Bottom - 1,
            fillBounds.Right, e.Bounds.Bottom - 1);

        // 헤더 텍스트: UPPERCASE, 흐리게
        var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y,
                                     e.Bounds.Width - 10, e.Bounds.Height);
        using var font = TossTheme.StatLabel();
        TextRenderer.DrawText(e.Graphics, e.Header?.Text?.ToUpper(),
            font, textRect, TossTheme.TextTertiary,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }

    // 빈 영역(아이템 없는 하단) 배경 처리 — 세로 구분선 제거
    protected override void WndProc(ref Message m)
    {
        const int WM_ERASEBKGND = 0x0014;
        if (m.Msg == WM_ERASEBKGND)
        {
            using var g = Graphics.FromHdc(m.WParam);
            g.Clear(TossTheme.Background);
            m.Result = (IntPtr)1;
            return;
        }
        base.WndProc(ref m);
    }

    private static void OnDrawItem(object? sender, DrawListViewItemEventArgs e)
    {
        // 행 배경만 그림 (선택 상태 포함) — 텍스트는 DrawSubItem에서
        var bg = (e.State & ListViewItemStates.Selected) != 0
            ? TossTheme.HoverState
            : TossTheme.Background;
        using var brush = new SolidBrush(bg);
        e.Graphics.FillRectangle(brush, e.Bounds);
    }

    private static void OnDrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.Item == null || e.SubItem == null) return;

        // 컬럼 0: 밝게(Primary) / 1(Sessions): Label / 2,3(날짜·경로): Label
        var fg = e.ColumnIndex == 0 ? TossTheme.TextPrimary : TossTheme.TextLabel;
        var textRect = new Rectangle(
            e.Bounds.X + 8, e.Bounds.Y,
            e.Bounds.Width - 10, e.Bounds.Height);

        using var font = TossTheme.Body(8.5f);
        TextRenderer.DrawText(e.Graphics, e.SubItem.Text, font, textRect, fg,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left |
            TextFormatFlags.EndEllipsis);
    }
}

// ── Button ────────────────────────────────────────────────────────────────────
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
            FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#3A6FEE");
            FlatAppearance.MouseDownBackColor = ColorTranslator.FromHtml("#2A5FDD");
        }
        else
        {
            BackColor = TossTheme.Surface;
            ForeColor = TossTheme.TextSecondary;
            FlatAppearance.BorderSize = 1;
            FlatAppearance.BorderColor = TossTheme.Border;
            FlatAppearance.MouseOverBackColor = TossTheme.HoverState;
            FlatAppearance.MouseDownBackColor = TossTheme.Border;
        }
    }
}
