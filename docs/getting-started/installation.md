# Installation

## Requirements

- **Windows 10 or 11** (64-bit)
- No .NET installation needed — the release `.exe` is self-contained

---

## Option A — Download a Release (recommended)

1. Go to the [Releases page](https://code.julscha.de/Julscha/JudryVehicleTool/releases)
2. Download `JulschaVehicleTool-<version>-win-x64.exe`
3. Put it anywhere and run it — no installer, no extraction needed

!!! note "Windows SmartScreen"
    On first launch Windows may show a SmartScreen warning. Click **More info → Run anyway**.

---

## Option B — Build from Source

```bash
git clone https://code.julscha.de/Julscha/JudryVehicleTool.git
cd JudryVehicleTool
dotnet run --project src/JulschaVehicleTool.App
```

Or build a release binary:

```bash
build.bat
```

The output ends up in `publish/`.

---

## Updating

Simply replace the old executable with the new one. Projects and settings are stored in your documents folder, not next to the `.exe`.
