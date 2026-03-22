# 반복적으로 발생한 주요 문제들

## P1. WinForms 창 확대 시 렌더링 아티팩트 (모서리 깨짐, 잔디 셀 불일치)

**증상**
- 창 크기를 늘렸을 때 RoundedCard, RoundedSurface의 오른쪽 모서리가 울퉁불퉁하게(aliased) 렌더링됨
- HeatmapPanel(잔디)의 왼쪽(기존 영역)과 오른쪽(새로 드러난 영역) 셀이 불일치하거나 아티팩트 발생
- 배너(RoundedSurface) 오른쪽 경계선이 깨짐

**원인**
- WinForms는 창 확대 시 새로 드러난 영역만 GDI 업데이트 리전에 추가함
- `Graphics.Clear(BackColor)`는 GDI+ 클립(SetClip)만 지울 뿐, GDI 업데이트 리전(실제 픽셀을 그릴 수 있는 영역)을 변경하지 못함
- 이전에 이미 그려진 왼쪽 영역은 "유효(valid)"로 간주되어 재렌더링되지 않음
- 결과: 반쪽만 새로 그려진 RoundedRect의 안티앨리어싱 엣지가 잘려 보임

**시도했으나 효과 없었던 접근**
- `e.Graphics.SetClip(ClientRectangle)` — GDI 업데이트 리전은 GDI+ SetClip으로 변경 불가
- WndProc에서 WM_PAINT 후 경계선 덮기 — 부분 적용만 됨

---

## P2. WinForms TabControl 시스템 테두리/인디케이터 제거 불가

**증상**
- 커스텀 다크 테마 적용 시 TabControl 탭 버튼에 흰색 테두리 잔존
- 선택/비선택 탭 모두에 흰색 표시줄(인디케이터)이 표시됨
- 탭 스트립 오른쪽 여백이 흰색으로 남음
- 컨텐츠 영역(TabPage) 주변에 흰색 테두리 잔존

**원인**
- WinForms TabControl은 OwnerDraw 모드에서도 비클라이언트 영역 테두리를 시스템이 그림
- `WS_EX_CLIENTEDGE`, `WS_BORDER` 제거와 `WM_NCPAINT` 차단을 해도 시스템이 선택/비선택 탭에 인디케이터를 독립적으로 그림
- `SetWindowTheme("", "")`, `Appearance = FlatButtons` 조합으로도 완전 제거 불가

**시도했으나 효과 없었던 접근**
- `DrawMode = OwnerDrawFixed` — 탭 버튼은 커스텀 가능하나 인디케이터는 별도
- `SetWindowTheme("", "")` — 기본 스타일 제거했으나 인디케이터 잔존
- `Appearance = TabAppearance.FlatButtons` + `ItemSize = (0, 1)` — 1px 스트립 잔존
- `CreateParams`에서 `WS_EX_CLIENTEDGE` + `WS_BORDER` 제거
- `WM_NCPAINT` 차단, `WM_ERASEBKGND` 오버라이드

---

## P3. ListView 헤더 우측 흰색 빈 공간

**증상**
- DarkListView에서 마지막 컬럼(PATH) 오른쪽에 흰색 빈 사각형 잔존

**원인**
- `DrawColumnHeader` 이벤트는 컬럼별로 발생하며, 마지막 컬럼 이후 빈 공간은 이벤트가 발생하지 않음
- GDI는 `DrawColumnHeader`의 e.Graphics 클립을 컬럼 bounds로 제한하므로 bounds 밖으로 그리기 불가

---

## P4. WinForms Label BackColor가 흰색으로 보임

**증상**
- TableLayoutPanel 안 Label에 `BackColor = TossTheme.Background` 설정했으나 흰색/밝은 색으로 렌더링됨

**원인**
- Windows visual styles 활성화 시 일부 Label 컨트롤의 BackColor가 시스템 색으로 override됨
- `Color.Transparent`는 부모 배경을 상속하는데, TableLayoutPanel 내부에서 부분적으로 동작하지 않는 경우 존재
