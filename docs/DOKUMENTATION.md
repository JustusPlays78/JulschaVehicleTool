# Julscha Vehicle Tool - Dokumentation

## Was ist das Julscha Vehicle Tool?

Das Julscha Vehicle Tool ist ein Desktop-Editor fuer GTA V Fahrzeuge. Es ermoeglicht dir, Fahrzeuge in FiveM-Resourcen zu organisieren, alle Meta-Daten inline zu bearbeiten, 3D-Modelle zu betrachten und fertige FiveM-Resourcen zu exportieren.

Alle Daten werden verschluesselt in einem selbststaendigen Projektordner gespeichert (DPAPI). Projekte koennen als passwortgeschuetzte ZIP-Datei exportiert und auf anderen Rechnern importiert werden.

---

## Schnellstart

### Starten
```
cd JulschaVehicleTool
dotnet run --project src/JulschaVehicleTool.App
```

### Tastenkuerzel
| Kuerzel | Aktion |
|---------|--------|
| Ctrl+N | Neues Projekt |
| Ctrl+O | Projekt oeffnen |
| Ctrl+S | Projekt speichern |
| Ctrl+Shift+S | Projekt speichern unter |
| Delete | Ausgewaehltes Fahrzeug loeschen |
| F2 | Umbenennen (geplant) |

---

## Projekt-System

### Hierarchie

```
Projekt (project.julveh)
├── Resource 1 (z.B. "police_pack")
│   ├── Fahrzeug 1 (z.B. "police1")
│   └── Fahrzeug 2 (z.B. "police2")
└── Resource 2 (z.B. "sports_cars")
    ├── Fahrzeug 3 (z.B. "adder")
    └── Fahrzeug 4 (z.B. "zentorno")
```

- **Projekt**: Beinhaltet mehrere FiveM-Resourcen
- **Resource**: Eine FiveM-Resource mit eigenen Einstellungen (Name, Autor, Version)
- **Fahrzeug**: Ein einzelnes Fahrzeug mit inline Meta-Daten (Handling, VehicleMeta, CarVariation, CarCols) und verschluesselten Binaerdateien (YFT/YTD)

### Projektordner auf Disk

```
MeinProjekt/
├── project.julveh          ← Verschluesselte JSON (DPAPI)
└── vehicles/               ← Verschluesselte Binaerdateien
    ├── police1/
    │   ├── police1.yft
    │   ├── police1_hi.yft
    │   ├── police1.ytd
    │   └── police1+hi.ytd
    └── adder/
        ├── adder.yft
        └── adder.ytd
```

Die `project.julveh` Datei enthaelt alle Meta-Daten (Handling, Variations, etc.) **inline als JSON** — keine externen .meta-Dateien mehr. Binaerdateien (YFT/YTD) werden mit DPAPI verschluesselt im `vehicles/`-Ordner gespeichert.

### Neues Projekt erstellen

1. **File > New Project** (oder Ctrl+N)
2. Ordner fuer das Projekt auswaehlen
3. Im TreeView erscheint das Projekt — klicke auf **+** um eine Resource hinzuzufuegen
4. Resource auswaehlen, dann **+ Add Vehicle** um ein Fahrzeug hinzuzufuegen
5. Fahrzeug auswaehlen, dann **Import Files...** um YFT/YTD-Dateien zu importieren
6. Meta-Daten in den Editoren bearbeiten (werden automatisch im Projekt gespeichert)

### Projekt oeffnen

1. **File > Open Project** (oder Ctrl+O)
2. Projektordner auswaehlen (der den `project.julveh` enthaelt)
3. Alle Resourcen und Fahrzeuge erscheinen im TreeView

### Auto-Save

Das Projekt wird automatisch gespeichert:
- Alle 5 Minuten
- Beim Wechsel zwischen Fahrzeugen/Resourcen/Tabs

Der Status wird in der Statusleiste angezeigt (z.B. "Auto-saved 14:30").

---

## TreeView Navigation

Die linke Sidebar zeigt die Projekt-Hierarchie als Baumstruktur:

