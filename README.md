# Immich BG

App de Windows que manté tres carpetes sincronitzades amb el teu servidor [Immich](https://immich.app): una per al **fons de pantalla**, una per al **salvapantalles** i una per a la **pantalla de bloqueig**. L'origen de cada carpeta pot ser un àlbum o els preferits, amb totes les fotos o només les últimes X, i es pot filtrar per orientació.

L'app no mostra cap imatge. Només descarrega i esborra fitxers, i Windows fa la presentació de diapositives. Quan no sincronitza, ocupa uns 16 MB de RAM i no fa servir CPU.

## Posada en marxa

1. A Immich, obre **Configuració del compte › Claus API** i crea una clau amb aquests permisos:
   `user.read`, `album.read`, `asset.read` i `asset.view`.
2. Executa `ImmichBg.exe`. S'obrirà la finestra de configuració.
3. A la pàgina **Connexió**, escriu l'**adreça local** (per exemple `http://192.168.1.200:2283`), l'**adreça remota** (per exemple `https://fotos.exemple.com`) i la clau. Després prem **Provar connexió**.
   - A cada sincronització, l'app prova primer la local i, si no respon, la remota, igual que l'app mòbil d'Immich.
   - Des de casa és normal que la remota no respongui. Molts routers no poden accedir a la seva pròpia IP pública des de dins (*NAT loopback*).
4. A cada pàgina de destí:
   1. Activa l'interruptor **Sincronitzar**.
   2. Tria la carpeta, l'origen, la quantitat i el filtre.
   3. Prem **Desar**.
5. Quan la primera sincronització hagi descarregat fotos (menú de la safata › *Sincronitzar ara*), torna a cada pàgina i prem **Aplicar ara** (a la targeta *Aplicar a Windows*).

Si el fons, el salvapantalles o la pantalla de bloqueig no es veuen bé en alguna foto, és probable que la imatge que envia el servidor tingui poca resolució. Consulta la secció [Qualitat de les imatges](#qualitat-de-les-imatges).

## Com funciona

- Cada *N* minuts (30 per defecte), l'app consulta la llista de fotos de l'origen. Descarrega les que falten i esborra les que ja no hi han de ser. La primera comprovació es fa 1 minut després d'engegar l'app. També se'n fa una en tornar de la suspensió.
- **Seguretat:** l'app només esborra fitxers que ella mateixa ha descarregat. Els té apuntats a `%APPDATA%\ImmichBg\state\`. Si el servidor falla o l'àlbum ja no existeix, no esborra res.
- Les fotos es baixen en JPEG a resolució completa (`fullsize`). Si el servidor no el té, es baixa la versió `preview`. Així funcionen també els HEIC i els RAW.
- Els noms dels fitxers són del tipus `20240131_183005_1a2b3c4d.jpg` (data de captura + identificador).
- Registre: menú de la safata › *Registre…*, o els fitxers de `%LOCALAPPDATA%\ImmichBg\logs\` (es guarden 14 dies).
- La icona de la safata canvia de color: gris vol dir sense configurar, blau sincronitzant, verd correcte i vermell hi ha hagut errors.

### Integració amb Windows ("Aplicar a Windows")

| Destí | Com es fa | Fiabilitat |
|---|---|---|
| Fons de pantalla | Amb l'API oficial `IDesktopWallpaper`: presentació de la carpeta, amb interval i ordre aleatori | Alta |
| Salvapantalles | Configura el salvapantalles «Fotos» de Windows a través del registre (format no documentat) | Mitjana. Si falla, s'obre el diàleg del salvapantalles i la ruta queda al porta-retalls. |
| Pantalla de bloqueig | Windows 10 no deixa que una app triï la carpeta. Hi ha dues opcions: **(a)** el botó obre Configuració i copia la ruta perquè la triïs tu, o **(b)** marques *Que l'app canviï la imatge* i l'app la va canviant cada N minuts | (a) Alta, (b) cal provar-la al teu PC |

## Qualitat de les imatges

- Perquè els HEIC i els RAW es descarreguin a resolució completa, activa a Immich **Administració › Configuració › Imatges › *Generar imatges a mida completa***.
- Si el servidor genera les imatges en WebP (també a la configuració d'Immich), és possible que el fons de Windows 10 no les mostri. Fes servir JPEG.

## Instal·lar en altres equips

Comparteix `installer\Output\ImmichBg-Setup-<versió>.exe` (uns 36 MB).

- S'instal·la per usuari, a `%LOCALAPPDATA%\Programs\ImmichBg`, sense permisos d'administrador. Crea una drecera al menú Inici i apareix a *Configuració › Aplicacions*.
- Com que l'executable no està signat, la primera vegada Windows mostra un avís de SmartScreen. Cal prémer *Més informació › Executa igualment*.
- **Actualitzar:** n'hi ha prou amb executar l'instal·lador de la versió nova. Tanca l'app si està oberta i conserva la configuració.
- **Desinstal·lar:** des de *Configuració › Aplicacions*. Treu l'inici automàtic i pregunta si també cal esborrar la configuració. Les fotos descarregades no s'esborren.
- Cada equip necessita la seva pròpia clau API, que queda xifrada amb l'usuari de Windows.

Per publicar una versió nova, apuja `<Version>` a `src/ImmichBg/ImmichBg.csproj` i executa `.\publish.ps1`. Cal tenir Inno Setup 6 instal·lat: `winget install JRSoftware.InnoSetup`.

## Desenvolupament

Cal el [.NET 10 SDK](https://dotnet.microsoft.com/download). Amb VS Code, instal·la l'extensió **C# Dev Kit**.

```powershell
dotnet build                          # compila
dotnet test                           # tests de la lògica de sincronització
dotnet run --project src/ImmichBg     # executa en mode desenvolupament
.\publish.ps1                         # tests + publish\ImmichBg.exe + instal·lador
```

Estructura:

```
src/ImmichBg/
  Program.cs              arrencada, una sola instància, prioritat baixa
  TrayAppContext.cs       icona de safata, bucle periòdic, menú
  Loc.cs                  textos en català, castellà i anglès
  Config/                 configuració (JSON + clau API xifrada amb DPAPI)
  Immich/                 client de l'API d'Immich
  Sync/                   SyncPlanner (lògica pura), Manifest, SyncEngine
  WindowsIntegration/     fons, salvapantalles, pantalla de bloqueig, inici amb Windows
  UI/                     finestres (construïdes en codi, sense dissenyador)
tests/ImmichBg.Tests/     tests xUnit
installer/ImmichBg.iss    instal·lador (Inno Setup)
assets-src/               logotip original
tools/make-icons.ps1      genera app.ico i els logotips a partir de assets-src/logo-source.png
```

Fitxers de l'usuari:

- Configuració: `%APPDATA%\ImmichBg\config.json`
- Estat: `%APPDATA%\ImmichBg\state\`
- Registres: `%LOCALAPPDATA%\ImmichBg\logs\`
