# Changelog

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