```
📁 MeinProjekt
├── 📦 police_pack
│   ├── police1
│   └── police2 ⚠
└── 📦 sports_cars
    └── adder
```

- **Projekt-Knoten**: Klicken zeigt Projekt-Uebersicht
- **Resource-Knoten**: Klicken zeigt Resource-Einstellungen (Name, Autor, Version, FiveM Export)
- **Fahrzeug-Knoten**: Klicken zeigt die Editor-Tabs (3D, Handling, Variations, Sirens, VehicleMeta)
- **⚠ Warnsymbol**: Zeigt an, dass Binaerdateien fehlen

### Kontextmenue (Rechtsklick)

**Auf Fahrzeug:**
- Add Vehicle / Import Vehicle Folder...
- Delete Vehicle
- Export handling.meta / vehicles.meta / carvariations.meta / carcols.meta

---

## Fahrzeuge verwalten

### Fahrzeug hinzufuegen

1. Resource im TreeView auswaehlen
2. **+ Add Vehicle** klicken (Sidebar oder Edit-Menue)
3. Das Fahrzeug wird mit Standard-Sedan-Template erstellt (vorgefuellte Handling/Meta-Werte)

### Dateien importieren

**Einzelne Dateien:**
1. Fahrzeug im TreeView auswaehlen
2. **Import Files...** klicken
3. YFT/YTD-Dateien auswaehlen
4. Dateien werden verschluesselt in den Projektordner kopiert

**Ganzer Ordner:**
1. Resource im TreeView auswaehlen
2. **Edit > Import Vehicle Folder...**
3. Ordner auswaehlen — das Tool erkennt automatisch YFT/YTD-Dateien und erstellt Fahrzeuge

### Meta-Dateien importieren

1. Fahrzeug im TreeView auswaehlen
2. **Edit > Import Meta File...**
3. Meta-Datei(en) auswaehlen (handling.meta, vehicles.meta, carvariations.meta, carcols.meta)
4. Die Daten werden inline im Fahrzeug gespeichert

### Einzelne Meta-Dateien exportieren

Rechtsklick auf ein Fahrzeug im TreeView > **Export handling.meta** / **Export vehicles.meta** / etc.

---

## Editoren

Wenn ein Fahrzeug im TreeView ausgewaehlt ist, erscheint eine Tab-Leiste mit den verfuegbaren Editoren.

### 3D Viewer

Zeigt das Fahrzeugmodell in 3D an. Das Modell wird automatisch geladen wenn ein Fahrzeug ausgewaehlt wird.

- **LOD-Level**: Zwischen High/Medium/Low/Very Low Detailstufen wechseln
- **Wireframe**: Drahtgitter-Ansicht ein/ausschalten
- **Texturen**: Texturanzeige ein/ausschalten
- Maus-Steuerung: Orbit (linke Maustaste), Zoom (Scrollrad), Pan (mittlere Maustaste)
- **Cache**: Bereits geladene Modelle werden zwischengespeichert fuer schnelles Umschalten

### Handling Editor

Bearbeitet die Fahrphysik des Fahrzeugs (handling.meta Werte).

**Kategorien:**
- **Physical Properties**: Masse, Drag, Downforce, Schwerpunkt, Traegheit
- **Transmission**: Antrieb (FWD/RWD/AWD), Gaenge, Beschleunigung, Hoechstgeschwindigkeit
- **Braking**: Bremskraft, Bremsbias, Handbremse
- **Steering**: Lenkeinschlag
- **Traction**: Grip, Reifenhaftung, Camber
- **Suspension**: Federung, Daempfung, Hoehe, Stabilisatoren
- **Damage**: Kollisions-/Waffen-/Verformungs-/Motorschaden
- **Miscellaneous**: Sitzposition, Geldwert, Flags, KI-Handling

**Handling Presets** (Menu > Presets):
- Sports Car, Muscle Car, SUV/Truck, Motorcycle, Boat, Helicopter

**Tipps:**
- `Drive Bias Front`: 0.0 = Hinterradantrieb, 1.0 = Frontantrieb, 0.5 = Allrad
- Aenderungen werden direkt im Projekt gespeichert (kein separates "Save" noetig)

