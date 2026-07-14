# GalaxyEngine3D

Korai, működő projektváz egy három léptékű, procedurálisan felépülő 3D űrjátékhoz.

## Technológia

- Godot 4.7 .NET
- C# / .NET 8
- elsődleges célplatform: Windows
- GL Compatibility renderer a könnyen futtatható prototípushoz

## Jelenlegi prototípus

A `Main` jelenetben a `ViewRouter` külön példányosítja a három nézetet:

- `GalaxyView`: seedből determinisztikusan generált, MultiMesh-ben megjelenített 50 000 csillagos spirálgalaxis;
- `SystemView`: egy csillag és három kijelölhető helyőrző bolygó;
- `PlanetView`: körbeforgatható bolygó egyszerű procedurális shaderrel.

A bal egérgomb kijelöl, a jobb egérgombbal húzva a kamera kering, a görgő zoomol. A galaxisszintű kijelölés analitikus sugártesztet használ, csillagonkénti node vagy collider nélkül. A HUD gombjai nyitják meg a következő nézetet és lépnek vissza. A nézetcsere közben a `TransitionController` kamera-zoommal és fényvillanással takarja el a jelenet- és koordinátaváltást. A `ViewRouter` nézetenként elmenti és visszaállítja a kameraállapotot. A fejlesztői panel a seedet, csillagszámot, spirálkarok számát és az FPS-t mutatja.

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

Galaxy.Core                   Godot-független seedek, stabil ID-k és generálás
```

A `Galaxy.Core` külön .NET 8 projekt, és nem hivatkozik Godotra. A verziózott spirálgenerátor 64 bites `GalaxySeed`, rögzített 64 bites keverés és stabil csillagindex alapján állítja elő a katalógust. A Godot-réteg ezt renderadattá alakítja, de nem módosítja a procedurális eredményt. Compute shader, köd, bolygógenerálás és végleges grafika még nincs a prototípusban.

## Indítás és ellenőrzés

1. Nyisd meg a könyvtárat a Godot 4.7 .NET editorral.
2. Várd meg a C# projekt visszaállítását és fordítását.
3. Indítsd a fő projektet (`F5`; az `F6` csak az aktuális jelenetet futtatja).

Parancssoros C# ellenőrzés:

```powershell
dotnet build .\GalaxyEngine3D.csproj
```

Motorfüggetlen generátortesztek:

```powershell
dotnet test .\Galaxy.Core.Tests\Galaxy.Core.Tests.csproj
```

Headless runtime smoke teszt Godotból (kijelölés, teljes oda-vissza navigáció és kamera-visszaállítás):

```powershell
godot --headless --path . --scene res://Tests/RuntimeSmokeTest.tscn
```
