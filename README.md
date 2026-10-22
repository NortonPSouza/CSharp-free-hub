<div align="center">

# ⚡ FreeHub — Desktop Telemetry & Gamified Focus HUD

**Zero-manual-input Windows desktop telemetry utility ("Spotify Wrapped" for your PC + Passive RPG Progression)**

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.6-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/C%23-7.3-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x86-0078D6?style=for-the-badge&logo=windows&logoColor=white)](https://www.microsoft.com/windows)
[![Database](https://img.shields.io/badge/SQLite-1.0.119-003B57?style=for-the-badge&logo=sqlite&logoColor=white)](https://www.sqlite.org/)
[![Cloud Sync](https://img.shields.io/badge/Sync-Google%20Drive%20API%20v3%20(OAuth2)-4285F4?style=for-the-badge&logo=googledrive&logoColor=white)](https://developers.google.com/drive)
[![UI](https://img.shields.io/badge/UI-WinForms%20%7C%20DevExpress%2018.1-FF7200?style=for-the-badge)](https://www.devexpress.com/)

</div>

---

<details>
<summary>🇧🇷 <b>Resumo em Português (Clique para expandir)</b></summary>

<br/>

O **FreeHub** é um utilitário desktop para Windows focado em produtividade passiva e gamificação (ideal para TDAH, com recompensa visual imediata e **zero digitação manual de tarefas**). Ele roda silenciosamente na bandeja do sistema (*System Tray*) com um HUD estilo Gamer, monitorando automaticamente a janela ativa do sistema operacional via **Win32 API (`user32.dll`)**, classificando a atividade em tempo real (`Coding`, `Database`, `Gaming`, `Browsing`, `Idle`) e concedendo **XP passivo**, *combos de foco* e *level-ups*.

Possui uma arquitetura **Local-First + Sincronização Multi-Máquina Livre de Conflitos**:
1. **Persistência Local Ultrarrápida:** Cada computador grava apenas no seu próprio arquivo `freehub_<MACHINE>.db` (em `%LocalAppData%\FreeHub_Data` ou pasta compartilhada) com `Journal Mode=TRUNCATE` e `Pooling=False`.
2. **Sincronização em Nuvem via Google Drive API (HTTP / OAuth 2.0):** Permite autenticar diretamente na conta Google via OAuth 2.0 para fazer upload do `.db` da máquina atual e download dos `.db` das outras máquinas via HTTP — sem exigir a instalação do aplicativo Google Drive Desktop no PC do trabalho!

</details>

---

## 🎯 Overview & Core Concept

Traditional time trackers fail because they require manual timers and constant task typing. **FreeHub** takes a completely passive, ADHD-friendly approach inspired by **RPG HUDs** and **Spotify Wrapped**:

1. **Zero Manual Input:** Runs quietly in the Windows System Tray and tracks foreground windows automatically via native Win32 P/Invoke (`User32.dll`).
2. **Real-Time Process Classification & Gamification:** Identifies what you are doing (`devenv` = **Coding**, `ssms`/`sqldeveloper` = **Database**, games = **Gaming**, browsers = **Browsing**) and rewards passive XP per minute, maintaining focus streaks/combos and triggering visual level-ups.
3. **Conflict-Free Multi-Machine Sync (Local SQLite + Google Drive HTTP API):**
   * Designed to run seamlessly across multiple PCs (**Work PC**, **Home PC**, **Laptop**).
   * **Write Isolation:** Each machine writes *exclusively* to its own local SQLite database named after `Environment.MachineName` (e.g., `freehub_WORK-PC.db`, `freehub_HOME-PC.db`) configured with `Journal Mode=TRUNCATE;` and `Pooling=False;` to release file handles immediately.
   * **Cloud Sync via HTTP (OAuth 2.0) or Shared Drive:** Supports both automatic local folder discovery and direct **Google Drive REST API v3** synchronization (authenticating via OAuth 2.0 Bearer Token to upload the local machine's `.db` and download other machines' `freehub_*.db` files without needing the Google Drive Desktop client installed).
   * **Unified Read Aggregation:** When rendering the HUD, Total XP, or analytics grids/charts, `SqliteTelemetryRepository` dynamically discovers all `freehub_*.db` files in `FreeHub_Data` and merges the telemetry into unified views (`DataTable` & domain lists).

---

## 🏗️ Architecture & Design Principles

The project strictly follows **SOLID**, **Clean Code**, and **Layered Architecture**, keeping business rules and SQLite persistence 100% decoupled from the Windows Forms UI code-behind.

```mermaid
flowchart LR
    subgraph OS["Windows OS"]
        W32["User32.dll\n(GetForegroundWindow)"]
    end

    subgraph Infra["Infrastructure Layer"]
        WT["Win32ActiveWindowTracker"]
        PATH["StoragePathResolver"]
        REPO["SqliteTelemetryRepository"]
        SYNC["GoogleDriveSyncService\n(OAuth 2.0 / HTTP API)"]
        DB1[("freehub_WORK-PC.db\n(Local Write)")]
        DB2[("freehub_HOME-PC.db\n(Synced Read-Only)")]
    end

    subgraph Cloud["Google Cloud"]
        GDRIVE[("Google Drive API v3\n(FreeHub_Data Folder)")]
    end

    subgraph Core["Core Layer (Contracts & Domain)"]
        IWT["IActiveWindowTracker"]
        IPC["IProcessClassifier"]
        IREPO["ITelemetryRepository"]
    end

    subgraph Services["Services Layer"]
        PC["ProcessClassifier\n(Strategy / Rules)"]
        ORCH["TelemetryOrchestrator\n(XP, Streaks & Leveling)"]
    end

    subgraph UI["UI Layer (WinForms / DevExpress)"]
        HUD["MainHudForm & TrayIcon"]
    end

    W32 --> WT
    WT -.->|Implements| IWT
    REPO --> PATH
    REPO -.->|Implements| IREPO
    REPO -->|Isolated Write| DB1
    REPO -->|Unified Read| DB1
    REPO -->|Unified Read| DB2
    DB1 <-->|Upload / Download HTTP| SYNC
    DB2 <-->|Download HTTP| SYNC
    SYNC <-->|OAuth2 Bearer Token| GDRIVE
    PC -.->|Implements| IPC
    ORCH --> IWT
    ORCH --> IPC
    ORCH --> IREPO
    HUD --> ORCH
```

### Key Patterns Applied
* **Repository Pattern (`ITelemetryRepository`):** Abstracts single-node SQLite writes and multi-node database discovery/aggregation.
* **Single Responsibility Path Resolution (`StoragePathResolver`):** Centralizes database directory discovery and filename sanitization per machine.
* **Strategy / Rule Pattern (`IProcessClassifier`):** Extensible classification rules mapping process names and window titles to `ActivityCategory` and XP multipliers.
* **Dependency Injection via Constructor:** Explicit dependency graph wired at the Composition Root (`Program.cs`).
* **Strict Separation of Concerns:** Zero SQL queries or process inspection logic inside `Form` event handlers.

---

## 📂 Project Structure

```text
FreeHub/
 ├── Core/            # Domain Entities, Enums, and Contracts (ITelemetryRepository, IActiveWindowTracker, IProcessClassifier)
 ├── Infrastructure/  # SQLite multi-machine persistence, path resolver, Google Drive HTTP sync, Win32 API P/Invoke (User32.dll)
 ├── Services/        # Process classification rules, XP/Streak/Level calculator, background Telemetry Orchestrator
 └── UI/              # Windows Forms HUD, System Tray (NotifyIcon) manager, and DevExpress 18.1 views
```

---

## 🛠️ Technical Stack & Constraints

| Component | Technology / Specification |
| :--- | :--- |
| **Target Framework** | `.NET Framework 4.6` |
| **Language Version** | Strictly **C# 7.3** (Visual Studio 2019 / MSBuild 15.0) |
| **Platform Target** | Strictly **x86 (32-bit)** (native Win32 & unmanaged SQLite interop compatibility) |
| **Database** | `System.Data.SQLite.Core` (`v1.0.119.0`) with `Journal Mode=TRUNCATE;` & `Pooling=False;` |
| **Cloud Sync** | Local folder discovery + **Google Drive REST API v3** (`OAuth 2.0` HTTP upload/download) |
| **UI Framework (Phase 1)** | Pure Windows Forms (`Form`, `DataGridView`, `NotifyIcon`, `Timer`, `ProgressBar`) |
| **UI Framework (Phase 2)** | **DevExpress 18.1.5.0** (`XtraForm`, `GridControl`, `GridView`, `XtraCharts`, `AlertControl`, `ProgressBarControl`, `The Bezier` Dark Skin) |

---

## 🗺️ Development Roadmap

- [x] **Step 1 — Core Foundation & Project Setup**
  - [x] Configure strict `.NET Framework 4.6`, `C# 7.3`, and `x86` target build profile.
  - [x] Define domain models (`ActivityCategory`, `WindowActivityLog`, `ActiveWindowInfo`) and core interfaces (`ITelemetryRepository`, `IActiveWindowTracker`, `IProcessClassifier`).
- [x] **Step 2 — Infrastructure Layer (Win32 + Multi-Machine SQLite)**
  - [x] Implement `Win32ActiveWindowTracker` via `user32.dll` (`GetForegroundWindow`, `GetWindowThreadProcessId`, `GetWindowText`).
  - [x] Implement `StoragePathResolver` and `SqliteTelemetryRepository` with isolated per-machine writes and unified multi-DB read aggregation.
- [ ] **Step 3 — Services & Gamification Engine**
  - [ ] Implement `ProcessClassifier` rules (`Coding`, `Database`, `Gaming`, `Browsing`, `Idle`).
  - [ ] Implement XP calculation, focus streak multipliers, and Level-Up events in `TelemetryService`.
- [ ] **Step 4 — Phase 1 UI (Pure Windows Forms) & Cloud Sync**
  - [ ] Build `MainHudForm` with real-time activity display, XP `ProgressBar`, `DataGridView` summary, and System Tray (`NotifyIcon`) integration.
  - [ ] Add Google Drive OAuth 2.0 HTTP Sync integration (`ICloudSyncService`) for syncing `freehub_*.db` files across machines without requiring Google Drive Desktop.
- [ ] **Step 5 — Phase 2 UI Migration (DevExpress 18.1.5)**
  - [ ] Migrate to `XtraForm` with dark theme skinning (`The Bezier`), `GridControl`/`GridView`, `XtraCharts` telemetry breakdown, and `AlertControl` level-up toasts.

---

## 🚀 Getting Started

### Prerequisites
* **Windows OS** (Windows 10 / 11)
* **Visual Studio 2019** (or newer) with **.NET Desktop Development** workload
* **.NET Framework 4.6 Targeting Pack**

### Build & Run
1. Clone the repository:
   ```powershell
   git clone https://github.com/NortonPSouza/C--free-pulse.git
   cd C--free-pulse
   ```
2. Open `FreeHub.sln` in **Visual Studio 2019**.
3. Restore NuGet packages:
   * Right-click the solution in **Solution Explorer** $\rightarrow$ **Restore NuGet Packages** (or run `msbuild /t:Restore`).
4. Ensure the active solution platform is set to **`Debug | x86`** and press **`F5`** to build and launch.

---

## 📏 Commit Convention

This project follows the [Conventional Commits](https://www.conventionalcommits.org/) specification:

* `feat:` — New feature or capability (e.g., `feat(infra): implement Win32 foreground window tracker`)
* `fix:` — Bug fix (e.g., `fix(sqlite): handle locked database file during cloud sync`)
* `refactor:` — Code restructuring without changing external behavior
* `docs:` — Documentation updates (e.g., `docs: add architecture diagram and project roadmap to README`)
* `chore:` — Build scripts, `.csproj`, `.gitignore`, or dependency maintenance