### Variations Editor

Bearbeitet Farbkombinationen und Ausstattung (carvariations.meta Werte).

- **Color Combinations**: Primaer-, Sekundaer-, Perlmutt-, Felgen-, Interieur- und Armaturenbrettfarbe
- **Light/Siren Settings ID**: Referenzen auf carcols.meta
- **Kits**: Bodykit-Varianten
- **Plate Probabilities**: Nummernschildtypen

### Siren Editor

Bearbeitet Sireneneinstellungen (carcols.meta Werte).

- **Siren Settings**: Mehrere Konfigurationen moeglich
- **Max 20 Lichter** pro Siren Setting
- **Pro Licht**: Farbe, Intensitaet, Rotation, Flashiness, Corona, Sequencer
- **Sequencer**: 32-bit Pattern als Dezimalzahl (1=an, 0=aus pro Beat)

**Wichtig:** Die Siren-ID muss mit dem Wert in carvariations.meta uebereinstimmen.

### Vehicle Meta Editor

Bearbeitet die Fahrzeugdefinition (vehicles.meta Werte).

- **Identity**: Modellname, TXD-Name, Handling-ID, Spielname, Hersteller
- **Classification**: Typ, Fahrzeugklasse, Radtyp, Nummernschild, Dashboard
- **Physics/Limits**: Masse, Passagiere
- **Audio**: Audio Hash
- **Flags**: Fahrzeug-Flags

---

## FiveM Export

Der Export erfolgt **pro Resource** (nicht mehr pro Projekt).

### Schritt-fuer-Schritt

1. Resource im TreeView auswaehlen
2. In den **Resource Settings** (erscheint rechts):
   - Resource-Infos pruefen (Name, Autor, Version)
   - **Output Folder** waehlen
   - **Export FiveM Resource** klicken
3. Fortschrittsbalken zeigt den Status

### Exportierte Ordnerstruktur

```
police_pack/
├── fxmanifest.lua          # FiveM Manifest (automatisch generiert)
├── stream/
│   ├── police1.yft         # Entschluesselte Modelle
│   ├── police1.ytd
│   ├── police2.yft
│   └── police2.ytd
├── data/
│   ├── police1/            # Meta-XMLs aus inline Daten generiert
│   │   ├── handling.meta
│   │   ├── vehicles.meta
│   │   └── carvariations.meta
│   └── police2/
│       ├── handling.meta
│       ├── vehicles.meta
│       ├── carvariations.meta
│       └── carcols.meta
└── vehicle_names.lua       # Anzeigenamen (optional)
```

- Meta-XMLs werden aus den inline Daten **frisch generiert** (nicht kopiert)
- Binaerdateien werden **entschluesselt** und unverschluesselt in `stream/` geschrieben
- Das `fxmanifest.lua` wird automatisch mit den richtigen `data_file` Eintraegen erstellt

### FiveM-Server Einrichtung

1. Exportierten Ordner nach `resources/[vehicles]/` kopieren
2. In `server.cfg`: `ensure police_pack`
3. Server starten
4. Im Spiel: `/spawn police1`

### 16 MB Limit

FiveM crasht bei Stream-Dateien ueber 16 MB. Der Export prueft automatisch und warnt.

---

## Projekt-Export / Import (ZIP)

### Export als ZIP

1. **File > Export Project ZIP...**
2. Speicherort und Passwort waehlen
3. Das Projekt wird als passwortgeschuetzte ZIP exportiert
4. Binaerdateien werden von DPAPI auf Passwort-Verschluesselung (AES-256-GCM) umgeschluesselt

### Import aus ZIP

1. **File > Import Project ZIP...**
2. ZIP-Datei und Zielordner waehlen
3. Passwort eingeben
4. Das Projekt wird entpackt und mit lokalem DPAPI neu verschluesselt

---

## Verschluesselung

| Kontext | Methode | Schluessel |
|---------|---------|------------|
| Lokal (project.julveh + vehicles/) | DPAPI | Maschinengebunden (CurrentUser) |
| ZIP Export/Import | AES-256-GCM + PBKDF2 | Benutzer-Passwort |

