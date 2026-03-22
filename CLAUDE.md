# cHelper 프로젝트 — Claude 가이드

## 프로젝트 개요
- **스택**: C# .NET 8, WinForms, Windows 시스템 트레이 앱
- **목적**: Claude Code 사용량 모니터링 (토큰 통계, 히트맵, 프로젝트 목록)
- **데이터 소스**: `~/.claude/projects/**/*.jsonl` (로컬 파일 파싱)
- **디자인**: Toss/Linear 스타일 다크 테마 (`TossTheme.cs`)

## 핵심 파일 구조
```
cHelper/
├── App/AppContext.cs          # 시스템 트레이, 타이머
├── Forms/MainForm.cs          # 메인 창 (커스텀 TabStrip + Panel 방식)
├── Controls/
│   ├── ClaudeCodeStatusControl.cs  # Usage 탭 (히트맵, 통계, 프로젝트 목록)
│   ├── ApiSettingsControl.cs       # Settings 탭
│   └── TossControls.cs            # 공통 커스텀 컨트롤
├── Services/ClaudeCodeService.cs   # 데이터 서비스 (2분 캐시)
├── Data/ClaudeCodeReader.cs        # JSONL 파싱
└── Utils/TossTheme.cs             # 색상/폰트 테마
```

## 반복 발생 문제 참조

깊은 렌더링 문제나 WinForms 관련 버그가 발생하면 반드시 아래 파일을 먼저 확인:

- **`Problem.md`** — 이전에 발생했던 주요 문제들과 원인 분석
- **`Solve.md`** — 각 문제의 해결책과 잘못된 접근 방법

### 자주 발생하는 문제 요약

| 문제 | 해결책 | 참조 |
|------|--------|------|
| 창 확대 시 렌더링 아티팩트 (모서리 깨짐) | `ResizeRedraw = true` | S1 |
| TabControl 흰색 테두리/인디케이터 제거 불가 | TabControl 포기 → Panel show/hide | S2 |
| ListView 헤더 우측 흰색 공간 | `FitLastColumn()` + `ResetClip()` | S3 |
| Label 흰색 배경 | 명시적 하드코딩 + `CellBorderStyle.None` | S4 |
| Windows 11 창 테두리 다크화 | `DwmSetWindowAttribute` attr=34 | S5 |

## 주요 설계 결정

### 탭 시스템
- WinForms `TabControl` **사용 금지** — 시스템 인디케이터 제거 불가
- 커스텀 `TabStrip` (Panel + GDI+) + `Panel` show/hide 방식 사용

### 커스텀 렌더링 패널
- `RoundedCard`, `RoundedSurface`, `HeatmapPanel` 모두 `ResizeRedraw = true` 필수
- `DoubleBuffered = true` 필수 (플리커 방지)

### 다크 테마
- `TossTheme.Background = #121214` (기준 배경)
- `TossTheme.Border = #2E2E35` (창 테두리 COLORREF 계산 주의)
- Label `BackColor`는 항상 `TossTheme.Background`로 명시적 하드코딩 (`Transparent` 금지)

### 데이터
- 토큰 데이터: `~/.claude/projects/**/{sessionId}.jsonl`
- Pro 플랜 = 토큰 기반, 비용 계산 무의미
- 히트맵: `MaxWeeks = 104`, `GetDailyTokens(104 * 7)`
- 서비스 캐시 TTL: 2분

## 빌드 & 실행
```
dotnet run --project cHelper/cHelper.csproj
```
- 빌드 전 반드시 트레이에서 cHelper 종료 (exe 잠금 방지)
