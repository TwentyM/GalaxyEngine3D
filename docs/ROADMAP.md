# GalaxyEngine3D fejlesztési roadmap

Ez a roadmap a jelenlegi, működő projektváztól vezet a nagy elemszámú, determinisztikusan generált galaxisig. A mérföldkövek sorrendje szándékos: előbb a motorfüggetlen adatmodell és reprodukálhatóság készül el, utána épül rá a GPU-központú megjelenítés.

## Állapotjelölések

- **Kész**: a mérföldkő prototípus-szintű elfogadási feltételei teljesültek.
- **Következő**: a következő fejlesztési fókusz.
- **Tervezett**: még nem kezdődött el, vagy csak helyőrző demonstráció létezik.

## 1. Godot projektváz és nézetváltások — Kész

Cél: stabil Godot 4.7 .NET alap létrehozása a három, egymástól elkülönített léptékű nézethez.

Elkészült:

- `Main` jelenet, HUD, `ViewRouter` és `TransitionController`;
- külön `GalaxyView`, `SystemView` és `PlanetView` jelenet;
- helyőrző csillag- és bolygókijelölés;
- előre- és visszanavigálás animált, a jelenetcserét elfedő átmenettel;
- nézetenként mentett kamera-transzformáció, látószög, méret és projekció;
- orbitkamera, C# build és headless runtime smoke teszt.

Kilépési feltétel: mindhárom nézet oda-vissza bejárható, a kameraállapotok visszaállnak, a projekt Godot 4.7 .NET alatt hibamentesen indul.

## 2. Determinisztikus 3D galaxisgenerálás — Kész

Cél: a `Galaxy.Core` motorfüggetlen C# réteg és a reprodukálható galaxisadatok létrehozása, renderelési függőség nélkül.

Elkészült:

- külön `Galaxy.Core` projekt Godot-hivatkozás nélkül;
- 64 bites `GalaxySeed`, 1-es generátorverzió és rögzített, platformfüggetlen 64 bites keverési stratégia;
- stabil `StarId`, amely a seedből, szerepkonstansból és stabil csillagindexből származik;
- paraméterezhető spirálprofil: karszám, sugár, korongvastagság, dudorméret, csavarodás és csillagszám;
- motorfüggetlen csillagpozíció, hőmérséklet és fényesség;
- teljes katalógus és sorrendfüggetlen, index szerinti csillaglekérdezés;
- reprodukálhatósági, sorrendfüggetlenségi, tartomány- és rögzített ujjlenyomat-tesztek;
- kezdeti Godot MultiMesh-adapter 50 000 csillaghoz, csillagonkénti node-ok nélkül;
- analitikus sugártesztes kijelölés és stabil `StarId` átadása a `SystemView` kontextusába.

A régiókra bontott streamelés, halo és részletesebb csillagosztályok későbbi bővítésként maradnak; az első változat teljes katalógusa memóriában él.

Kilépési feltétel: azonos seed és generátorverzió minden futásban azonos csillagazonosítókat és adatokat ad, a generátor pedig Godot nélkül tesztelhető.

## 3. Galaxis látványvilága, csillag-LOD és ködök — Tervezett

Cél: nagy elemszámú galaxis megjelenítése csillagonkénti `Node3D` objektumok nélkül.

Tervezett eredmények (az első MultiMesh-lépcső már a 2. mérföldkőben elkészült):

- tömbösített CPU-adatút a `Galaxy.Core` katalógusából a renderelő felé;
- távolság/fényesség szerinti LOD a meglévő `MultiMesh`-alapú csillagmegjelenítéshez;
- térbeli culling és látható régiók streamelése;
- későbbi `RenderingDevice`, storage buffer és compute shader prototípus;
- galaktikus korong, színrégiók, por- és emissziós ködök rétegei;
- mérhető frame time-, memória- és feltöltési keretek reprezentatív csillagszámokra.

Kilépési feltétel: a célgépen nagy elemszám mellett interaktív kameramozgás érhető el, és egyetlen csillag sem igényel önálló Godot node-ot.

## 4. Csillagkijelölés és generált csillagrendszerek — Tervezett

Cél: a GPU-n megjelenített csillagok stabil azonosító alapján kijelölhetők legyenek, és csak a kiválasztott csillaghoz készüljön részletes rendszer.

Tervezett eredmények (a stabil ID-alapú analitikus alapkijelölés már működik):

