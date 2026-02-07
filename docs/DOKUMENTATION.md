# Julscha Vehicle Tool - Dokumentation

## Was ist das Julscha Vehicle Tool?

Das Julscha Vehicle Tool ist ein Desktop-Editor fuer GTA V Fahrzeuge. Es ermoeglicht dir, alle relevanten Fahrzeug-Dateien an einem Ort zu bearbeiten, das 3D-Modell zu betrachten und eine fertige FiveM-Resource zu exportieren.

---

## Schnellstart

### Starten
```
cd JulschaVehicleTool
dotnet run --project src/JulschaVehicleTool.App
```
Oder die EXE direkt ausfuehren:
```
src\JulschaVehicleTool.App\bin\Debug\net8.0-windows10.0.19041\JulschaVehicleTool.App.exe
```

### Tastenkuerzel
| Kuerzel | Aktion |
|---------|--------|
| Ctrl+N | Neues Projekt |
| Ctrl+O | Projekt oeffnen |
| Ctrl+S | Projekt speichern |
| Ctrl+Shift+S | Projekt speichern unter |

---

## Projekt-System (.julveh)

Ein Projekt ist eine `.julveh`-Datei (JSON), die **mehrere Fahrzeuge** und deren Dateipfade speichert. Das Projekt selbst enthaelt keine Fahrzeugdaten, sondern merkt sich wo deine Dateien liegen. Beim Export werden alle Fahrzeuge gemeinsam als eine FiveM-Resource exportiert.

### Neues Projekt erstellen

1. **File > New Project** (oder Ctrl+N)
2. In der Sidebar unter **VEHICLES** auf **+** klicken um ein Fahrzeug hinzuzufuegen
3. **Import Files...** klicken um alle Dateien (.yft, .ytd, .meta) fuer das Fahrzeug zu importieren
   - Die Dateien werden automatisch anhand des Dateinamens zugeordnet (z.B. handling.meta -> Handling Editor)
   - Der Fahrzeugname wird automatisch vom YFT-Dateinamen uebernommen
4. Weitere Fahrzeuge mit **+** hinzufuegen und Dateien importieren
5. Fahrzeug in der Liste anklicken -> Editoren zeigen die Daten dieses Fahrzeugs
6. **File > Save Project** (Ctrl+S) -> Speichert alle Fahrzeuge und Pfade als .julveh

### Fahrzeuge verwalten

- **+ Button**: Neues Fahrzeug zum Projekt hinzufuegen
- **- Button**: Ausgewaehltes Fahrzeug aus dem Projekt entfernen
- **Import Files...**: Dateien (.yft, .ytd, .meta) fuer das ausgewaehlte Fahrzeug importieren
- **Fahrzeug anklicken**: Wechselt die Editoren auf dieses Fahrzeug

Die Dateizuordnung beim Import erfolgt automatisch:
| Dateiname enthaelt | Zuordnung |
|---------------------|-----------|
| `*.yft` (ohne `_hi`) | Fahrzeugmodell |
| `*_hi.yft` | High-Detail Modell |
| `*.ytd` (ohne `+hi`) | Texturen |
| `*+hi.ytd` | HD-Texturen |
| `handling*.meta` | Handling Editor |
| `vehicles*.meta` | Vehicle Meta Editor |
| `carvariations*.meta` | Variations Editor |
| `carcols*.meta` | Siren Editor |
| `vehiclelayouts*.meta` | Vehicle Layouts |

### Projekt oeffnen

1. **File > Open Project** (oder Ctrl+O)
2. Eine `.julveh`-Datei auswaehlen
3. Alle Fahrzeuge erscheinen in der Sidebar
4. Das erste Fahrzeug wird automatisch in die Editoren geladen
5. Der Export-Tab zeigt alle Fahrzeuge mit ihrem Dateistatus

### Kuerzlich geoeffnete Projekte

Unter **File > Recent Projects** findest du die letzten 10 geoeffneten Projekte.

---

## Editoren

### 3D Viewer

Zeigt das Fahrzeugmodell in 3D an.

- **Load**: .yft Datei laden (aus OpenIV oder einem entpackten GTA V Verzeichnis)
- **LOD-Level**: Zwischen High/Medium/Low/Very Low Detailstufen wechseln
- **Wireframe**: Drahtgitter-Ansicht ein/ausschalten
- **Texturen**: Texturanzeige ein/ausschalten
- Maus-Steuerung: Orbit (linke Maustaste), Zoom (Scrollrad), Pan (mittlere Maustaste)

**Unterstuetzte Formate**: `.yft` (Fahrzeugmodell), `.ytd` (Texturen)

### Handling Editor

Bearbeitet die `handling.meta` - die Fahrphysik des Fahrzeugs.

