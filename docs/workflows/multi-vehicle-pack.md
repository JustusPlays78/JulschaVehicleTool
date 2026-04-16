# Workflow: Multi-Vehicle Pack

This workflow covers building a resource that contains several vehicles — for example a police pack with five cars, each sharing the same siren configuration.

---

## When to Use This Workflow

- You have a folder of vehicles extracted from a pack or from OpenIV
- The meta files cover multiple vehicles in a single file (one `handling.meta` with multiple `<Item>` entries)
- You want to apply the same handling preset to all vehicles at once

---

## Step 1: Create Project and Resource

Create a new project (`Ctrl+N`) and add one resource. Rename the resource to the intended FiveM resource name (e.g. `emergency_pack`).

---

## Step 2: Import a Vehicle Folder

If your vehicles are in a single folder, use the bulk import:

1. Select the resource in the tree.
2. **Edit > Import Vehicle Folder**.
3. Select the folder containing all `.yft` and `.ytd` files.

The tool scans for `.yft` files that do not end in `_hi`. For each one it finds, it:

- Creates a vehicle with that name
- Imports all matching binary files (main YFT, `_hi.yft`, YTD, `+hi.ytd`) automatically
- Adds the vehicle to the selected resource

A progress bar shows each vehicle as it is processed.

After import, all vehicles appear in the tree. The last imported vehicle is selected.

---

## Step 3: Import Multi-Vehicle Meta Files

With any vehicle or the resource selected, use **Edit > Import Meta File** and select the meta files.

When a meta file contains multiple entries (e.g. a `handling.meta` with entries for `police1`, `police2`, `police3`), the tool auto-matches entries to vehicles in the resource:

- Matching is done by name similarity (case-insensitive substring and prefix matching)
- The result is logged: each match shows the parsed name, the matched vehicle name, and a confidence score
- Entries with no matching vehicle are skipped and logged

If the automatic matching misses vehicles, rename the vehicles in the tree (`F2`) to better match the meta entry names, then re-import.

!!! tip "Best naming convention"
    Keep vehicle names identical to the identifiers in the meta files. If the meta file has `<handlingName>POLICE</handlingName>` and a separate `<handlingName>POLICE2</handlingName>`, your vehicle names should be `police` and `police2`. Exact matches are always preferred.

---

## Step 4: Apply a Handling Preset to All Vehicles

If the vehicles do not have handling data yet, or you want to baseline all of them:

1. Check the checkboxes next to each vehicle in the sidebar tree.
2. Open **Presets menu** and pick a preset (e.g. Sports Car, SUV / Truck).

The preset is applied to every checked vehicle simultaneously. If no vehicles are checked, the preset applies only to the currently selected vehicle.

After applying, the Handling tab switches into view showing the currently selected vehicle's new values.

!!! note
    Checking a vehicle checkbox does not change the tree selection. Clicking the checkbox row does not navigate to that vehicle — use a direct click on the vehicle name for that.

---

## Step 5: Configure the Shared Siren Pool

For an emergency pack, all vehicles typically share a small set of siren patterns.

1. Click the **project node** (top of the sidebar tree).
2. The Siren Pool editor opens.
3. Use **Add Siren Setting** to create groups. Each group gets an auto-incremented ID (starts at 1000 if the pool is empty, otherwise `max existing ID + 1`).
4. Configure each group's lights, colors, and sequencers.
5. Use **Clone Siren Setting** to duplicate a group and adjust it for variants.

---

## Step 6: Assign Siren Groups to Vehicles

For each vehicle that needs lights:

1. Select the vehicle in the tree.
2. Go to the **Sirens tab**.
3. Pick the appropriate siren group from the dropdown.

The dropdown lists all groups from the project pool by name. Selecting one writes the group's ID to `vehicle.CarVariation.SirenSettings`.

Vehicles that do not need sirens (civilian cars) should have no group assigned (leave the dropdown empty or use the Clear button).

---

## Step 7: Validate and Export

Select the resource. Check for validation errors in the Resource Settings panel. Common issues with multi-vehicle imports:

- Missing `vehicles.meta` on a vehicle (ModelName not set) — Error, blocks export
- HandlingId mismatch (meta entry was not matched) — Warning
- Siren group ID assigned but no matching group in the pool — check that the ID exists in the project siren pool

Once validated, click **Export FiveM Resource**. The output structure:

```
emergency_pack/
├── fxmanifest.lua
├── stream/
│   ├── police1.yft
│   ├── police1.ytd
│   ├── police2.yft
│   ├── police2.ytd
│   └── ... (all vehicles, flattened)
├── data/
│   ├── carcols.meta          (only groups referenced by vehicles in this resource)
│   ├── police1/
│   │   ├── handling.meta
│   │   ├── vehicles.meta
│   │   └── carvariations.meta
│   └── police2/
│       ├── handling.meta
│       ├── vehicles.meta
│       └── carvariations.meta
└── vehicle_names.lua         (if enabled)
```

!!! warning "Stream folder is flat"
    All stream files land directly in `stream/` with no subfolders. If two vehicles have a file with the same filename, the export warns about a collision. This should not happen if vehicle names are unique.

---

## Step 8: Deploy

Copy the exported folder to `resources/[vehicles]/`. Add one `ensure` line per resource to `server.cfg`.

To spawn a specific vehicle by model name: `/spawn police1`
