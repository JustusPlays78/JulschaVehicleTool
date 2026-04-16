# Car Variations

The Car Variations editor edits `carvariations.meta` — the file that defines which colors, kits, and extras a vehicle can have in-game.

---

## Overview

Select a vehicle → **Variations** tab.

The main sections:

| Section | Description |
|---------|-------------|
| **Color Combinations** | Sets of primary/secondary/pearlescent/rim/interior/dash colors |
| **Kits** | Body kit variants (usually `0_default_modkit`) |
| **Plate Probabilities** | Likelihood of each plate style spawning |
| **Settings** | Light Settings ID, Siren Settings ID |

---

## Color Combinations

Each combination is one possible color set that can spawn randomly in traffic.

| Field | Description | Range |
|-------|-------------|-------|
| **Primary Color** | Main body color index | 0–159 |
| **Secondary Color** | Secondary color (roof, mirrors, etc.) | 0–159 |
| **Pearlescent Color** | Pearlescent layer overlay | 0–159 |
| **Rim Color** | Wheel rim color | 0–159 |
| **Interior Trim Color** | Seat/interior trim | 0–159 |
| **Dashboard Color** | Dashboard color | 0–159 |

!!! tip "GTA V Color Indices"
    0–12 = matte colors, 13–74 = standard colors, 75–111 = metallic, 112–127 = pearlescent, 128–143 = UV reactive. A full color chart is available in the OpenIV community resources.

### Liveries

Each color combination can also have livery slots. These are boolean flags that map to livery texture indices on the model's YTD.

---

## Kits

Kits reference body kit mod slots. For most custom vehicles, one entry is sufficient:

```
0_default_modkit
```

If your model has multiple visual variants, add one kit per variant and name them accordingly.

---

## Plate Probabilities

Controls which plate style spawns. Each entry is a probability weight.

| Common Names | Plate Style |
|-------------|-------------|
| `PLATE_TYPE_FRONT_AND_BACK` | Standard front + back plate |
| `PLATE_TYPE_FRONT_ONLY` | Front plate only |
| `PLATE_TYPE_BACK_ONLY` | Rear plate only |
| `PLATE_TYPE_NONE` | No plate |

All probabilities are relative weights — they don't need to sum to 100.

---

## Siren Settings ID

The **Siren Settings** field is an integer that references a siren group in the project's siren pool.

- `0` = no siren assigned
- Any other value = ID of a group defined in the [Siren Pool](sirens.md)

You can set this directly here, or use the **Sirens tab** which provides a dropdown with all named groups.

---

## Light Settings ID

References a `<Item>` in the `lightSettings` block of `carcols.meta`. For most custom vehicles this can be `0`.