Die Verschluesselung schuetzt die Assets vor unbefugtem Zugriff. DPAPI ist an den Windows-Benutzer gebunden — das Projekt kann nur auf dem gleichen Rechner/Benutzer geoeffnet werden. Fuer den Transfer auf andere Rechner den ZIP-Export verwenden.

---

## Typischer Workflow

### Bestehendes Fahrzeug bearbeiten

1. Fahrzeugdateien aus OpenIV extrahieren (.yft, .ytd, alle .meta)
2. Julscha Vehicle Tool starten
3. Neues Projekt erstellen (Ordner waehlen)
4. Resource hinzufuegen
5. Fahrzeug hinzufuegen, dann **Import Files...** fuer YFT/YTD
6. **Edit > Import Meta File...** fuer die .meta Dateien
7. In den Editoren Werte anpassen
8. Resource im TreeView auswaehlen > FiveM Export
9. Auf dem FiveM-Server testen

### Mehrere Fahrzeuge als eine Resource

1. Neues Projekt erstellen
2. Resource hinzufuegen (z.B. "emergency_pack")
3. Fuer jedes Fahrzeug:
   - **+ Add Vehicle**
   - **Import Files...** oder **Import Vehicle Folder...**
   - Meta importieren oder direkt in den Editoren bearbeiten
4. Resource auswaehlen > Export

### Neues Fahrzeug von Grund auf

1. 3D-Modell in ZModeler/Blender erstellen -> .yft/.ytd
2. Neues Projekt + Resource + Fahrzeug erstellen
3. YFT/YTD importieren
4. Handling Preset waehlen (Menu > Presets > z.B. "Sports Car")
5. Handling feintunen
6. Vehicle Meta ausfuellen
7. Carvariations anlegen (Farbkombinationen)
8. Bei Einsatzfahrzeugen: Sirenen konfigurieren
9. FiveM Export

---

## Dateiformate

| Datei | Format | Beschreibung |
|-------|--------|-------------|
| `project.julveh` | Verschluesselte JSON | Projektdatei mit allen Meta-Daten inline |
| `vehicles/*.yft` | Verschluesselte GTA V Binary | Fahrzeug-3D-Modell |
| `vehicles/*.ytd` | Verschluesselte GTA V Binary | Texturen |
| `handling.meta` | XML (nur Export) | Fahrphysik |
| `vehicles.meta` | XML (nur Export) | Fahrzeugdefinition |
| `carvariations.meta` | XML (nur Export) | Farbkombinationen |
| `carcols.meta` | XML (nur Export) | Sirenen/Lichteinstellungen |

---

## Technische Details

### Tech Stack
- .NET 8 (WPF Desktop App)
- HelixToolkit.Wpf.SharpDX (3D Rendering)
- CodeWalker.Core (GTA V Binary Parser)
- CommunityToolkit.Mvvm (MVVM Framework)
- MaterialDesignThemes (Dark Theme, Grey/BlueGrey Palette)
- System.Security.Cryptography.ProtectedData (DPAPI)
- Microsoft.Extensions.DependencyInjection

### Koordinatensystem
- GTA V: Z-up (X=Ost, Y=Nord, Z=Oben)
- HelixToolkit: Y-up
- Transformation: `(X, Z, -Y)` wird automatisch angewendet

### Projektstruktur
```
JulschaVehicleTool/
├── src/
│   ├── JulschaVehicleTool.Core/    # Models, Services, Serialization
│   ├── JulschaVehicleTool.App/     # WPF UI, ViewModels, Views
│   └── JulschaVehicleTool.Tests/   # Unit Tests
└── docs/
    └── DOKUMENTATION.md
```

### Architektur

```
App (WPF)
├── Views (XAML)
├── ViewModels (MVVM mit CommunityToolkit)
└── Converters

Core
├── Models (Project, Resource, Vehicle, HandlingData, ...)
├── Services (ProjectService, MetaXmlService, FiveMExportService, EncryptionService, ...)
├── Serialization (JSON Converter: Vector3, SubHandling, ProjectJsonOptions)
└── Constants (HandlingPresets)
```

---

© 2026 Julscha
