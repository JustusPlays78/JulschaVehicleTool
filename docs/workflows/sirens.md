# Workflow: Siren Setup for Emergency Vehicles

This workflow covers configuring siren lights for one or more emergency vehicles. Sirens are defined once at the project level in a shared pool; vehicles reference a group from that pool by ID.

---

## Overview

The siren system has two parts:

1. **Project siren pool** — where you define siren groups (colors, flash patterns, sequencers). Accessed by clicking the project node at the top of the sidebar tree.
2. **Vehicle Sirens tab** — where you assign one of those groups to a specific vehicle.

This separation means twenty vehicles in a pack can all reference the same two or three siren groups without duplicating configuration.

---

## Step 1: Open the Siren Pool Editor

Click the **project node** (the top-level item in the sidebar tree, showing the project name). The right panel switches to the Siren Pool editor.

If the project has no siren groups yet, the editor shows an empty list with an **Add Siren Setting** button.

---

## Step 2: Add a Siren Group

Click **Add Siren Setting**. A new group appears with:

- **ID** — assigned automatically. If the pool is empty, the first ID is `1000`. Otherwise it is `max(existing IDs) + 1`. The ID is what vehicles store in their carvariations data; do not change it after assigning it to vehicles.
- **Name** — defaults to `Siren 1000` (or the auto-assigned ID). Rename it to something descriptive, for example `Police Primary` or `Ambulance Rear`.

---

## Step 3: Configure the Group

Each siren group contains one or more **Siren Light** entries. These map to the individual light attachment points defined in the vehicle model.

### Siren Light Fields

| Field | Purpose |
|---|---|
| LightGroup | Which model light group this entry controls (0–3) |
| Rotate | Whether the light head rotates |
| Scale | Size multiplier for the light effect |
| SpeedMultiplier | Rotation speed relative to base |
| CoronaIntensity | Brightness of the corona bloom |
| CoronaSize | Size of the corona |
| Direction | Angle the light faces when not rotating |
| Hearted | Whether the flash pattern is heart-shaped |

### Flash Color and Pattern

Each siren light has a primary and secondary color (RGBA). The 32-bit sequencer value controls the flash pattern over one cycle. The **Sequencer** is displayed as a 4×8 bit grid — each bit is one step. Click a cell to toggle it. Drag across cells to set a range.

Four sequencer channels are shown:
- **Left Head** — front-left lights
- **Right Head** — front-right lights
- **Left Tail** — rear-left lights
- **Right Tail** — rear-right lights

The **Sequencer BPM** field sets the playback speed of all sequencers in this group.

### Common Patterns

**Police 2-color (red/blue alternating):**
- Left Head + Left Tail: Red primary, sequencer = `10101010...` (alternating bits)
- Right Head + Right Tail: Blue primary, sequencer = `01010101...` (inverted)

**Amber warning (steady pulse):**
- All channels: Amber primary, sequencer = `11110000 11110000` (on for half, off for half)

**Strobe (fast double-flash):**
- Sequencer = `11001100 11001100`, BPM set high (around 120–160)

---

## Step 4: Clone a Group for Variants

If you need a second pattern that is similar to the first — for example a traffic advisor mode with different rear sequencers — use **Clone Siren Setting** on the existing group. The clone receives a new auto-incremented ID and a name suffixed with ` (Copy)`. Adjust only the fields that differ.

---

## Step 5: Assign a Group to a Vehicle

1. Select the vehicle in the sidebar tree.
2. Click the **Sirens** tab in the right panel.
3. The **Siren Group** dropdown lists all groups from the project pool by name.
4. Select the appropriate group.

The selection writes the group's ID to `CarVariation.SirenSettings`. The read-only sequencer preview below the dropdown shows the selected group's sequencer pattern without allowing edits.

To remove the assignment, click the **Clear** button next to the dropdown. This sets `SirenSettings` to `0`.

Vehicles that do not need sirens (civilian cars, non-emergency) should have no group assigned.

---

## Step 6: Verify the Assignment

With the vehicle still selected, switch to the **Variations** tab. The `SirenSettings` field should show the numeric ID of the group you assigned. If it shows `0`, no group is assigned.

If the ID shown does not match any group in the pool, the siren will not work correctly in game. Check that the group was not deleted or that the ID was not manually edited to a value that no longer exists.

---

## Step 7: Export

Export the resource normally. The tool generates `data/carcols.meta` containing **only** the siren groups that are referenced by vehicles in that resource. Groups defined in the pool but not assigned to any vehicle in the resource are excluded from the export.

The manifest entry generated is:

```lua
data_file 'CARCOLS_FILE' 'data/carcols.meta'
```

---

## Troubleshooting

**Siren dropdown is empty**
The project siren pool has no groups. Click the project node and add at least one group before assigning.

**Siren lights do not flash in game**
- Verify that the `SirenSettings` ID in the Variations tab matches a group ID in the pool.
- Verify that `data/carcols.meta` was exported and is present in the resource's `data/` folder.
- Check that the manifest correctly references the carcols file.

**Siren sound does not play**
The audio hash in the Vehicle tab (`AudioName`) must map to an audio entry that has a siren defined, such as `POLICE_CAR` or `AMBULANCE`. Lights work independently of audio; sound does not.

**Wrong lights flash**
The `LightGroup` field in the siren light entry must match the attachment point index defined in the vehicle model (`.yft`). If you are using a custom model, consult the model's documentation for the correct group indices.
