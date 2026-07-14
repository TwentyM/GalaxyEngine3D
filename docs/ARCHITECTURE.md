# GalaxyEngine3D architektúra

## Cél és alapelvek

A GalaxyEngine3D három különböző léptékű prezentációt kezel: galaxist, csillagrendszert és bolygót. Ezek nem egyetlen, folytonos Godot-világ részei. A játék a determinisztikus, motorfüggetlen világmodellt igény szerint lekérdezi, majd az aktuális nézethez megfelelő lokális koordinátákra és renderadatokra alakítja.

Az architektúra alapelvei:

- azonos seed és generátorverzió azonos világot eredményez;
- a procedurális modell nem hivatkozik Godot API-ra;
- a részletes adatok csak akkor készülnek el, amikor szükségesek;
- a három nézet saját koordinátarendszert és kamerát használ;
- a nagy elemszámú objektumok tömbökben és GPU-bufferekben élnek, nem node-fákban;
- a renderelési technika cserélhető anélkül, hogy a generátor megváltozna.

## Rétegek

| Réteg | Felelősség | Függhet Godottól? |
|---|---|---:|
| `Galaxy.Core` | Seedek, stabil ID-k, motorfüggetlen modellek, generátorok és lekérdezések | Nem |
| alkalmazás/adaptáció | Core-adatok lekérése, nézetkontextus, renderadatokká alakítás, erőforrás-élettartam | Igen |
| nézetek | Kamera, input, kijelölés, vizuális reprezentáció | Igen |
| navigáció és UI | Nézetváltás, átmenet, HUD és játékosi műveletek | Igen |
| render backend | `MultiMesh`, `RenderingDevice`, GPU-bufferek és shaderek | Igen |

Az engedélyezett függési irány:

```text
Godot UI / Navigation / Views / Rendering
                    |
                    v
                Galaxy.Core
```

A `Galaxy.Core` sem közvetlenül, sem közvetve nem hivatkozhat a Godot assemblykre.

## `Galaxy.Core`: motorfüggetlen procedurális réteg

A `Galaxy.Core` külön .NET 8 C# projekt/assembly, és nincs NuGet- vagy Godot-függősége. Jelenlegi feladatai:

- 64 bites `GalaxySeed`, stabil `StarId` és az 1-es generátorverzió;
- paraméterezett spirálgalaxis-generálás;
- determinisztikus 64 bites hash/keverés és indexenkénti véletlen minták;
- teljes csillagkatalógus és sorrendfüggetlen index szerinti lekérdezés;
- saját `GalaxyVector3` értéktípus, csillaghőmérséklet és fényesség;
- Godot nélkül futó unit- és reprodukálhatósági tesztek.

A régió-, rendszer-, bolygó-, hold- és gyűrűmodellek későbbi mérföldkövekben bővítik ezt a réteget.

Nem kerülhet ebbe a rétegbe `Node`, `Resource`, `Godot.Vector3`, `Color`, shader, `RenderingDevice`, input vagy jelenetkezelés. A matematikai típusok saját kis értéktípusok vagy szabványos .NET típusok legyenek, egyértelmű mértékegységekkel.

A `Source/Core` továbbra is a Godot alkalmazás közös nézetkontextusát és kameraállapotát tartalmazza; nem része a `Galaxy.Core` assemblynek. A két névtér és projekt külön marad, így Godot-típus nem kerülhet a generátor függőségi gráfjába.

## Determinisztikus seed-hierarchia

A generálás fa alakú, névterezett seed-hierarchiát használ. Egy gyermek seed kizárólag a szülő stabil seedjéből, a gyermek szerepét jelölő konstansból és stabil indexéből vagy koordinátájából származhat.

```text
WorldSeed + GeneratorVersion
└── GalaxySeed
    ├── RegionSeed(region coordinate)
    │   └── StarSeed(stable star index)
    │       └── SystemSeed
    │           ├── PlanetSeed(orbit index)
    │           │   ├── MoonSeed(moon index)
    │           │   └── RingSeed
    │           └── AsteroidBeltSeed(belt index)
    └── NebulaSeed(region/layer index)
```

Követelmények:

