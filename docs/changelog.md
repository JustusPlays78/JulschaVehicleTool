# Changelog

## 1.1.0 — 2026-11-24

### Added
- **Drag & drop import** of vehicle folders and `.zip` files (also whole packs / FiveM resources) with a preview: per-vehicle checkbox, detected files and metas, warnings
- Metas are recognised by their XML content, not the file name; vehicle names come from `vehicles.meta` or the `.yft` files
- **Mod kit parts and extra texture dictionaries** (`name_*.yft`, `name_*.ytd`, `name+hi*.ytd`) are imported and exported
- **Import as…** — vehicles whose name already exists get a free name proposed; files and meta names are renamed
- **IDs used on server** for siren groups; new and imported groups never get these IDs
- **Mod kits and light settings** from `carcols.meta` are imported (only those the vehicles use) and exported again
- **vehiclelayouts.meta** is imported and exported unchanged with `VEHICLE_LAYOUTS_FILE`
- **Project-wide validation before export**: duplicate modelName/handlingName/txdName across resources, model name ≠ `.yft` file name, stream file collisions, siren IDs outside 1–255 or reserved

### Changed
- **Local encryption is optional and off by default** — new projects are stored as plain files and can be opened on any PC. Switch it on with *File > Encrypt Project on this PC*; projects from 1.0 stay encrypted until you switch it off
- **Safe saving** — the project file is written via a temp file and the previous version is kept as `project.julveh.bak`; a damaged project file is restored from the backup automatically
- Project ZIP password key derivation: 600 000 PBKDF2 iterations (was 100 000), derived once per export instead of per file; ZIPs from 1.0 still import
- CI, releases and docs build run on Forgejo; the GitHub workflows were removed
- **Rename** renames the folder, the stream files (`old.yft` → `new.yft`, `old_bumf.yft` → `new_bumf.yft`, …) and the meta names (modelName, txdName, handlingName, …) in one step and rolls back on conflicts
- **Clone** copies the stream files under the new name and renames all meta names — the clone is fully independent
- New siren groups get the lowest free ID in 1–255 (was: 1000+); colliding imported siren groups are renumbered automatically
- Saving deletes files of removed vehicles/resources; the project ZIP only contains referenced files
- Re-export replaces `stream/`, `data/` and the generated Lua files, so files of removed vehicles disappear (only in folders exported by the tool; own files are kept)
- `fxmanifest.lua`: `data_file` order layouts → handling → vehicles → carcols → carvariations; `description` uses the resource description
- Import runs in the background

### Fixed
- Importing `name_bumf.yft` with *Import Vehicle Files* overwrote the main model
- Exporting a single `carcols.meta` from the vehicle context menu did nothing (it read the old per-vehicle data)
- Meta auto-match: `police1` no longer matches `police10`; partial matches are only suggested; a vehicle is matched at most once
- Rename silently broke file paths when the target folder existed
- Vehicle names were only unique per resource, so vehicles in different resources overwrote each other's files

## 1.0.0 — 2026-11-24

### Added
- **Project-level siren pool** — siren groups are now defined once at the project level; vehicles reference them by ID
- **Vehicle Sirens tab redesigned** — dropdown to pick a siren group from the project pool, with read-only 32-bit sequencer preview
- **Selecting the project node** now opens the full siren pool editor
- **Clone vehicle** — context menu "Clone Vehicle" creates a copy with a unique name (`_copy`, `_copy_2`, …)
- **Batch handling presets** — check multiple vehicles via checkboxes, then apply a preset to all at once
- **Tree filter** — search box with live filter and × clear button in the sidebar
- **Inline rename** — F2 renames resources and vehicles directly in the tree
- **3D Viewer texture panel** — shows thumbnails of all textures in the YTD
- **Validation before export** — blocks export on critical errors (missing ModelName, missing VehicleMeta), warns on missing handling or variations
- **Password dialog** for ZIP export and import (replaces hardcoded placeholder)
- **Warning icons** on vehicles with missing files, missing VehicleMeta, or missing Handling

### Changed
- `carcols.meta` is now exported as a single `data/carcols.meta` per resource (filtered to IDs used by that resource), instead of one per vehicle subfolder
- Import of `carcols.meta` merges siren groups into the project pool instead of assigning to individual vehicles
- Manifest uses `data_file 'CARCOLS_FILE' 'data/carcols.meta'` (exact path, not glob)

### Fixed
- **Meta export preserves imported XML** — imported handling/vehicles/carvariations/carcols entries keep their original XML; only fields changed in the tool are patched. Fields the tool does not model (`lodDistances`, wheel scale, cameras, `strAdvancedFlags`, `type="NULL"` sub-handling, …) are no longer lost
- String/enum fields (`handlingName`, `strModelFlags`, `AIHandling`, `type`, `flags`, `vehicleClass`, `swankness`, …) are written as text content instead of `value` attributes, as GTA expects
- `rewards` / `cinematicPartCamera` are read and written as `<Item>` lists
- New vehicles start from a complete vanilla-style `vehicles.meta` template and get a `vehshare` txd relationship
- `txdRelationships` are imported per vehicle and exported again (and follow a renamed txdName)
- Default/suggested values fixed: `VEHICLE_TYPE_*`, `SWANKNESS_0`–`5`, `VWT_HIEND`, `VPT_*_PLATES`
- `vehicle_names.lua` uses the Game Name label as key and the new **Display Name** field as text
- Rename vehicle now correctly renames the `vehicles/<name>/` folder on disk and updates all relative paths
- XAML `Style` duplicate-set errors in MainWindow and SirenEditorView

---

## Earlier development

Initial features: project hierarchy, handling editor, car variations, vehicle meta editor, 3D viewer, FiveM export with auto-generated manifest, DPAPI + ZIP encryption, recent projects, import vehicle folder with auto-matching, handling presets.
