# GalaxyEngine3D

Korai, működő projektváz egy három léptékű, procedurálisan felépülő 3D űrjátékhoz.

## Technológia

- Godot 4.7 .NET
- C# / .NET 8
- elsődleges célplatform: Windows
- GL Compatibility renderer a könnyen futtatható prototípushoz

## Jelenlegi prototípus

A `Main` jelenetben a `ViewRouter` külön példányosítja a három nézetet:

- `GalaxyView`: négy kijelölhető helyőrző csillag;
- `SystemView`: egy csillag és három kijelölhető helyőrző bolygó;
- `PlanetView`: körbeforgatható bolygó egyszerű procedurális shaderrel.

A bal egérgomb kijelöl, a jobb egérgombbal húzva a kamera kering, a görgő zoomol. A HUD gombjai nyitják meg a következő nézetet és lépnek vissza. A nézetcsere közben a `TransitionController` kamera-zoommal és fényvillanással takarja el a jelenet- és koordinátaváltást. A `ViewRouter` nézetenként elmenti és visszaállítja a kameraállapotot.

## Architektúra

```text
Main
├── ViewRouter                jelenetváltás, nézetkontextus, kameraállapotok
├── TransitionController      a váltást elfedő animáció
├── Hud                       kijelölés és navigáció
└── aktuális GameView
    ├── GalaxyView
    ├── SystemView
    └── PlanetView
```

A későbbi seedelt procedurális generálás motorfüggetlen C# rétegbe kerül. Ez a váz szándékosan nem tartalmaz végleges galaxismodellt, GPU-s csillagpipeline-t vagy nagy elemszámú generálást; a Godot-réteg most csak a jelenetek, kamera, input, UI és átmenetek felelőse.

## Indítás és ellenőrzés

1. Nyisd meg a könyvtárat a Godot 4.7 .NET editorral.
2. Várd meg a C# projekt visszaállítását és fordítását.
3. Indítsd a fő projektet (`F5`; az `F6` csak az aktuális jelenetet futtatja).

Parancssoros C# ellenőrzés:

```powershell
dotnet build .\GalaxyEngine3D.csproj
```

Headless runtime smoke teszt Godotból (kijelölés, teljes oda-vissza navigáció és kamera-visszaállítás):

```powershell
godot --headless --path . --scene res://Tests/RuntimeSmokeTest.tscn
```