**Kategorien:**
- **Physical Properties**: Masse, Drag, Downforce, Schwerpunkt, Traegheit
- **Transmission**: Antrieb (FWD/RWD/AWD), Gaenge, Beschleunigung, Hoechstgeschwindigkeit
- **Braking**: Bremskraft, Bremsbias, Handbremse
- **Steering**: Lenkeinschlag
- **Traction**: Grip, Reifenhaftung, Camber
- **Suspension**: Federung, Daempfung, Hoehe, Stabilisatoren
- **Damage**: Kollisions-/Waffen-/Verformungs-/Motorschaden
- **Miscellaneous**: Sitzposition, Geldwert, Flags, KI-Handling

**Handling Presets**: Unter Menu > Presets findest du vorgefertigte Handling-Konfigurationen:
- Sports Car, Muscle Car, SUV/Truck, Motorcycle, Boat, Helicopter

**Tipps:**
- `Drive Bias Front`: 0.0 = Hinterradantrieb, 1.0 = Frontantrieb, 0.5 = Allrad
- Float-Werte werden im GTA V Format mit 6 Dezimalstellen gespeichert (z.B. `1500.000000`)
- Die Slider haben Min/Max-Bereiche, du kannst aber auch direkt Werte ins Textfeld eingeben

### Variations Editor

Bearbeitet die `carvariations.meta` - Farbkombinationen und Ausstattung.

- **Color Combinations**: Primaer-, Sekundaer-, Perlmutt-, Felgen-, Interieur- und Armaturenbrettfarbe (als Farbindex 0-160)
- **Light Settings ID**: Referenz auf die Lichteinstellungen in carcols.meta
- **Siren Settings ID**: Referenz auf die Sireneneinstellungen in carcols.meta
- **Kits**: Fahrzeug-Kits (Bodykit-Varianten)
- **Plate Probabilities**: Wahrscheinlichkeiten fuer Nummernschildtypen

### Siren Editor

Bearbeitet die Sireneneinstellungen in `carcols.meta`.

- **Siren Settings**: Mehrere Sirenen-Konfigurationen pro Datei moeglich
- **Max 20 Lichter** pro Siren Setting
- **Pro Licht konfigurierbar**: Farbe, Intensitaet, Rotation, Flashiness, Corona, Sequencer
- **Sequencer**: 32-bit Binaermuster als Dezimalzahl (1=Licht an, 0=Licht aus pro BPM-Beat)
- **BPM**: Beats per Minute fuer die Blinkgeschwindigkeit

**Wichtig:**
- Die `id` muss mit dem `sirenSettings`-Wert in carvariations.meta uebereinstimmen
- Reservierte IDs: 0, 255, 65535 **NICHT** verwenden!

### Vehicle Meta Editor

Bearbeitet die `vehicles.meta` - die Fahrzeugdefinition.

**Bereiche:**
- **Identity**: Modellname, TXD-Name, Handling-ID, Spielname, Hersteller
- **Classification**: Typ, Fahrzeugklasse, Radtyp, Nummernschild, Dashboard, Layout
- **Physics/Limits**: Masse, Passagiere, Untertauchtiefe
- **Audio**: Audio Hash
- **Flags**: Fahrzeug-Flags
- **Extras**: Belohnungen, Kameraeinstellungen

---

## FiveM Export

Der Export-Tab erstellt eine fertige FiveM-Resource aus **allen Fahrzeugen im Projekt**.

### Schritt-fuer-Schritt

1. Fahrzeuge zum Projekt hinzufuegen und Dateien importieren (siehe oben)
2. **Export** Tab in der Sidebar auswaehlen
3. **Resource Information** ausfuellen:
   - Resource Name (wird zum Ordnernamen)
   - Author, Version
4. Im Bereich **Vehicles in Project** pruefen ob alle Dateien vorhanden sind
   - Vorhandene Dateien werden normal angezeigt, fehlende sind ausgegraut
5. **Vehicle Names** aktivieren (optional) - generiert automatisch AddTextEntry fuer jedes Fahrzeug
6. **Output Folder** waehlen
7. **Export Resource** klicken

### Exportierte Ordnerstruktur (Multi-Vehicle)

```
meine_resource/
├── fxmanifest.lua          # FiveM Manifest (automatisch generiert)
├── stream/
│   ├── adder.yft           # Fahrzeug 1 Modell
│   ├── adder.ytd           # Fahrzeug 1 Texturen
│   ├── police.yft          # Fahrzeug 2 Modell
│   └── police.ytd          # Fahrzeug 2 Texturen
├── data/
│   ├── adder/              # Fahrzeug 1 Meta-Dateien
│   │   ├── handling.meta
│   │   ├── vehicles.meta
│   │   └── carvariations.meta
│   └── police/             # Fahrzeug 2 Meta-Dateien
│       ├── handling.meta
│       ├── vehicles.meta
│       ├── carvariations.meta
│       └── carcols.meta
└── vehicle_names.lua       # Anzeigenamen (optional)
```