- fixen definiált, platformfüggetlen hash- vagy keverőfüggvény;
- a bemeneti mezők egyértelmű sorrendje és bináris kódolása;
- stabil indexek, amelyek nem függnek kollekcióbejárási sorrendtől;
- a generátor algoritmusának verziózása;
- részlekérdezéskor ugyanaz az eredmény, mint teljes generáláskor;
- párhuzamos futtatás ne változtassa meg az eredményt.

Az első generátorverzió a `GalaxySeed`, egy rögzített szerepkonstans, a generátorverzió és a stabil csillagindex egyértelműen rendezett 64 bites értékeiből SplitMix64-alapú avalanche keveréssel képez csillagseedet és `StarId`-t. Minden véletlen minta a csillagseed és egy rögzített streamindex tiszta függvénye; nincs megosztott PRNG-állapot. Emiatt az index szerinti részlekérdezés és a teljes katalógus ugyanazt a rekordot adja.

Nem használható determinisztikus azonosítóhoz vagy seedhez `string.GetHashCode()`, `HashCode`, `Random.Shared`, folyamatfüggő hash vagy nem rögzített globális véletlengenerátor. A jelenlegi `PlanetView` névből számított shader-eltolása csak vizuális helyőrző, és a determinisztikus bolygóparaméterek bevezetésekor lecserélendő.

## A nézetek felelőssége

### `GalaxyView`

- a látható galaxisrégiók és csillagkatalógus lekérése;
- csillag-LOD, ködök, galaktikus struktúrák és GPU-s renderelés;
- csillagkijelölés stabil `StarId` alapján;
- kamerafókusz és a `SystemView` megnyitásához szükséges kontextus előállítása.

A `GalaxyView` nem generál előre részletes rendszert minden csillaghoz, és nem hoz létre csillagonként `Node3D`-t.

### `SystemView`

- a kiválasztott `StarId`-hoz tartozó rendszer igény szerinti lekérése;
- csillag, bolygók, holdak, gyűrűk és aszteroidaövek megjelenítése;
- bolygókijelölés stabil `PlanetId` alapján;
- vizuális pályák és a fizikai modell közötti adaptáció.

A `SystemView` csak az aktuális rendszer részletes modelljét tartja életben. A jelenetbeli távolságok prezentációs távolságok lehetnek, de az adatmodell mértékegységeit nem írhatják felül.

### `PlanetView`

- a kiválasztott bolygó közeli, körbeforgatható megjelenítése;
- a motorfüggetlen bolygóparaméterek leképezése shader- és renderparaméterekre;
- légkör, felhők, óceán, gyűrű vagy közeli hold vizuális kezelése;
- bolygónézethez tartozó kamera és UI.

Ebben a projektfázisban nem felel leszállásért, bejárható felszínért vagy folyamatos átmenetű planetáris terepért.

## Saját koordinátarendszer nézetenként

A léptékek közötti nagyságrendi különbség miatt minden nézet lokális, lebegőpontos pontosságra optimalizált koordinátateret használ.

| Nézet | Logikai lépték | Godot-tér |
|---|---|---|
| `GalaxyView` | galaktikus koordináták, régiók, parsec jellegű távolságok | kamerához vagy aktív régióhoz közeli lokális tér |
| `SystemView` | csillagközpontú pályaadatok, AU jellegű távolságok | külön, vizuálisan skálázott rendszer-tér |
| `PlanetView` | bolygósugár, atmoszféra- és felszíni paraméterek | bolygóközpontú lokális tér |

A nézetek között nem viszünk át nyers Godot `Transform3D` objektumokat világpozícióként. Stabil objektum-ID és magasabb szintű fókuszkontextus kerül átadásra. Az átmenet közepén a régi jelenet és koordinátatér megszűnik, az új létrejön; ezt a `TransitionController` animációja takarja el.

## Kameraállapotok

A jelenlegi `ViewRouter` nézetenként `CameraState` értéket tárol. Ez tartalmazza:

- a kamera lokális `Transform3D` értékét;
- a látószöget (`Fov`);
- az ortografikus méretet (`Size`);
- a projekció típusát.

Navigáció előtt a router rögzíti az aktuális állapotot. Visszatéréskor az újonnan példányosított nézet kamerája megkapja a mentett értékeket, majd az orbitkamera belső szögei és távolsága újraszinkronizálódnak.

