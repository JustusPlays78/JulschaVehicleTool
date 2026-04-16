# File Formats

## Project Files (internal)

| File | Format | Description |
|------|--------|-------------|
| `project.julveh` | DPAPI-encrypted JSON | All project metadata: vehicles, handling, meta, siren groups |
| `vehicles/<name>/*.yft` | DPAPI-encrypted binary | GTA V YFT model file |
| `vehicles/<name>/*.ytd` | DPAPI-encrypted binary | GTA V YTD texture dictionary |

The JSON inside `project.julveh` uses camelCase property names and omits null values.

### Project JSON structure (simplified)

```json
{
  "name": "My Pack",
  "author": "Julscha",
  "version": "1.0.0",
  "carCols": {
    "sirenSettings": [
      { "id": 1001, "name": "police_2color", "sequencerBpm": 220, "sirens": [...] }
    ]
  },
  "resources": [
    {
      "name": "police_pack",
      "vehicles": [
        {
          "name": "police1",
          "yftRelativePath": "police1/police1.yft",
          "handling": { "handlingName": "POLICE", "fMass": 1700.0, ... },
          "carVariation": { "modelName": "police1", "sirenSettings": 1001, ... },
          "vehicleMeta": { "modelName": "police1", "handlingId": "POLICE", ... }
        }
      ]
    }
  ]
}
```

---

## Exported Meta Files (XML)

These are only generated during FiveM export. They are not stored on disk between sessions.

### `handling.meta`

```xml
<CHandlingDataMgr>
  <HandlingData>
    <Item type="CHandlingData">
      <handlingName>POLICE</handlingName>
      <fMass value="1700.000000" />
      <fInitialDragCoeff value="6.000000" />
      ...
    </Item>
  </HandlingData>
</CHandlingDataMgr>
```

### `vehicles.meta`

```xml
<CVehicleModelInfo__InitDataList>
  <InitDatas>
    <Item>
      <modelName>police1</modelName>
      <txdName>police1</txdName>
      <handlingId>POLICE</handlingId>
      <gameName>POLICE</gameName>
      <vehicleClass>VC_EMERGENCY</vehicleClass>
      ...
    </Item>
  </InitDatas>
</CVehicleModelInfo__InitDataList>
```

### `carvariations.meta`

```xml
<CVehicleModelInfoVariation>
  <variationData>
    <Item>
      <modelName>police1</modelName>
      <colors>
        <Item>
          <indices content="short_array">0 0 0 0 0 0</indices>
        </Item>
      </colors>
      <sirenSettings value="1001" />
    </Item>
  </variationData>
</CVehicleModelInfoVariation>
```

### `carcols.meta`

```xml
<CVehicleModelInfoVarGlobal>
  <Sirens>
    <Item id="1001">
      <timeMultiplier value="1.000000" />
      <lightFalloffMax value="10.000000" />
      <sequencerBpm value="220" />
      <sirens>
        <Item>
          <flashiness>
            <sequencer value="2863311530" />
          </flashiness>
          ...
        </Item>
      </sirens>
    </Item>
  </Sirens>
</CVehicleModelInfoVarGlobal>
```

---

## GTA V Binary Files

| Extension | Content |
|-----------|---------|
| `.yft` | Vehicle model (drawable, skeleton, bounding box, LODs) |
| `_hi.yft` | High-detail LOD (separate file in newer games) |
| `.ytd` | Texture dictionary (DDS textures, livery slots) |
| `+hi.ytd` | High-detail texture dictionary |

These are parsed by [CodeWalker.Core](https://github.com/dexyfex/CodeWalker) internally. The tool does not modify them — they are stored as-is (encrypted) and decrypted verbatim on export.
