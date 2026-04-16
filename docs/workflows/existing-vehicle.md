# Workflow: Import and Edit an Existing Vehicle

This is the most common starting point. You have a GTA V vehicle extracted from OpenIV and want to convert it into a working FiveM resource.

---

## Prerequisites

- Vehicle files extracted from OpenIV: `.yft`, optionally `_hi.yft`, `.ytd`, optionally `+hi.ytd`
- The `.meta` files for that vehicle: `handling.meta`, `vehicles.meta`, `carvariations.meta`, optionally `carcols.meta`

---

## Step 1: Create a Project

Open the tool. On the welcome screen, click **New Project** (`Ctrl+N`) and choose an empty folder. This folder becomes the project root.

The tool creates:
```
MyProject/
├── project.julveh   (encrypted JSON, initially empty)
└── vehicles/        (encrypted binary storage)
```

---

## Step 2: Add a Resource

A project holds one or more FiveM resources. Click the **Add Resource** button in the sidebar header or use **Edit > Add Resource**.

Rename the resource immediately with `F2` — use the name the FiveM resource folder will have (e.g. `police_pack`).

---

## Step 3: Add a Vehicle

Select the resource. Click **Add Vehicle** (sidebar or Edit menu). A new vehicle appears with default sedan values. Rename it with `F2` to match the `.yft` filename exactly (e.g. `police1`).

!!! warning "Name must match the .yft filename"
    The vehicle name in the tree determines the output `ModelName`. It must match the `.yft` file without extension. A mismatch means the vehicle will not spawn in game.

---

## Step 4: Import Binary Files

With the vehicle selected, click **Import Files** in the Resource Settings panel or right-click the vehicle and select **Import Vehicle Files**.

Select the `.yft` and `.ytd` files. The tool accepts:

| File pattern | Assigned to |
|---|---|
| `name.yft` | Main LOD model |
| `name_hi.yft` | High-detail model |
| `name.ytd` | Main texture dictionary |
| `name+hi.ytd` | High-detail texture dictionary |

Each file is encrypted with DPAPI and stored in `vehicles/police1/` inside the project folder.

---

## Step 5: Import Meta Files

Right-click the vehicle or use **Edit > Import Meta File**. Select one or more `.meta` files in any order.

The tool detects type by filename:

| Filename contains | Loaded as |
|---|---|
| `handling` | Handling data |
| `vehicles` | Vehicle meta |
| `carvariations` | Color variations |
| `carcols` | Siren groups (merged into project pool) |

**Single entry + single vehicle selected**: The entry is assigned directly to this vehicle regardless of name.

**Multiple entries in one file**: The tool auto-matches entries to vehicles in the resource by name similarity. The match confidence is logged. If no match is found for an entry, it is skipped with a log message.

**carcols.meta**: Siren groups are merged into the project-level siren pool. Groups with an ID that already exists in the pool are skipped.

After import, the editors refresh automatically.

---

## Step 6: Edit

Select the vehicle. The tab bar appears on the right:

**Handling tab** — review and adjust drive physics. If the imported `handling.meta` matched correctly, all values are pre-filled. Use the **Presets menu** to apply a base preset and fine-tune from there.

**Variations tab** — verify color combinations loaded correctly. Check that `SirenSettings` ID matches a group in the siren pool if this is an emergency vehicle.

**Sirens tab** — if the vehicle should have lights, use the dropdown to assign a siren group from the project pool. If the pool is empty, click the project node (top of the sidebar tree) first to create groups.

**Vehicle tab** — verify `ModelName`, `HandlingId` (must exactly match `HandlingName` in the Handling editor), `VehicleClass`, and `AudioName`.

All edits are saved automatically to `project.julveh` on every change.

---

## Step 7: Validate

Before exporting, select the resource. The Resource Settings panel shows any validation issues:

| Issue | Severity | Effect |
|---|---|---|
| No VehicleMeta | Error | Export blocked |
| ModelName empty | Error | Export blocked |
| HandlingId != HandlingName | Warning | Export proceeds |
| No handling data | Warning | Export proceeds |
| No color variations | Warning | Export proceeds |

Fix errors before proceeding.

---

## Step 8: Export

In the Resource Settings panel:

1. Confirm Name, Author, Version.
2. Click **Choose Output Folder** and pick a destination.
3. Click **Export FiveM Resource**.

The output lands in `OutputFolder/police_pack/`. Copy that folder to the FiveM server.

---

## Step 9: Deploy and Test

```
resources/
└── [vehicles]/
    └── police_pack/
```

`server.cfg`:
```
ensure police_pack
```

Test in game:
```
/spawn police1
```

---

## Troubleshooting

**Vehicle does not spawn**
Check that `ModelName` in the Vehicle tab matches the `.yft` filename and the spawn command exactly.

**Wrong handling (too fast/slow, wrong drive type)**
Check that `HandlingId` in the Vehicle tab matches `HandlingName` in the Handling editor. A mismatch causes the game to use a fallback handling entry.

**No siren sound**
The `AudioName` field in the Vehicle tab must be set to an audio hash that has a siren defined (e.g. `POLICE_CAR`, `AMBULANCE`). Lights work regardless of audio hash; sound does not.

**Siren lights not flashing**
The siren group ID in Car Variations (`SirenSettings`) must match a group ID in `data/carcols.meta`. Verify the assignment in the Sirens tab.
