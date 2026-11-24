# Julscha Vehicle Tool

**Desktop editor for GTA V vehicles — organize, edit, and export FiveM vehicle resources.**

[![CI](https://code.julscha.de/Julscha/JudryVehicleTool/actions/workflows/ci.yml/badge.svg)](https://code.julscha.de/Julscha/JudryVehicleTool/actions?workflow=ci.yml)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![.NET](https://img.shields.io/badge/.NET-8-purple)
![License](https://img.shields.io/badge/license-All%20rights%20reserved-red)

---

## What it does

Instead of editing XML files by hand and managing folders manually, Julscha Vehicle Tool gives you a project tree with dedicated editors for every GTA V meta format. When you're ready, a single click generates a complete, ready-to-drop FiveM resource.

```
Project
├── emergency_pack   →  exports as a FiveM resource
│   ├── police1
│   ├── police2
│   └── ambulance
└── sports_cars      →  separate FiveM resource
    ├── adder
    └── zentorno
```

---

## Features

| | Feature |
|---|---------|
| 🌳 | **Project hierarchy** — Project → Resources → Vehicles |
| ⚙️ | **Handling editor** — Full `handling.meta` with sliders, presets, batch apply |
| 🎨 | **Car Variations** — Color combos, kits, liveries, plate probabilities |
| 🚨 | **Siren pool** — Project-level siren groups, assigned to vehicles by ID |
| 🚗 | **Vehicle Meta** — Model name, class, flags, audio hash |
| 🧊 | **3D Viewer** — LOD switching, wireframe, texture panel |
| 📦 | **FiveM export** — Auto-generates `fxmanifest.lua`, meta XMLs, decrypted stream files |
| 🔒 | **Encryption** — optional DPAPI locally; AES-256-GCM for password-protected ZIP sharing |
| 🔍 | **Tree filter** — Live search/filter in the sidebar |
| ✅ | **Validation** — Warns before export on missing fields or mismatched IDs |

---

## Requirements

- Windows 10 / 11 (64-bit)
- No .NET installation needed — the release `.exe` is self-contained

---

## Getting Started

**Download** `JulschaVehicleTool-<version>-win-x64.exe` from the [Releases page](https://code.julscha.de/Julscha/JudryVehicleTool/releases) and run it — no installer needed.

Or build from source:

```bash
git clone https://code.julscha.de/Julscha/JudryVehicleTool.git
cd JudryVehicleTool
dotnet run --project src/JulschaVehicleTool.App
```

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+N` | New Project |
| `Ctrl+O` | Open Project |
| `Ctrl+S` | Save Project |
| `Ctrl+Shift+S` | Save As |
| `F2` | Rename selected item |
| `Delete` | Remove selected vehicle |

---

## Documentation

Full documentation: **[docs/](https://code.julscha.de/Julscha/JudryVehicleTool/src/branch/main/docs)** (MkDocs — `pip install mkdocs-material && mkdocs serve` for a local site)

- [Installation](https://code.julscha.de/Julscha/JudryVehicleTool/src/branch/main/docs/getting-started/installation.md)
- [Your First Project](https://code.julscha.de/Julscha/JudryVehicleTool/src/branch/main/docs/getting-started/quickstart.md)
- [Handling Editor](https://code.julscha.de/Julscha/JudryVehicleTool/src/branch/main/docs/editors/handling.md)
- [Siren Pool](https://code.julscha.de/Julscha/JudryVehicleTool/src/branch/main/docs/editors/sirens.md)
- [FiveM Export](https://code.julscha.de/Julscha/JudryVehicleTool/src/branch/main/docs/export/fivem.md)

---

## Tech Stack

- .NET 8 (WPF)
- CommunityToolkit.Mvvm
- MaterialDesignThemes
- HelixToolkit.Wpf.SharpDX (3D rendering)
- CodeWalker.Core (GTA V binary parsing)
- Pfim (DDS texture decoding)

---

## License

All rights reserved. © 2026 Julscha
