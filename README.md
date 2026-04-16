# Julscha Vehicle Tool

**Desktop editor for GTA V vehicles — organize, edit, and export FiveM vehicle resources.**

[![Build](https://github.com/JustusPlays78/JulschaVehicleTool/actions/workflows/build.yml/badge.svg)](https://github.com/JustusPlays78/JulschaVehicleTool/actions/workflows/build.yml)
[![Docs](https://github.com/JustusPlays78/JulschaVehicleTool/actions/workflows/docs.yml/badge.svg)](https://justusplays78.github.io/JulschaVehicleTool)
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
| 🔒 | **Encrypted storage** — DPAPI locally; AES-256-GCM for ZIP sharing |
| 🔍 | **Tree filter** — Live search/filter in the sidebar |
| ✅ | **Validation** — Warns before export on missing fields or mismatched IDs |

---

## Requirements

- Windows 10 / 11 (64-bit)
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Getting Started

**Download** the latest release from the [Releases page](https://github.com/JustusPlays78/JulschaVehicleTool/releases), extract, and run `JulschaVehicleTool.exe`.

Or build from source:

```bash
git clone https://github.com/JustusPlays78/JulschaVehicleTool.git
cd JulschaVehicleTool
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

Full documentation: **[justusplays78.github.io/JulschaVehicleTool](https://justusplays78.github.io/JulschaVehicleTool)**

- [Installation](https://justusplays78.github.io/JulschaVehicleTool/getting-started/installation/)
- [Your First Project](https://justusplays78.github.io/JulschaVehicleTool/getting-started/quickstart/)
- [Handling Editor](https://justusplays78.github.io/JulschaVehicleTool/editors/handling/)
- [Siren Pool](https://justusplays78.github.io/JulschaVehicleTool/editors/sirens/)
- [FiveM Export](https://justusplays78.github.io/JulschaVehicleTool/export/fivem/)

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
