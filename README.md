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

A bal egérgomb kijelöl egy csillagot, és azonnal sima, megszakítható fókuszanimációt indít rá; ettől a `SystemView` még nem nyílik meg. A jobb egérgombbal húzva a kamera az aktuális fókuszpont körül kering, a középső egérgomb húzása pedig a kamera képernyősíkjában pásztázza a fókuszt. A görgő exponenciálisan zoomol. A `W`/`S` a kamera előreirányának galaxis síkjára vetített irányában mozgat, az `A`/`D` ehhez képest balra-jobbra; a közel függőleges kameranézet stabil utolsó irányt használ. A mozgás a zoomtávolsággal skálázódik, gyorsítása és lassítása simított. A `Q`/`E` a galaxis síkjára merőlegesen mozgat, `F` újrafókuszál a kijelölt objektumra, `Home` pedig a nézet középpontjára. Bármely felhasználói kameramozgás biztonságosan megszakítja az aktív fókuszanimációt.

A HUD külön aktiválógombja nyitja meg a következő nézetet, a vissza gomb pedig visszalép. Előrelépés csak érvényes kijelöléssel indul: a kamera a kijelölt csillag vagy bolygó tényleges pozíciójára közelít, majd a képet kitöltő objektum és fényvillanás alatt történik a jelenet- és koordinátaváltás. A `ViewRouter` nézetenként a fókuszponttal együtt menti és állítja vissza a kameraállapotot. A galaxisszintű kijelölés analitikus sugártesztet használ, csillagonkénti node vagy collider nélkül.

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

A `Galaxy.Core` külön .NET 8 projekt, és nem hivatkozik Godotra. A 3-as verziójú spirálgenerátor 64 bites `GalaxySeed`, rögzített 64 bites keverés és stabil csillagindex alapján állítja elő a katalógust. Az exponenciális korong spirálkarjai a középpontig futnak, ahol folyamatosan sűrűbbé, szélesebbé és vastagabbá válva külön gömbszerű dudor nélkül olvadnak össze belső lemezzé. A külső perem `EdgeFadeStart` után seedelt, szög- és karfüggő valószínűséggel halványul el. A csillagok vizuális sugarát is figyelembe vevő minimális távolságot determinisztikus térbeli hash-rács ellenőrzi, a centrumban külön faktorral enyhíthető. A Godot-réteg ezt renderadattá alakítja, de nem módosítja a procedurális eredményt. Compute shader, köd, bolygógenerálás és végleges grafika még nincs a prototípusban.

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
