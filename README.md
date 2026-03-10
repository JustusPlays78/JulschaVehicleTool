# Julscha Vehicle Tool

Desktop editor for GTA V vehicles — organize, edit, and export FiveM vehicle resources.

## Features

- **Project hierarchy**: Project → Resources → Vehicles
- **Inline meta editing**: Handling, VehicleMeta, CarVariations, Sirens (CarCols) — all stored in the project
- **3D model viewer**: View YFT models with LOD switching and texture support
- **FiveM export**: Per-resource export with auto-generated fxmanifest.lua and vehicle_names.lua
- **Encrypted storage**: DPAPI for local projects, AES-256 password-protected ZIP for sharing
- **Handling presets**: Sports Car, Muscle Car, SUV, Motorcycle, Boat, Helicopter
- **Auto-save**: Every 5 minutes + on vehicle/resource switch

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Getting Started

```bash
git clone https://github.com/Julscha/JulschaVehicleTool.git
cd JulschaVehicleTool
dotnet run --project src/JulschaVehicleTool.App
```

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+N | New Project |
| Ctrl+O | Open Project |
| Ctrl+S | Save Project |
| Ctrl+Shift+S | Save As |
| Delete | Remove selected vehicle |

## Tech Stack

- .NET 8 (WPF)
- CommunityToolkit.Mvvm
- MaterialDesignThemes (Dark, Grey/BlueGrey)
- HelixToolkit.Wpf.SharpDX (3D rendering)
- CodeWalker.Core (GTA V binary parsing)

## Documentation

See [docs/DOKUMENTATION.md](docs/DOKUMENTATION.md) for full documentation (German).

## License

All rights reserved. © 2026 Julscha