Később eldönthető, hogy egyetlen állapot tartozzon-e nézettípusonként, vagy a `SystemView` és `PlanetView` kamerája stabil objektum-ID szerint is gyorsítótárazódjon. Ez prezentációs állapot, ezért nem része a `Galaxy.Core` világgenerálásnak.

## Nagy elemszámú csillagmegjelenítés

A `GalaxyView` 50 000 csillagot egyetlen `MultiMeshInstance3D` node-ban jelenít meg. Csillagonként nem készül `Node3D`, `MeshInstance3D`, fizikai test vagy egyedi material. Egyetlen külön `MeshInstance3D` szolgál a kijelölés vizuális jelölőjeként.

A tervezett adatút:

```text
Galaxy.Core régió-/csillagkatalógus
        ↓
tömbösített render snapshot / adapter
        ↓
MultiMesh vagy RenderingDevice GPU-bufferek
        ↓
culling + LOD + shaderes kirajzolás
```

Csak kevés, interaktív prezentációs objektum kaphat node-ot, például a kijelölésjelző, a fókuszpont vagy az aktuális rendszer elemei. A galaxisszintű picking nem épülhet milliónyi fizikai colliderre; térbeli index, analitikus sugárteszt vagy GPU ID-buffer használható.

## `MultiMesh`, `RenderingDevice` és compute shader irány

Az első skálázható renderlépcső elkészült `MultiMesh` használatával: a Core pozícióit, hőmérsékletét és fényességét az adapter transzformmá, példányszínné és méretté alakítja. Az egy példányra jutó adatmennyiség és a culling lehetőségei azonban korlátozottak.

A galaxisszintű kijelölés jelenleg a kamera sugarát analitikusan összeveti a tömbben tárolt csillagpozíciókkal. Ez kattintásonként lineáris, de 50 000 elemnél megfelelő első verzió, és nem igényel fizikai collidereket. Nagyobb katalógusnál térbeli index vagy GPU ID-buffer válthatja le.

A nagyobb elemszámú célarchitektúra a Godot `RenderingDevice` alacsonyabb szintű API-jára épülhet:

- storage bufferek a tömör csillagadatokhoz;
- compute shaderes frustum-, távolság- és fényességalapú culling;
- LOD-osztályozás és látható indexlisták előállítása;
- minimális CPU → GPU adatmozgatás és régiónkénti bufferfrissítés;
- shaderes pont-, billboard- vagy impostor-megjelenítés;
- compute vagy render shaderekből épített köd- és sűrűségtextúrák.

A konkrét GPU-reprezentáció nem szivároghat vissza a `Galaxy.Core` modelljeibe. A Core stabil katalógusadatot ad, az adapter pedig az aktuális render backend által igényelt memóriaképet hozza létre.

## Jelenlegi adat- és navigációs folyamat

1. A `MainController` fogadja a HUD-műveletet.
2. A `ViewRouter` elmenti az aktuális kameraállapotot.
3. A `TransitionController` a kamerát a fókusz felé zoomolja, és fényátmenetet jelenít meg.
4. Az animáció fedett pontján a router lecseréli a `GameView` példányt.
5. Az új nézet megkapja a `ViewContext` kijelölési kontextusát és korábbi kameraállapotát.
6. Az átmenet felfedi az új, saját koordinátaterű jelenetet.

A `ViewContext` a kiválasztott generált csillagot stabil `StarId` és megjelenítési név formájában adja át. A bolygók még helyőrző nevet használnak; a későbbi rendszer- és bolygógeneráláskor `PlanetId` lesz az elsődleges hivatkozás.

## Tesztelési határok

- A `Galaxy.Core` tesztjei Godot nélkül ellenőrzik a determinisztikusságot, invariánsokat és sorosítható modelleket.
- A Godot réteg smoke/integrációs tesztjei a jeleneteket, erőforrás-hivatkozásokat, kijelölést, navigációt és kamera-visszaállítást ellenőrzik.
- A render backendhez rögzített referencia-adatkészletek és teljesítménymérések szükségesek.
- A vizuális tesztek nem helyettesíthetik a seedből képzett adatok egzakt összehasonlítását.
