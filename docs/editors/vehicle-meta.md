# Vehicle Meta Editor

The Vehicle Meta editor edits `vehicles.meta` — the file that registers your model with the game engine and defines its class, behavior, and audio.

---

## Overview

Select a vehicle → **Vehicle** tab.

---

## Identity

| Field | Description | Notes |
|-------|-------------|-------|
| **Model Name** | The spawn name (e.g. `police1`). Must match the `.yft` filename. | Case-sensitive in some contexts |
| **TXD Name** | Texture dictionary name. Usually the same as model name. | |
| **Handling ID** | Must exactly match `HandlingName` in the [Handling editor](handling.md). | Mismatch shows ⚠ warning |
| **Game Name** | In-game display name (GXT2 label or raw text). | Shows in menus |
| **Manufacturer Name** | Manufacturer label (GXT2). | e.g. `BRAND_BENEFACTOR` |

!!! warning "HandlingId must match"
    The game looks up physics data by HandlingId. If it doesn't match the handling file entry exactly, the vehicle will use wrong physics or default handling.

---

## Classification

| Field | Description |
|-------|-------------|
| **Type** | Vehicle base type: `VEHICLE_TYPE_CAR`, `VEHICLE_TYPE_BIKE`, `VEHICLE_TYPE_BOAT`, `VEHICLE_TYPE_HELI`, etc. |
| **Vehicle Class** | The class shown in stats menus. `VC_EMERGENCY`, `VC_SPORT`, `VC_SUV`, etc. |
| **Plate Type** | Default plate display: `VPT_FRONT_AND_BACK_PLATES`, `VPT_BACK_PLATES`, etc. |
| **Wheeltype** | Wheel category for tuning: `WHEELTYPE_CAR_STANDARD`, `WHEELTYPE_MOTORBIKE`, etc. |
| **Dashboard Type** | Dashboard/instrument cluster type |

---

## Physics / Limits

| Field | Description |
|-------|-------------|
| **Mass** | Physics mass (should match `fMass` in handling for consistency) |
| **Percentage of Body in Water** | For boats: how much of the body is submerged at rest |
| **Drive Train** | Drive layout used for visual drivetrain components |
| **Max Num of Passengers** | Passenger seat count |
| **Max Num of Seats** | Total seat count including driver |

---

## Audio

| Field | Description |
|-------|-------------|
| **Audio Name Hash** | Reference to the vehicle's audio profile (e.g. `POLICE_CAR`, `AMBULANCE`). Controls engine sound, siren sound, etc. |

!!! tip "Police sirens need the right audio hash"
    For a working police siren sound, set `Audio Name Hash` to `POLICE_CAR` (or another emergency vehicle hash). Without this, only lights will work — no sound.

---

## Flags

Vehicle flags are a bitmask controlling dozens of behaviors:

| Common Flag | Effect |
|-------------|--------|
| `FLAG_HAS_LIVERY` | Allows livery swapping in Los Santos Customs |
| `FLAG_DOUBLE_REAR_WHEELS` | Shows dual rear wheels (trucks) |
| `FLAG_NO_BOOT` | Disables boot animation |
| `FLAG_LAW_ENFORCEMENT` | Marks vehicle as police vehicle |
| `FLAG_EMERGENCY_SERVICE` | Marks vehicle as emergency service |
| `FLAG_HAS_EMERGENCY_LIGHTS` | Vehicle has siren lights |
| `FLAG_NO_RESPRAY` | Prevents color change in Los Santos Customs |
