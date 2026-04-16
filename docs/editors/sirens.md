# Sirens

The siren system in GTA V is driven by `carcols.meta`. This tool manages it at the **project level** — one shared pool of siren groups, referenced by vehicles via an integer ID.

---

## Architecture

```
Project
└── Siren Pool (carcols.meta)
    ├── Siren Group 1001  (e.g. "police_2color")
    │   ├── Light 1
    │   ├── Light 2
    │   └── ...
    └── Siren Group 1002  (e.g. "ambulance_white")
        └── ...

Vehicle (police1)
└── Car Variation → SirenSettings = 1001   ← references group by ID

Vehicle (ambulance)
└── Car Variation → SirenSettings = 1002
```

This mirrors how FiveM actually works: `carcols.meta` is a shared file per resource, not one per vehicle. The tool enforces this correctly.

---

## Editing the Siren Pool

1. **Select the project node** (top item in the sidebar tree).
2. The **Siren Pool Editor** opens on the right — this is the full editor for all siren groups.

### Siren Groups

Each **Siren Group** (`<Item>` in carcols.meta) has:

| Field | Description |
|-------|-------------|
| **ID** | Integer identifier. Referenced by vehicles via `<sirenSettings value="X"/>`. Must be unique. |
| **Name** | Human-readable label (only used in the tool, not exported). |
| **Time Multiplier** | Global speed of all flashiness sequences. 1.0 = normal. |
| **Sequencer BPM** | BPM used when `SyncToBpm` is enabled on individual lights. |
| **Use Real Lights** | Enable real-time light casting (performance hit on low-end servers). |
| **Head/Tail Light Sequencers** | 32-bit patterns controlling when each exterior light flashes. |

### Siren Lights (per group, max 20)

Each group contains up to 20 individual lights:

| Category | Fields |
|----------|--------|
| **Color & Intensity** | Hex color (`0xFFRRGGBB`), intensity |
| **Position** | Light group index (which bone it attaches to) |
| **Rotation** | Delta, start angle, speed, 32-bit sequencer, direction, sync to BPM |
| **Flashiness** | Delta, start, speed, 32-bit sequencer, direction, sync to BPM |
| **Corona** | Intensity, size, pull distance, face-camera |
| **Flags** | Rotate, Scale, Flash, Light, SpotLight, CastShadows |

### The 32-bit Sequencer

The sequencer is a `uint` (32 bits) where each bit represents one "beat":

- `0xFFFFFFFF` (all 1s) = always on
- `0x00000000` (all 0s) = always off
- `0xAAAAAAAA` = alternating on/off at BPM rate

The bit grid in the editor lets you paint patterns visually by clicking or dragging.

---

## Assigning a Siren Group to a Vehicle

1. Select a **vehicle** in the tree.
2. Go to the **Sirens** tab.
3. Use the dropdown to pick a siren group from the project pool.
4. The assignment is saved to `CarVariation.SirenSettings` (the integer ID).

!!! info "No siren groups yet?"
    If the dropdown is empty, select the project node first to create siren groups in the pool.

---

## Export

When exporting a resource, the tool:

1. Collects all siren group IDs referenced by vehicles in that resource.
2. Filters the project pool to those IDs only.
3. Writes a single `data/carcols.meta` for the entire resource.

The manifest gets `data_file 'CARCOLS_FILE' 'data/carcols.meta'` automatically.

!!! warning "ID conflicts across resources"
    Siren group IDs are **global** in GTA V. If two resources both use ID `1000`, whichever loads last wins. Use unique IDs per project (e.g. start at 1000, 2000, 3000 for different packs).

---

## Common Patterns

### 2-Color Alternating (Police)

- Light 1: Red, `FlashinessSequencer = 0xAAAAAAAA` (alternating)
- Light 2: Blue, `FlashinessSequencer = 0x55555555` (inverse of Light 1)
- Both: `FlashinessSyncToBpm = true`, BPM around 220

### Steady-Burn Amber (Construction)

- Color: `0xFFFFA500`
- `FlashinessSequencer = 0xFFFFFFFF` (always on)
- `RotationSequencer = 0xFFFFFFFF` + `Rotate = true`

### Strobe White

- Color: `0xFFFFFFFF`
- `FlashinessSequencer = 0x88888888` (short pulses)
- `FlashinessSyncToBpm = true`, high BPM