Jedes Fahrzeug bekommt einen eigenen Unterordner unter `data/`. Stream-Dateien (.yft, .ytd) liegen alle gemeinsam in `stream/`. Das `fxmanifest.lua` verwendet Glob-Patterns (`data/**/*.meta`) um alle Unterordner automatisch zu erfassen.

### FiveM-Server Einrichtung

1. Exportierten Ordner nach `resources/[vehicles]/` auf deinem Server kopieren
2. In `server.cfg` eintragen: `ensure meine_resource`
3. Server starten
4. Im Spiel spawnen: `/spawn fahrzeugname`

### 16 MB Limit

FiveM crasht bei Stream-Dateien ueber 16 MB. Der Export-Service prueft automatisch und zeigt eine Warnung an. Bei zu grossen Dateien:
- Texturen in der YTD komprimieren (DXT1 statt BC7)
- YTD in mehrere Dateien aufteilen
- Texturaufloesung reduzieren

---

## Typischer Workflow

### Bestehendes Fahrzeug bearbeiten

1. Fahrzeugdateien aus OpenIV extrahieren (.yft, .ytd, alle .meta)
2. Julscha Vehicle Tool starten
3. **+** klicken um ein Fahrzeug hinzuzufuegen
4. **Import Files...** klicken und alle extrahierten Dateien auswaehlen
5. In den Editoren Werte anpassen (z.B. Handling tunen, Farben aendern)
6. Meta-Dateien speichern (Save in jedem Editor)
7. Weitere Fahrzeuge hinzufuegen (optional)
8. Im Export-Tab als FiveM-Resource exportieren
9. Auf dem FiveM-Server testen

### Mehrere Fahrzeuge als eine Resource

1. Julscha Vehicle Tool starten -> Neues Projekt
2. Fuer jedes Fahrzeug:
   - **+** klicken
   - **Import Files...** -> Alle Dateien des Fahrzeugs auswaehlen
   - In den Editoren anpassen
3. Projekt speichern (.julveh)
4. Export-Tab -> Resource-Name vergeben -> Export
5. Alle Fahrzeuge werden in einer gemeinsamen FiveM-Resource gebundelt

### Neues Fahrzeug von Grund auf

1. 3D-Modell in ZModeler/Blender erstellen -> .yft/.ytd
2. Julscha Vehicle Tool starten
3. **+** klicken um Fahrzeug hinzuzufuegen
4. **Import Files...** -> .yft und .ytd auswaehlen
5. Handling Preset waehlen (z.B. "Sports Car" unter Menu > Presets)
6. Handling-Werte feintunen
7. Vehicle Meta ausfuellen (Name, Klasse, Typ etc.)
8. Carvariations anlegen (Farbkombinationen)
9. Bei Einsatzfahrzeugen: Sirenen konfigurieren
10. Projekt speichern (.julveh)
11. Als FiveM-Resource exportieren

---

## Dateiformate

| Datei | Format | Beschreibung |
|-------|--------|-------------|
| `.julveh` | JSON | Projektdatei (speichert mehrere Fahrzeuge mit allen Dateipfaden) |
| `.yft` | GTA V Binary | Fahrzeug-3D-Modell (Fragment Type) |
| `.ytd` | GTA V Binary | Texturen-Woerterbuch (Texture Dictionary) |
| `handling.meta` | XML | Fahrphysik (~50+ Parameter) |
| `vehicles.meta` | XML | Fahrzeugdefinition (~20+ Felder) |
| `carvariations.meta` | XML | Farbkombinationen, Kits, Liveries |
| `carcols.meta` | XML | Sirenen, Lichteinstellungen |
| `vehiclelayouts.meta` | XML | Sitzplatz-Layout |

### GTA V XML Format

Meta-Dateien verwenden ein spezielles XML-Format mit `value`-Attributen:
```xml
<fMass value="1500.000000" />
<handlingName>ADDER</handlingName>
<vecCentreOfMassOffset x="0.000000" y="0.000000" z="-0.120000" />
```

---

## Technische Details

### Tech Stack
- .NET 8 (WPF Desktop App)
- HelixToolkit.Wpf.SharpDX 3.1.2 (3D Rendering)
- CodeWalker.Core 1.0.3 (GTA V Binary Parser fuer YFT/YTD)
- CommunityToolkit.Mvvm 8.3.2 (MVVM Framework)
- MaterialDesignThemes 5.3.0 (UI Theme)
- System.Xml.Linq (Meta-XML Parsing)

### Koordinatensystem
- GTA V: Z-up (X=Ost, Y=Nord, Z=Oben)
- HelixToolkit: Y-up
- Transformation: `(X, Z, -Y)` wird automatisch angewendet

### Projektstruktur
```
JulschaVehicleTool/
├── src/
│   ├── JulschaVehicleTool.Core/    # Models, Services, Parsing
│   ├── JulschaVehicleTool.App/     # WPF UI, ViewModels, Views
│   └── JulschaVehicleTool.Tests/   # Unit Tests
└── docs/
    └── DOKUMENTATION.md
```
