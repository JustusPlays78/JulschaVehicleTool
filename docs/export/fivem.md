# FiveM Resource Export

The export converts a resource's inline data into a ready-to-use FiveM resource folder.

---

## How to Export

1. **Select the resource** in the sidebar tree (not a vehicle — the resource node).
2. The **Resource Settings** panel opens.
3. Fill in **Name**, **Author**, **Version** if not already set.
4. Click **Choose Output Folder**, then **Export FiveM Resource**.

A progress bar tracks: stream files → meta XMLs → manifest generation.

---

## Output Structure

```
police_pack/
├── fxmanifest.lua          ← Auto-generated manifest
├── stream/
│   ├── police1.yft         ← Decrypted, ready for streaming
│   ├── police1_hi.yft
│   ├── police1.ytd
│   └── police2.yft
├── data/
│   ├── carcols.meta        ← Shared siren pool (filtered to this resource)
│   ├── police1/
│   │   ├── handling.meta
│   │   ├── vehicles.meta
│   │   └── carvariations.meta
│   └── police2/
│       ├── handling.meta
│       ├── vehicles.meta
│       └── carvariations.meta
└── vehicle_names.lua       ← Display names (optional, if enabled)
```

---

## What Gets Generated

### `fxmanifest.lua`

Generated automatically from the resource's metadata and which meta types are present:

```lua
fx_version 'cerulean'
game { 'gta5' }

author 'YourName'
description 'police_pack'
version '1.0.0'

files {
    'data/**/*.meta'
}

data_file 'HANDLING_FILE'          'data/**/handling.meta'
data_file 'CARCOLS_FILE'           'data/carcols.meta'
data_file 'VEHICLE_VARIATION_FILE' 'data/**/carvariations.meta'
data_file 'VEHICLE_METADATA_FILE'  'data/**/vehicles.meta'

client_script 'vehicle_names.lua'  -- only if enabled
```

Entries are only included if data actually exists (e.g. `CARCOLS_FILE` is omitted if no vehicle has a siren group assigned).

### `data/carcols.meta`

A single shared siren file for the entire resource. The tool automatically:

1. Looks at which siren group IDs are referenced by vehicles in this resource
2. Filters the project's siren pool to those IDs only
3. Writes one combined `carcols.meta`

This matches the correct FiveM behavior where `carcols.meta` is per-resource, not per-vehicle.

### `stream/` files

Binary files are **decrypted** from the project's DPAPI storage and written as plain GTA V binary files — exactly what FiveM needs for streaming.

!!! warning "16 MB stream file limit"
    FiveM crashes when streaming files exceed 16 MB. The export checks each file and shows a warning if any exceed this limit. You can still export, but the server will likely crash on that vehicle.

### Meta XML files (`data/<vehicleName>/`)

All meta values are **freshly serialized from the inline project data** — not copied from any imported files. This means any edits you made in the editors are reflected, even if you never touched the original `.meta` file.

---

## Validation

Before export, the tool runs a validation pass:

| Check | Severity | Effect |
|-------|----------|--------|
| No `vehicles.meta` data | Error | Export is blocked |
| Empty `ModelName` | Error | Export is blocked |
| No `handling.meta` data | Warning | Export proceeds, warning shown |
| No color variations | Warning | Export proceeds, warning shown |
| `HandlingId` ≠ `HandlingName` | Warning | Export proceeds, warning shown |

Errors are shown in the status bar; the export button remains disabled.

---

## Installing on a FiveM Server

1. Copy the exported folder to `resources/[vehicles]/` (or any category folder).
2. Add to `server.cfg`:
   ```
   ensure police_pack
   ```
3. Restart the server.
4. Test: `/spawn police1`

---

## Vehicle Names (`vehicle_names.lua`)

Enable "Include Vehicle Names" in Resource Settings to generate this file:

```lua
-- Vehicle display names
AddTextEntry('police1', 'LSPD Patrol Car')
AddTextEntry('police2', 'LSPD Interceptor')
```

This sets the name shown when players look at the vehicle. The display name comes from the `GameName` field in the Vehicle Meta editor.
