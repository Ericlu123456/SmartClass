# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build (debug)
dotnet build SmartClass.csproj

# Build (release)
dotnet build SmartClass.csproj -c Release

# Run
dotnet run --project SmartClass.csproj

# Publish (single-file trimmed for win-x64)
dotnet publish SmartClass.csproj -c Release -r win-x64 --self-contained false
```

No test project exists — there is no test runner to invoke.

## Architecture

This is a **.NET 8 WPF + WinForms top-bar application** for classroom management (下课提醒, 值日表, 值日提醒). `MainWindow` is a frameless, topmost, full-width top bar (70px) positioned at the top of the screen — three-column layout: date/countdown/buttons (left), clock (center), today's courses/duty (right). A tray icon provides balloon notifications and context menu.

### Startup flow

1. `App.OnStartup` → `SingleInstanceManager` (named `Global\SmartClass_SingleInstance_{MachineName}` mutex) → creates `MainWindow` → calls `InitializeTopBar()` → `Show()`.
2. `MainWindow.InitializeTopBar()`: loads `AppState`, positions window at screen top, sets up tray icon, starts business timer (60s) + UI refresh timer (1s), checks yesterday's duties, opens `ScheduleWindow`.

### Core loop

- **Business timer (60s)**: For each `Course` matching today's weekday: fire `AskBoardCleaned` on start time (modal `AutoCloseDialog` — 10min timeout, social credits +1/-1), `NotifyAfterClass` on end time (balloon tip + speech). At 17:30: `NotifyDutyAtEndOfDay`.
- **UI refresh timer (1s)**: Updates date label, clock (`HH:mm:ss`), semester countdown, next course labels, today's duty group names.
- On startup: `CheckYesterdayDuties` (MessageBox Yes/No → +5/-5 social credits).

- For each `Course` matching today's weekday: fire `AskBoardCleaned` on start time (modal `AutoCloseDialog` — 10min timeout, social credits +1/-1), and `NotifyAfterClass` on end time (balloon tip).
- At 17:30: `NotifyDutyAtEndOfDay` (balloon tip with duty group members).
- On startup: `CheckYesterdayDuties` (MessageBox Yes/No → +5/-5 social credits).

### Data layer

- **`StorageService`** (static): Thread-safe JSON persistence to `appstate.json`. Atomic writes (write `.tmp` → rename), automatic `.bak` backup on every save, JSON round-trip validation before write (refuses to write corrupt data), retry logic (5 attempts, 100ms apart). Load falls back to `.bak` if main file is corrupt; creates fresh default state if both fail. Never throws — always returns a valid `AppState`.
- **`LogService`** (static): Thread-safe append log to `error.log` with 1 MB auto-rotation to `error.old.log`. Used pervasively — all exception handlers log here. The app never crashes silently; all failures are captured.

### Crash-prevention patterns (do not regress)

1. **WPF `DispatcherTimer.Tick` must never throw.** An unhandled exception in the tick handler silently kills the timer forever with no error. `MainWindow.Timer_Tick` has a per-course try-catch + a top-level catch-all.
2. **All event handlers are wrapped in try-catch** — every button click, selection change, and focus handler logs failures via `LogService.Log(ex, context)`.
3. **`App.xaml.cs` registers global handlers**: `DispatcherUnhandledException` (keeps app alive), `AppDomain.CurrentDomain.UnhandledException` (logs before crash), `TaskScheduler.UnobservedTaskException`.
4. **`dynamic` usage** in `SettingsWindow` (for anonymous types from `ItemsSource`) is guarded with null checks and try-catch — `RuntimeBinderException` would otherwise crash the app.
5. **`ScheduleWindow.PositionWindowBottom`** guards against NaN `ActualHeight`/`Height` with fallback to 200.
6. **`AutoCloseDialog`** stops its timer on close to prevent post-close `DialogResult` assignment crashes.

### Data model (`AppState`)

- `List<Student>` — has `Id` (Guid string), `Name`, `SocialCredits` (default 50)
- `List<Course>` — has `Subject`, `DayOfWeek` (Chinese: 周一–周日), `StartTime`/`EndTime` (`HH:mm`)
- `List<DutyGroup>` — has `Name`, `List<DutyMember>` (each links to a `StudentId` + `Role`)
- `List<DailyDuty>` — maps `Date` → `DutyGroupId`
- `FontSize` (double, default 14), `EnableAutoShutdown` (bool), `AutoShutdownTime` (string, default "23:00")

### Windows

- **`MainWindow`**: Hidden coordinator; tray icon with context menu (设置, 显示/隐藏课程表, 导出报表, 重启, 退出). Has a visible status bar and Settings/Exit buttons if shown.
- **`SettingsWindow`**: Tabbed settings with 5 navigation pages — General, Students, Courses, DutyGroups, DailyDuties.
- **`ScheduleWindow`**: Borderless, transparent, topmost floating overlay at bottom of screen. Shows today's courses (left) and duty group members (right). Adapts to Windows light/dark theme.
- **`AutoCloseDialog`**: Modal dialog with a countdown timer that auto-closes after a configurable `TimeSpan`.

### UI framework

Uses `iNKORE.UI.WPF.Modern` (0.10.2.1) for WinUI-style theming — Mica backdrop, modern window chrome, theme-aware controls. `System.Drawing.Common` is used only for tray icon extraction.

### Namespace

The C# namespace is `smartClass` (lowercase). Models live in `smartClass.Models`, services in `smartClass.Services`, windows in `smartClass.Windows`.
