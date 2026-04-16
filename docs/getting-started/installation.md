# Installation

## Requirements

- **Windows 10 or 11** (64-bit)
- **.NET 8 Runtime** — [download here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

---

## Option A — Download a Release (recommended)

1. Go to the [Releases page](https://github.com/Schallenkammer/JulschaVehicleTool/releases)
2. Download the latest `.zip`
3. Extract it anywhere — no installer needed
4. Run `JulschaVehicleTool.exe`

!!! note "Windows SmartScreen"
    On first launch Windows may show a SmartScreen warning. Click **More info → Run anyway**.

---

## Option B — Build from Source

```bash
git clone https://github.com/Schallenkammer/JulschaVehicleTool.git
cd JulschaVehicleTool
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
