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

Ein Projekt ist eine `.julveh`-Datei (JSON), die **Pfade** zu allen Dateien eines Fahrzeugs speichert. Das Projekt selbst enthaelt keine Fahrzeugdaten, sondern merkt sich wo deine Dateien liegen.

### Neues Projekt erstellen

1. **File > New Project** (oder Ctrl+N)
2. Lade deine Dateien in den einzelnen Editoren:
   - **Handling** Tab: Load -> handling.meta auswaehlen
   - **Variations** Tab: Load -> carvariations.meta auswaehlen
   - **Sirens** Tab: Load -> carcols.meta auswaehlen
   - **Vehicle** Tab: Load -> vehicles.meta auswaehlen
   - **3D Viewer** Tab: Load -> .yft Modelldatei auswaehlen
3. **File > Save Project** (Ctrl+S) -> Speichert alle Pfade als .julveh

### Projekt oeffnen

1. **File > Open Project** (oder Ctrl+O)
2. Eine `.julveh`-Datei auswaehlen
3. Alle verknuepften Dateien werden automatisch in die Editoren geladen
4. Der Export-Tab wird automatisch mit allen Pfaden befuellt

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

Der Export-Tab erstellt eine fertige FiveM-Resource aus allen Dateien.

### Schritt-fuer-Schritt

1. **Export** Tab in der Sidebar auswaehlen
2. **Resource Information** ausfuellen:
   - Resource Name (wird zum Ordnernamen)
   - Author, Version
3. **Stream Files** angeben:
   - Vehicle Model (.yft) - Pflicht
   - High-Detail Model (_hi.yft) - Optional
   - Textures (.ytd) - Empfohlen
   - High-Detail Textures (+hi.ytd) - Optional
4. **Meta Files** angeben:
   - handling.meta, vehicles.meta, carvariations.meta, carcols.meta, vehiclelayouts.meta
5. **Vehicle Names** (optional):
   - Anzeigename fuer das Fahrzeug im Spiel (via AddTextEntry)
6. **Output Folder** waehlen
7. **Export Resource** klicken

### Exportierte Ordnerstruktur

```
meine_resource/
├── fxmanifest.lua          # FiveM Manifest (automatisch generiert)
├── stream/
│   ├── fahrzeug.yft        # 3D-Modell
│   ├── fahrzeug_hi.yft     # High-Detail (falls vorhanden)
│   ├── fahrzeug.ytd        # Texturen
│   └── fahrzeug+hi.ytd     # HD-Texturen (optional)
├── data/
│   ├── handling.meta       # Fahrphysik
│   ├── vehicles.meta       # Fahrzeugdefinition
│   ├── carvariations.meta  # Farben & Ausstattung
│   ├── carcols.meta        # Sirenen & Lichter
│   └── vehiclelayouts.meta # Sitzplaetze
└── vehicle_names.lua       # Anzeigename (optional)
```

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
3. In jedem Editor die passende .meta Datei laden
4. Werte anpassen (z.B. Handling tunen, Farben aendern)
5. Meta-Dateien speichern (Save in jedem Editor)
6. Im Export-Tab alle Dateien zusammenstellen
7. Als FiveM-Resource exportieren
8. Auf dem FiveM-Server testen

### Neues Fahrzeug von Grund auf

1. 3D-Modell in ZModeler/Blender erstellen -> .yft/.ytd
2. Julscha Vehicle Tool starten
3. Handling Preset waehlen (z.B. "Sports Car" unter Menu > Presets)
4. Handling-Werte feintunen
5. Vehicle Meta ausfuellen (Name, Klasse, Typ etc.)
6. Carvariations anlegen (Farbkombinationen)
7. Bei Einsatzfahrzeugen: Sirenen konfigurieren
8. Projekt speichern (.julveh)
9. Als FiveM-Resource exportieren

---

## Dateiformate

| Datei | Format | Beschreibung |
|-------|--------|-------------|
| `.julveh` | JSON | Projektdatei (speichert Pfade zu allen Dateien) |
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
