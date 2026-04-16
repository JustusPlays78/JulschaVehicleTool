# 3D Viewer

The 3D viewer renders YFT models in real-time using HelixToolkit with SharpDX. It loads automatically when you select a vehicle that has a `.yft` file imported.

---

## Controls

| Input | Action |
|-------|--------|
| Left mouse + drag | Orbit camera |
| Right mouse + drag | Pan |
| Scroll wheel | Zoom in/out |
| Middle mouse + drag | Pan (alternative) |

---

## LOD Switching

GTA V models contain multiple Level of Detail (LOD) meshes — the game switches between them based on distance. Use the **LOD selector** in the toolbar to preview each:

| LOD | Name | Description |
|-----|------|-------------|
| 0 | High | Full detail, closest distance |
| 1 | Medium | Slightly reduced |
| 2 | Low | Further reduction |
| 3 | Very Low | Distant view |

!!! tip
    If your model looks wrong at a distance in-game, switch to LOD 2/3 in the viewer — the lower LOD mesh may have issues.

---

## Display Options

| Option | Description |
|--------|-------------|
| **Wireframe** | Toggle wireframe overlay to inspect mesh topology |
| **Textures** | Toggle texture display |
| **Mesh visibility** | Toggle individual sub-meshes on/off via the mesh list |

---

## Texture Panel

When a `.ytd` file is imported alongside the `.yft`, the viewer shows a **texture panel** on the right side. Each texture is displayed with:

- Thumbnail preview
- Texture name
- Resolution (width × height)
- Format (DXT1, DXT5, BC7, etc.)

This is useful for checking livery slot textures — each livery slot is a separate texture in the YTD.

---

## Model Cache

Already-loaded models are cached in memory. Switching between vehicles is instant if they've been viewed before. The cache is cleared when:

- A vehicle is renamed
- New files are imported to a vehicle
- The project is closed

---

## Limitations

- The viewer renders the extracted mesh geometry only — no in-game shaders, no real-time reflections, no game lighting.
- Very large models (> 16 MB YFT) may be slow to load.
- Animated parts (doors, wheels) are shown in their default pose.
