# GalaxyEngine3D fejlesztési útmutató

- A projekt Godot 4.7 .NET-et, C#-ot és .NET 8-at használ; elsődleges célplatformja Windows.
- A motorfüggetlen procedurális modellek és algoritmusok később külön, Godot API-t nem hivatkozó C# rétegbe kerüljenek.
- A `Source/Views` csak nézetspecifikus prezentációt és kijelölést, a `Source/Navigation` nézetváltást és átmenetet, a `Source/UI` HUD-logikát tartalmazzon.
- A `GalaxyView`, `SystemView` és `PlanetView` eltérő koordinátaterek; ne próbáld őket egyetlen folyamatos világkoordináta-rendszerbe kényszeríteni.
- Új változtatás után futtasd a `dotnet build GalaxyEngine3D.csproj` parancsot, és ellenőrizd a `.tscn`/erőforrás-hivatkozásokat.
- Ne commitolj generált `.godot`, `.mono`, import- vagy build-kimeneteket, és ne vezess be milliós csillagrenderelést a prototípus-ágba.