- a jelenlegi lineáris analitikus sugárteszt skálázása térbeli indexszel vagy GPU ID-bufferrel;
- kijelöléshez fókuszálás és részletesebb csillaginformációk;
- a csillag stabil azonosítójából származtatott `SystemSeed`;
- motorfüggetlen csillagrendszer-modell és igény szerinti rendszer-generálás;
- a már működő stabil ID-alapú `GalaxyView` → `SystemView` kontextusátadás kiterjesztése generált rendszeradatra.

Kilépési feltétel: bármely látható csillag kijelölhető, és ugyanahhoz a csillaghoz minden alkalommal ugyanaz a rendszeradat tartozik.

## 5. Procedurális bolygók, holdak, gyűrűk és aszteroidaövek — Tervezett

Cél: koherens, determinisztikus rendszerfelépítés létrehozása a `SystemView` számára.

Tervezett eredmények:

- csillagtípushoz és pályatávolsághoz igazodó bolygóosztályok;
- stabil pályaindexekből származtatott bolygó- és holdseedek;
- holdrendszerek, gyűrűparaméterek és aszteroidaövek;
- vizuális lépték és fizikai/adatlépték világos szétválasztása;
- rendszerösszefüggések és generálási invariánsok automatikus tesztjei.

Kilépési feltétel: a generált rendszerek minden elemének stabil ID-ja és seedje van, és a `SystemView` a modellt helyőrzők nélkül képes megjeleníteni.

## 6. Bolygónézet és procedurális bolygóshaderek — Tervezett

Cél: a kiválasztott bolygó részletes, körbeforgatható, de még nem bejárható megjelenítése.

Tervezett eredmények:

- a bolygómodellből származtatott, stabil shaderparaméterek;
- több bolygóosztályhoz felszín-, óceán-, felhő-, légkör- és éjszakai fényváltozatok;
- gyűrűk és közeli holdak megjelenítése, ahol releváns;
- vizuális LOD és kiszámítható GPU-erőforrás-élettartam;
- a jelenlegi demonstrációs shader lecserélése determinisztikus megoldásra.

Kilépési feltétel: ugyanaz a bolygó minden megnyitáskor azonos vizuális paraméterekkel jelenik meg, több támogatott bolygóosztály mellett.

Nem cél ebben a mérföldkőben a leszállás, a bejárható felszín vagy a teljes planetáris terepgenerálás.

## 7. Mentés, felfedezett objektumok és elnevezések — Tervezett

Cél: csak a játékos által létrehozott vagy megváltoztatott állapot mentése, a determinisztikusan újragenerálható világadatok duplikálása nélkül.

Tervezett eredmények:

- mentésformátum seed-, generátorverzió- és sémaazonosítóval;
- felfedezett csillagok, rendszerek és bolygók stabil ID-alapú nyilvántartása;
- játékos által adott nevek és jegyzetek;
- migrációs stratégia formátum- vagy generátorverzió-váltáshoz;
- automatikus mentés/betöltés round-trip tesztek.

Kilépési feltétel: egy mentés visszatöltése ugyanazokat a felfedezéseket és neveket állítja helyre, miközben az alapvilág továbbra is seedből generálódik.

## 8. Optimalizálás és végleges átvezető animációk — Tervezett

Cél: a teljes adat- és renderpipeline profilozása, valamint a léptékváltások végleges vizuális kialakítása.

Tervezett eredmények:

- CPU-, GPU-, memória- és streamelési profilok reprezentatív jelenetekhez;
- allokációk, culling, bufferfrissítés és shaderköltségek optimalizálása;
- finomított galaxis → csillagrendszer és rendszer → bolygó átvezetés;
- az animáció közepén végzett jelenet- és koordinátaváltás teljes elfedése;
- megszakítható vagy védett navigáció, töltési hibák kezelése és vizuális fallback;
- Windows build- és teljesítményellenőrzési folyamat.

Kilépési feltétel: a célhardverre meghatározott teljesítménykeretek teljesülnek, a nézetváltások pedig látható ugrás nélkül, stabilan működnek.

## Átfogó korlátok

- A procedurális világmodell nem függhet Godottól vagy a renderelési technikától.
- A generálás eredménye nem függhet bejárási sorrendtől, szálütemezéstől vagy folyamatonként változó hashfüggvénytől.
- A részletes adatokat igény szerint generáljuk; nem építünk fel minden csillaghoz teljes rendszert előre.
- A GPU-s irány bevezetése mérésvezérelt legyen: előbb referencia-adatkészlet és teljesítménykeret, utána optimalizálás.
