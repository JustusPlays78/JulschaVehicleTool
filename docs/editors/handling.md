# Handling Editor

The handling editor lets you edit every field of GTA V's `handling.meta` format directly inside the project. No external XML editing required.

---

## Overview

Select a vehicle in the tree, then click the **Handling** tab. Fields are organized into collapsible categories. Changes are saved automatically to the project.

### Handling Presets

Don't start from scratch. Use **Presets menu** (menu bar) to apply a base configuration:

| Preset | Best for |
|--------|----------|
| Sports Car | Fast road cars, supercars |
| Muscle Car | Heavy, powerful, rear-wheel drive |
| SUV / Truck | High ride height, 4WD |
| Motorcycle | Two-wheeled vehicles |
| Boat | Water vehicles |
| Helicopter | Rotary aircraft |

You can also apply a preset to **multiple vehicles at once** — check the checkboxes next to vehicle names in the sidebar, then pick a preset.

---

## Field Reference

### Physical Properties

| Field | Description | Typical range |
|-------|-------------|---------------|
| `fMass` | Vehicle mass in kg. Affects collision physics and braking distance. | 800–4000 |
| `fInitialDragCoeff` | Air drag coefficient. Higher = slower top speed. | 4–20 |
| `fDownforceModifier` | Downforce at speed. Higher = more grip at high speed. | 0–100 |
| `vecCentreOfMassOffset` | Centre of mass offset (X=left/right, Y=front/back, Z=up/down). Affects handling balance. | −0.5 to 0.5 |
| `vecInertiaMultiplier` | Rotational inertia multiplier per axis. Higher = harder to spin. | 1.0–2.5 |

### Transmission

| Field | Description | Notes |
|-------|-------------|-------|
| `nDriveType` | Drive layout | `F` = front, `R` = rear, `4` = all-wheel |
| `fDriveBiasFront` | Torque split front/rear | 0.0 = full rear, 1.0 = full front, 0.45 = AWD-ish |
| `nInitialDriveGears` | Number of gears | 4–7 |
| `fInitialDriveForce` | Engine force multiplier | 0.2–0.6 |
| `fDriveInertia` | How quickly engine revs build | 0.5–2.0 |
| `fClutchChangeRateScaleUpShift` | Clutch speed on upshift | 0.5–3.0 |
| `fClutchChangeRateScaleDownShift` | Clutch speed on downshift | 0.5–3.0 |
| `fInitialDriveMaxFlatVel` | Top speed (km/h) at max gear. Not a hard cap but a shift-point target. | 100–400 |

### Braking

| Field | Description |
|-------|-------------|
| `fBrakeForce` | Overall brake strength. 0.5 = weak, 2.0 = very strong. |
| `fBrakeBiasFront` | Brake distribution front/rear. 0.5 = balanced, 1.0 = front-heavy. |
| `fHandBrakeForce` | Handbrake force. High = drifty. |

### Steering

| Field | Description |
|-------|-------------|
| `fSteeringLock` | Maximum steering angle in degrees. Higher = tighter turns but more twitchy. Typical: 28–45. |
| `fSteeringLockRatio` | Ratio of inner to outer wheel angle. Usually 1.0. |

### Traction

| Field | Description |
|-------|-------------|
| `fTractionCurveMax` | Peak grip. Higher = more traction before sliding. |
| `fTractionCurveMin` | Grip while sliding. Closer to Max = less drift, further = more oversteer. |
| `fTractionCurveLateral` | Lateral grip curve shape. |
| `fTractionSpringDeltaMax` | How much suspension travel affects grip. |
| `fLowSpeedTractionLossMult` | Wheelspin multiplier at low speed. 0 = no spin, 1+ = burnout-prone. |
| `fCamberStiffnesss` | Camber effect on grip. Usually 0. |
| `fTractionBiasFront` | Grip distribution front/rear. 0.5 = balanced. |
| `fTractionLossMult` | Traction loss on non-road surfaces. |

### Suspension

| Field | Description |
|-------|-------------|
| `fSuspensionForce` | Spring stiffness. Low = soft/bouncy, high = stiff. |
| `fSuspensionCompDamp` | Compression damping (absorbs bumps). |
| `fSuspensionReboundDamp` | Rebound damping (controls bounce-back). |
| `fSuspensionUpperLimit` | Maximum suspension compression (upward travel). |
| `fSuspensionLowerLimit` | Maximum suspension extension (downward travel, negative). |
| `fSuspensionRaise` | Static ride height offset. |
| `fSuspensionBiasFront` | Suspension stiffness distribution front/rear. |
| `fAntiRollBarForce` | Anti-roll bar stiffness. Higher = less body roll. |
| `fAntiRollBarBiasFront` | Anti-roll bar distribution front/rear. |
| `fRollCentreHeightFront` | Roll centre height front (affects roll behaviour). |
| `fRollCentreHeightRear` | Roll centre height rear. |

### Damage

| Field | Description |
|-------|-------------|
| `fCollisionDamageMult` | Multiplier for damage from collisions. |
| `fWeaponDamageMult` | Multiplier for bullet/explosion damage. |
| `fDeformationDamageMult` | How much the body deforms on impact. |
| `fEngineDamageMult` | Engine damage from collisions. |

### Miscellaneous

| Field | Description |
|-------|-------------|
| `fSeatOffsetDistX/Y/Z` | Driver seat position offset |
| `fMonetaryValue` | In-game price (affects insurance etc.) |
| `handlingFlags` | Bitmask of special handling flags |
| `modelFlags` | Bitmask of model flags |
| `AIHandling` | AI driving style: `AVERAGE`, `SPORTS_CAR`, `TRUCK`, `CRAP` |

---

## Tips

!!! tip "Drive Bias — Quick Reference"
    - `0.0` = pure rear-wheel drive (RWD)
    - `0.45` = all-wheel drive, rear bias (like AWD sports cars)
    - `0.5` = true 50/50 AWD
    - `1.0` = pure front-wheel drive (FWD)

!!! tip "Getting the right top speed"
    `fInitialDriveMaxFlatVel` is in km/h but it's a shift-point target, not a hard cap. The actual top speed also depends on `fInitialDriveForce` and `fInitialDragCoeff`. Increase drag to reduce top speed without touching power.

!!! warning "HandlingId must match"
    The `HandlingId` field in [Vehicle Meta](vehicle-meta.md) must exactly match the `HandlingName` in this editor. A mismatch shows a warning icon on the vehicle in the tree.
