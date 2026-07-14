# GalaxyEngine3D fejlesztési útmutató

- A projekt Godot 4.7 .NET-et, C#-ot és .NET 8-at használ; elsődleges célplatformja Windows.
- A részletes terv és a réteghatárok forrása a `docs/ROADMAP.md` és a `docs/ARCHITECTURE.md`; architekturális változtatáskor ezeket is frissítsd.
- A procedurális modellek, stabil ID-k, seed-hierarchia és generátorok külön `Galaxy.Core` C# projektbe kerüljenek. Ez a projekt sem közvetlen, sem közvetett Godot-hivatkozást nem tartalmazhat.
- A determinisztikus generálás ne használjon `string.GetHashCode()`, `HashCode`, `Random.Shared` vagy bejárási sorrendtől, szálütemezéstől, platformtól vagy folyamattól függő seedképzést.
- A `Source/Views` csak nézetspecifikus prezentációt és kijelölést, a `Source/Navigation` nézetváltást és átmenetet, a `Source/UI` HUD-logikát tartalmazzon. Godot-kameraállapot nem tartozik a `Galaxy.Core` rétegbe.
- A `GalaxyView`, `SystemView` és `PlanetView` eltérő koordinátaterek; köztük stabil objektum-ID-t és magasabb szintű kontextust adj át, ne nyers világtranszformot.
- Galaxisszinten csillagonként tilos `Node3D`, `MeshInstance3D` vagy fizikai collider létrehozása. A jelenlegi node-ok helyőrzők; a skálázható irány tömbösített adat, `MultiMesh`, majd szükség szerint `RenderingDevice` és compute shader.
- Új változtatás után futtasd a `dotnet build GalaxyEngine3D.csproj` parancsot, és ellenőrizd a `.tscn`/erőforrás-hivatkozásokat. Navigációs vagy inputváltozásnál futtasd a runtime smoke tesztet is.
- Ne commitolj generált `.godot`, `.mono`, import- vagy build-kimeneteket. A Godot által generált, erőforrás-azonosságot biztosító `.uid` fájlokat viszont tartsd verziókezelés alatt.
