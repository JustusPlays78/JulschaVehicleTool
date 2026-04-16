# Workflow: Clone a Vehicle and Create Variants

This workflow covers creating multiple variants of the same base vehicle — for example a standard and a slicktop police car, or a vehicle with two different livery sets.

---

## When to Use This Workflow

- You have one fully configured vehicle and need a second variant with slightly different handling, a different livery, or a different siren assignment.
- You want to create a dirty or damaged variant alongside a clean base vehicle.
- You need to duplicate a vehicle's configuration to use as a starting point for a different model.

---

## Step 1: Fully Configure the Base Vehicle First

Clone works best when the source vehicle is complete:

- Binary files imported (`.yft`, `.ytd`)
- `vehicles.meta` imported or entered (ModelName, HandlingId, VehicleClass, AudioName)
- Handling data set (either imported or a preset applied)
- Color variations imported or set
- Siren group assigned if applicable

A clone copies all of this data. Configuring the base first means you only need to adjust the differences in the clone rather than building from scratch.

---

## Step 2: Clone the Vehicle

Right-click the vehicle in the sidebar tree and select **Clone Vehicle**.

The tool creates a deep copy of the vehicle with a new name:

- If the original is named `police1`, the clone is named `police1_copy`.
- If `police1_copy` already exists in the same resource, the clone is named `police1_copy_2`, then `police1_copy_3`, and so on.

The clone appears in the tree immediately. All model data, handling values, color variations, and siren assignments are copied from the original.

---

## Step 3: Rename the Clone

Select the clone in the tree. Press `F2` to rename it. Use the name that matches the variant's intended model name — this becomes the `ModelName` in `vehicles.meta` and must match the `.yft` filename you intend to ship.

!!! warning "Name determines model identity"
    The vehicle name in the tree is used as the model name in the exported `vehicles.meta`. If the clone will have a different model file (a separate `.yft`), the name must match that file exactly.

Renaming also renames the storage folder on disk (`vehicles/police1_copy/` → `vehicles/police1_slicktop/`). All relative file paths stored in the project are updated automatically.

---

## Step 4: Import New Binary Files (if the model differs)

If the variant uses a different model or different textures:

1. With the clone selected, click **Import Files** in the Resource Settings panel (or right-click → **Import Vehicle Files**).
2. Select the variant's `.yft` and `.ytd` files.

The new files overwrite the clone's binary references. The original vehicle's files are not affected.

If the variant shares the same model and textures as the base (for example, only the handling or siren assignment differs), skip this step. Both vehicles will reference the same stored binaries, which is valid — the exporter copies the files for each vehicle independently.

---

## Step 5: Adjust the Variant's Configuration

Change only what differs between the base and the variant:

**Different siren assignment:**
Go to the Sirens tab and pick a different group from the dropdown, or clear the assignment entirely.

**Different handling:**
Go to the Handling tab and adjust values, or apply a different preset from the Presets menu.

**Different color combinations:**
Go to the Variations tab and modify the color entries.

**Different vehicle class or audio:**
Go to the Vehicle tab and update the relevant fields. Ensure `HandlingId` still matches the `HandlingName` in the Handling editor if the handling was changed.

---

## Step 6: Update the Vehicle Tab

Even if you did not change the model, verify the Vehicle tab fields for the clone:

| Field | What to check |
|---|---|
| ModelName | Must match the `.yft` filename (without extension) |
| HandlingId | Must match HandlingName in the Handling editor |
| GameName | Update to reflect the variant (e.g. `Police Slicktop`) |
| AudioName | Adjust if the variant should have different audio behavior |

---

## Step 7: Validate and Export

Select the resource. Check for validation errors in the Resource Settings panel. Both the base vehicle and the clone must pass validation independently — a cloned vehicle that shares the same ModelName as its source will cause a conflict in game even if both export without errors.

Export the resource. The `stream/` folder will contain separate files for each vehicle. If both the base and the clone use the same `.yft` and `.ytd` files (same model, same textures), the exporter still writes separate copies — one named `police1.yft` and one named `police1_slicktop.yft` — so they can be registered independently in FiveM.

---

## Example: Slicktop Variant

Starting from a configured `police1`:

1. Clone → `police1_copy` → rename to `police1_slicktop`
2. Import `police1_slicktop.yft` and `police1_slicktop.ytd` (the model without a lightbar)
3. Sirens tab → clear the siren assignment (slicktop has no lights)
4. Vehicle tab → set GameName to `Police Slicktop`
5. Handling is identical — no change needed
6. Export → both `police1` and `police1_slicktop` appear in `stream/` with their own meta files

In FiveM: `/spawn police1` and `/spawn police1_slicktop` work independently.
