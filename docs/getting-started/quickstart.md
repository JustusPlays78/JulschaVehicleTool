# Your First Project

This guide walks through the full workflow: creating a project, importing a vehicle, editing its meta data, and exporting a working FiveM resource.

---

## Step 1 — Create a Project

1. Launch the tool. The welcome screen appears.
2. Click **New Project** (or `Ctrl+N`).
3. Choose an **empty folder** — this becomes the project root.

The tool creates a `project.julveh` file and a `vehicles/` subfolder.

---

## Step 2 — Add a Resource

A project can contain multiple FiveM resources. Start with one:

1. Click the **+** (Add Resource) button in the sidebar header.
2. A new resource appears in the tree — right-click to **Rename** it (e.g. `police_pack`).

---

## Step 3 — Add a Vehicle

1. Select your resource in the tree.
2. Click **+ Add Vehicle** (or use the Edit menu).
3. A new vehicle is created with sensible defaults (sedan template).
4. Rename it to match your model name (e.g. `police1`). This name **must** match the `.yft` filename.

---

## Step 4 — Import Files

### Binary files (YFT / YTD)

1. Select the vehicle in the tree.
2. Click **Import Files...** (or right-click → Import Vehicle Files).
3. Select the `.yft` and `.ytd` files.

The files are encrypted and stored inside `vehicles/police1/`.

### Importing a whole folder at once

If you have a folder with multiple vehicles:

1. Select the **resource** in the tree.
2. **Edit → Import Vehicle Folder...**
3. Pick the folder — the tool scans for `.yft` files and creates one vehicle per model name automatically.

### Meta files

1. Select the vehicle.
2. **Edit → Import Meta File...**
3. Select one or more `.meta` files (`handling.meta`, `vehicles.meta`, `carvariations.meta`, `carcols.meta`).

When you import a multi-vehicle meta file (e.g. a `handling.meta` with 3 entries), the tool auto-matches entries to vehicles by name.

---

## Step 5 — Edit

Select the vehicle — the editor tabs appear on the right:

| Tab | What it edits |
|-----|---------------|
| **3D Viewer** | Visual preview of the model |
| **Handling** | Drive physics (`handling.meta`) |
| **Variations** | Color combinations, kits (`carvariations.meta`) |
| **Sirens** | Siren group assignment from the project pool |
| **Vehicle** | Model name, class, flags (`vehicles.meta`) |

!!! tip "Handling Presets"
    Don't start from zero. Use **Presets menu** to apply a base preset (Sports Car, Muscle Car, SUV, Motorcycle, Boat, Helicopter), then fine-tune from there.

---

## Step 6 — Export

1. Select the **resource** in the tree (not a vehicle).
2. The Resource Settings panel opens on the right.
3. Fill in **Name**, **Author**, **Version**.
4. Click **Choose Output Folder** → **Export FiveM Resource**.

A progress bar runs. When done, the output folder contains a ready FiveM resource.

---

## Step 7 — Test on Server

1. Copy the exported folder to your FiveM server's `resources/[vehicles]/` directory.
2. Add to `server.cfg`:
   ```
   ensure police_pack
   ```
3. Start the server and test with `/spawn police1` in-game.

---

## What's Next?

- [Handling Editor](../editors/handling.md) — full field reference
- [Siren Pool](../editors/sirens.md) — how siren groups work
- [FiveM Export](../export/fivem.md) — what exactly gets generated
