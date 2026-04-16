# Julscha Vehicle Tool

**Desktop editor for GTA V vehicles — organize, edit, and export FiveM vehicle resources.**

---

## What is it?

Julscha Vehicle Tool lets you manage an entire FiveM vehicle pack from one place. Instead of manually editing XML files and juggling folders, you work with a visual project tree and dedicated editors for every meta file format. When you're done, a single click generates a ready-to-drop FiveM resource.

```
Project
├── emergency_pack  (FiveM resource)
│   ├── police1
│   ├── police2
│   └── ambulance
└── sports_cars     (FiveM resource)
    ├── adder
    └── zentorno
```

---

## Key Features

| Feature | Description |
|---------|-------------|
| **Project hierarchy** | Project → Resources → Vehicles, with inline meta storage |
| **Handling editor** | Full `handling.meta` editor with visual sliders and presets |
| **Car Variations** | Color combinations, kits, plate probabilities |
| **Siren pool** | Project-level siren group editor; vehicles reference groups by ID |
| **Vehicle Meta** | `vehicles.meta` editor for model name, class, flags, audio |
| **3D Viewer** | YFT model viewer with LOD switching, wireframe, texture panel |
| **FiveM export** | Auto-generates `fxmanifest.lua`, meta XMLs, and decrypted stream files |
| **Encrypted storage** | DPAPI locally; AES-256-GCM + PBKDF2 for ZIP sharing |
| **Batch presets** | Apply handling presets to multiple vehicles at once via checkboxes |
| **Auto-save** | Every 5 minutes and on every context switch |

---

## How does it relate to FiveM?

FiveM loads GTA V vehicles from **resources** — folders containing stream files (`.yft`, `.ytd`) and data files (`.meta`). This tool manages exactly that structure internally and produces a correct resource on export.

```
police_pack/          ← what the tool exports
├── fxmanifest.lua
├── stream/
│   ├── police1.yft
│   └── police1.ytd
├── data/
│   ├── police1/
│   │   ├── handling.meta
│   │   ├── vehicles.meta
│   │   └── carvariations.meta
│   └── carcols.meta       ← shared siren pool for this resource
└── vehicle_names.lua
```

---

## Quick Links

- [Installation →](getting-started/installation.md)
- [Your First Project →](getting-started/quickstart.md)
- [Handling Editor →](editors/handling.md)
- [FiveM Export →](export/fivem.md)
- [Keyboard Shortcuts →](reference/shortcuts.md)
