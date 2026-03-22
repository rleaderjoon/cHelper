# 문제별 해결책

## S1. 창 확대 시 렌더링 아티팩트 → ResizeRedraw = true

**해결책**: 커스텀 렌더링 패널의 생성자에 `ResizeRedraw = true` 설정

```csharp
public RoundedCard()
{
    DoubleBuffered = true;
    ResizeRedraw = true; // 핵심: 리사이즈 시 전체 재렌더링 강제
}
```

**작동 원리**
- `ResizeRedraw = true`는 컨트롤이 리사이즈될 때 전체 클라이언트 영역을 `Invalidate()` 처리
- 결과적으로 다음 WM_PAINT의 GDI 업데이트 리전 = 전체 클라이언트
- `OnPaintBackground`/`OnPaint`의 `Graphics.Clear()`가 전체 영역을 지움
- GDI+ 안티앨리어싱이 전체 경계에서 정상 동작

**적용 대상 컨트롤**
- `RoundedCard` (TossControls.cs)
- `RoundedSurface` (TossControls.cs)
- `HeatmapPanel` (ClaudeCodeStatusControl.cs)

**잘못된 접근 (사용 금지)**
```csharp
// X - GDI 업데이트 리전을 변경하지 못함
e.Graphics.SetClip(ClientRectangle);
```

---

## S2. TabControl 흰색 인디케이터 완전 제거 → TabControl 포기, 패널 show/hide 방식 채택

**해결책**: WinForms TabControl을 완전히 제거하고 커스텀 TabStrip + Panel show/hide로 대체

```csharp
// TabStrip: 커스텀 Panel (GDI+로 직접 그림)
private sealed class TabStrip : Panel
{
    protected override void OnPaint(PaintEventArgs e)
    {
        g.Clear(TossTheme.Background); // 시스템 렌더링 없음, 완전 커스텀
        // 선택된 탭만 HoverState 배경 + 파란 언더라인
    }
}

// 페이지 전환: Visible 토글
for (int i = 0; i < _pages.Length; i++)
    _pages[i].Visible = i == idx;
```

**핵심 교훈**
- WinForms TabControl은 OwnerDraw로도 시스템 인디케이터를 완전 제거할 수 없음
- 다크 테마가 필요한 경우 TabControl 자체를 포기하는 것이 가장 깨끗한 해결책
- Panel show/hide 방식은 TabControl 없이 동일한 UX 제공

---

## S3. ListView 헤더 우측 흰색 공간 → 두 가지 방법 병행

**방법 A**: 마지막 컬럼 너비를 동적으로 채우기 (권장)
```csharp
private bool _fittingColumn;
private void FitLastColumn()
{
    if (_fittingColumn || _projectList.Columns.Count == 0) return;
    _fittingColumn = true;
    int usedWidth = 0;
    for (int i = 0; i < _projectList.Columns.Count - 1; i++)
        usedWidth += _projectList.Columns[i].Width;
    int remaining = _projectList.ClientRectangle.Width - usedWidth;
    if (remaining > 40)
        _projectList.Columns[_projectList.Columns.Count - 1].Width = remaining;
    _fittingColumn = false;
}
// 연결: _projectList.Resize += (_, _) => FitLastColumn();
```

**방법 B**: DrawColumnHeader에서 마지막 컬럼 그릴 때 ResetClip 후 오른쪽 끝까지 채우기
```csharp
private void OnDrawHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
{
    bool isLast = e.ColumnIndex == Columns.Count - 1;
    var fillBounds = e.Bounds;
    if (isLast)
    {
        e.Graphics.ResetClip(); // 컬럼 클립 해제
        fillBounds = new Rectangle(e.Bounds.X, e.Bounds.Y,
            ClientRectangle.Width - e.Bounds.X, e.Bounds.Height);
    }
    using var bg = new SolidBrush(TossTheme.Surface);
    e.Graphics.FillRectangle(bg, fillBounds);
}
```

---

## S4. Label 흰색 배경 → 명시적 하드코딩

**해결책**: `Color.Transparent` 대신 정확한 색상값으로 하드코딩 + `CellBorderStyle = None`

```csharp
// TableLayoutPanel에 명시적 설정
var layout = new TableLayoutPanel
{
    CellBorderStyle = TableLayoutPanelCellBorderStyle.None, // 셀 경계선 제거
    BackColor = TossTheme.Background,
};

// Label BackColor 명시적 하드코딩
private static Label MakeSectionLabel(string text) => new()
{
    BackColor = TossTheme.Background, // Transparent 사용 금지
    // ...
};
```

---

## S5. WinForms 창 테두리 다크화 (Windows 11)

```csharp
protected override void OnHandleCreated(EventArgs e)
{
    base.OnHandleCreated(e);
    try
    {
        int dark = 1;
        DwmSetWindowAttribute(Handle, 20, ref dark, 4); // 다크 타이틀바

        // DWMWA_BORDER_COLOR: COLORREF = 0x00BBGGRR
        // #2E2E35 → R=0x2E, G=0x2E, B=0x35
        int borderColor = 0x2E | (0x2E << 8) | (0x35 << 16);
        DwmSetWindowAttribute(Handle, 34, ref borderColor, 4);
    }
    catch { } // Windows 10에서는 attr=34 미지원
}
```
