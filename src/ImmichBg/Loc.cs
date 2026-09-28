using System.Globalization;
using ImmichBg.Config;

namespace ImmichBg;

/// <summary>
/// UI and log texts in Catalan, Spanish and English. Each entry is [ca, es, en].
/// To add a language, add a column here and an entry to the language list in SettingsForm.
/// </summary>
public static class Loc
{
    static readonly string[] Codes = ["ca", "es", "en"];
    static int current = 2;

    public static void SetLanguage(string code)
    {
        if (string.IsNullOrEmpty(code)) code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var index = Array.IndexOf(Codes, code);
        current = index >= 0 ? index : 2;
    }

    public static string T(string key, params object?[] args)
    {
        var text = Texts.TryGetValue(key, out var row) ? row[current] : key;
        return args.Length == 0 ? text : string.Format(text, args);
    }

    public static string TargetName(TargetKind kind) => T(kind switch
    {
        TargetKind.Wallpaper => "target.wallpaper",
        TargetKind.Screensaver => "target.screensaver",
        _ => "target.lockscreen",
    });

    static readonly Dictionary<string, string[]> Texts = new()
    {
        // Targets
        ["target.wallpaper"] = ["Fons de pantalla", "Fondo de pantalla", "Wallpaper"],
        ["target.screensaver"] = ["Salvapantalles", "Salvapantallas", "Screensaver"],
        ["target.lockscreen"] = ["Pantalla de bloqueig", "Pantalla de bloqueo", "Lock screen"],

        // Tray menu and status
        ["menu.syncNow"] = ["Sincronitzar ara", "Sincronizar ahora", "Sync now"],
        ["menu.settings"] = ["Configuració…", "Configuración…", "Settings…"],
        ["menu.log"] = ["Registre…", "Registro…", "Log…"],
        ["menu.exit"] = ["Sortir", "Salir", "Exit"],
        ["status.waiting"] = ["Esperant la primera sincronització", "Esperando la primera sincronización", "Waiting for first sync"],
        ["status.notConfigured"] = ["Falta configurar la connexió", "Falta configurar la conexión", "Connection not configured"],
        ["status.nothingEnabled"] = ["Cap destí habilitat", "Ningún destino habilitado", "No destination enabled"],
        ["status.noKey"] = ["No es pot llegir la clau API", "No se puede leer la clave API", "Cannot read the API key"],
        ["status.syncing"] = ["Sincronitzant…", "Sincronizando…", "Syncing…"],
        ["status.ok"] = ["Última sincronització: {0} – correcta", "Última sincronización: {0} – correcta", "Last sync: {0} – OK"],
        ["status.error"] = ["Última sincronització: {0} – amb errors (mira el registre)", "Última sincronización: {0} – con errores (mira el registro)", "Last sync: {0} – with errors (see log)"],

        // Log messages
        ["log.started"] = ["Immich BG {0} iniciat", "Immich BG {0} iniciado", "Immich BG {0} started"],
        ["log.exiting"] = ["Tancant Immich BG", "Cerrando Immich BG", "Closing Immich BG"],
        ["log.configSaved"] = ["Configuració desada", "Configuración guardada", "Settings saved"],
        ["log.targetDone"] = ["{0}: +{1} / −{2} · {3} fotos a la carpeta ({4})", "{0}: +{1} / −{2} · {3} fotos en la carpeta ({4})", "{0}: +{1} / −{2} · {3} photos in folder ({4})"],
        ["log.targetFailed"] = ["{0}: error, no s'ha esborrat res. {1}", "{0}: error, no se ha borrado nada. {1}", "{0}: failed, nothing was deleted. {1}"],
        ["log.folderChanged"] = ["{0}: la carpeta ha canviat; s'esborren les fotos de l'antiga ({1})", "{0}: la carpeta ha cambiado; se borran las fotos de la antigua ({1})", "{0}: folder changed; removing photos from the old one ({1})"],
        ["log.downloadFailed"] = ["No s'ha pogut descarregar {0}: {1}", "No se ha podido descargar {0}: {1}", "Could not download {0}: {1}"],
        ["log.deleteFailed"] = ["No s'ha pogut esborrar {0}: {1}", "No se ha podido borrar {0}: {1}", "Could not delete {0}: {1}"],
        ["log.fullsizeUnsupported"] = ["El servidor no suporta imatges 'fullsize'; es fa servir 'preview' (menys resolució)", "El servidor no soporta imágenes 'fullsize'; se usa 'preview' (menos resolución)", "Server does not support 'fullsize' images; using 'preview' (lower resolution)"],
        ["log.fullsizeMissing"] = ["{0}: no hi ha versió 'fullsize', es fa servir 'preview'", "{0}: no hay versión 'fullsize', se usa 'preview'", "{0}: no 'fullsize' version, using 'preview'"],
        ["log.lockRotateFailed"] = ["No s'ha pogut canviar la pantalla de bloqueig: {0}", "No se ha podido cambiar la pantalla de bloqueo: {0}", "Could not change the lock screen: {0}"],
        ["log.title"] = ["Registre", "Registro", "Log"],
        ["log.openFolder"] = ["Obrir carpeta de registres", "Abrir carpeta de registros", "Open log folder"],
        ["src.favorites"] = ["preferits", "favoritos", "favorites"],
        ["src.album"] = ["àlbum «{0}»", "álbum «{0}»", "album “{0}”"],

        // Errors
        ["err.unauthorized"] = ["Clau API no vàlida (401)", "Clave API no válida (401)", "Invalid API key (401)"],
        ["err.forbidden"] = ["La clau API no té prou permisos (403). {0}", "La clave API no tiene permisos suficientes (403). {0}", "The API key lacks permissions (403). {0}"],
        ["err.albumNotFound"] = ["L'àlbum no existeix o ja no es comparteix amb tu", "El álbum no existe o ya no se comparte contigo", "The album does not exist or is no longer shared with you"],
        ["err.noFolder"] = ["Cal triar una carpeta de destí", "Hay que elegir una carpeta de destino", "Choose a destination folder"],
        ["err.folderNotAbsolute"] = ["La carpeta ha de ser una ruta completa (p. ex. C:\\...)", "La carpeta debe ser una ruta completa (p. ej. C:\\...)", "The folder must be a full path (e.g. C:\\...)"],
        ["err.noAlbum"] = ["Cal triar un àlbum", "Hay que elegir un álbum", "Choose an album"],
        ["err.sharedFolder"] = ["Aquesta carpeta ja la fa servir un altre destí", "Esta carpeta ya la usa otro destino", "This folder is already used by another destination"],
        ["err.duplicateFolder"] = ["{0} i {1} no poden fer servir la mateixa carpeta", "{0} y {1} no pueden usar la misma carpeta", "{0} and {1} cannot use the same folder"],
        ["err.tooManyFailures"] = ["S'atura després de {0} errors de descàrrega", "Se detiene tras {0} errores de descarga", "Stopped after {0} download errors"],
        ["err.someFailed"] = ["{0} fotos no s'han pogut descarregar", "{0} fotos no se han podido descargar", "{0} photos could not be downloaded"],
        ["err.timeout"] = ["no respon (temps d'espera esgotat)", "no responde (tiempo de espera agotado)", "no answer (timed out)"],

        // Settings window
        ["settings.title"] = ["Configuració", "Configuración", "Settings"],
        ["settings.save"] = ["Desar", "Guardar", "Save"],
        ["settings.cancel"] = ["Cancel·lar", "Cancelar", "Cancel"],
        ["tab.connection"] = ["Connexió", "Conexión", "Connection"],
        ["tab.general"] = ["General", "General", "General"],
        ["conn.url"] = ["URL del servidor:", "URL del servidor:", "Server URL:"],
        ["conn.key"] = ["Clau API:", "Clave API:", "API key:"],
        ["conn.keySaved"] = ["(desada – deixa-ho buit per mantenir-la)", "(guardada – déjalo vacío para mantenerla)", "(saved – leave empty to keep it)"],
        ["conn.test"] = ["Provar connexió", "Probar conexión", "Test connection"],
        ["conn.testing"] = ["Connectant…", "Conectando…", "Connecting…"],
        ["conn.missing"] = ["Escriu la URL i la clau API", "Escribe la URL y la clave API", "Enter the URL and the API key"],
        ["conn.ok"] = ["Immich {0} · usuari {1}", "Immich {0} · usuario {1}", "Immich {0} · user {1}"],
        ["conn.failed"] = ["✖ No s'ha pogut connectar: {0}", "✖ No se ha podido conectar: {0}", "✖ Could not connect: {0}"],
        ["conn.help"] = [
            "Crea la clau a Immich › Configuració del compte › Claus API. Permisos necessaris: user.read, album.read, asset.read, asset.view. La clau es desa xifrada amb el teu usuari de Windows.",
            "Crea la clave en Immich › Ajustes de la cuenta › Claves API. Permisos necesarios: user.read, album.read, asset.read, asset.view. La clave se guarda cifrada con tu usuario de Windows.",
            "Create the key in Immich › Account settings › API keys. Required permissions: user.read, album.read, asset.read, asset.view. The key is stored encrypted with your Windows user."],
        ["general.interval"] = ["Comprovar canvis cada:", "Comprobar cambios cada:", "Check for changes every:"],
        ["general.minutes"] = ["minuts", "minutos", "minutes"],
        ["general.startup"] = ["Iniciar amb Windows", "Iniciar con Windows", "Start with Windows"],
        ["general.language"] = ["Idioma:", "Idioma:", "Language:"],
        ["general.langAuto"] = ["Automàtic (sistema)", "Automático (sistema)", "Automatic (system)"],
        ["general.langHint"] = ["El canvi d'idioma s'aplica en tornar a obrir aquesta finestra.", "El cambio de idioma se aplica al volver a abrir esta ventana.", "Language changes apply when this window is reopened."],

        // Target panel
        ["target.enabled"] = ["Habilitar sincronització", "Habilitar sincronización", "Enable sync"],
        ["target.folder"] = ["Carpeta de destí:", "Carpeta de destino:", "Destination folder:"],
        ["target.browse"] = ["Examinar…", "Examinar…", "Browse…"],
        ["target.source"] = ["Font:", "Origen:", "Source:"],
        ["target.album"] = ["Àlbum", "Álbum", "Album"],
        ["target.favorites"] = ["Preferits", "Favoritos", "Favorites"],
        ["target.amount"] = ["Quantitat:", "Cantidad:", "Amount:"],
        ["target.all"] = ["Totes", "Todas", "All"],
        ["target.last"] = ["Només les últimes", "Solo las últimas", "Only the latest"],
        ["target.photos"] = ["fotos (per data de captura)", "fotos (por fecha de captura)", "photos (by capture date)"],
        ["target.filter"] = ["Filtre:", "Filtro:", "Filter:"],
        ["target.orientation"] = ["Només fotos", "Solo fotos", "Only"],
        ["target.landscape"] = ["horitzontals", "horizontales", "landscape"],
        ["target.portrait"] = ["verticals", "verticales", "portrait"],
        ["target.folderNotEmpty"] = [
            "La carpeta ja conté {0} fitxers. L'app no els esborrarà mai (només esborra les fotos que ella mateixa descarrega), però Windows també els mostrarà.",
            "La carpeta ya contiene {0} archivos. La app nunca los borrará (solo borra las fotos que ella misma descarga), pero Windows también los mostrará.",
            "The folder already contains {0} files. The app will never delete them (it only deletes photos it downloaded itself), but Windows will show them too."],

        // Windows integration
        ["win.group"] = ["Aplicar a Windows", "Aplicar a Windows", "Apply to Windows"],
        ["win.apply"] = ["Aplicar ara", "Aplicar ahora", "Apply now"],
        ["win.shuffle"] = ["Ordre aleatori", "Orden aleatorio", "Shuffle"],
        ["win.wallpaper.minutes"] = ["Canviar imatge cada (min):", "Cambiar imagen cada (min):", "Change picture every (min):"],
        ["win.screensaver.minutes"] = ["Activar després de (min):", "Activar después de (min):", "Start after (min):"],
        ["win.lock.minutes"] = ["cada (min):", "cada (min):", "every (min):"],
        ["win.lock.selfRotate"] = ["Que l'app canviï la imatge", "Que la app cambie la imagen", "Let the app change the picture"],
        ["win.wallpaper.help"] = [
            "Posa el fons de pantalla en mode presentació amb aquesta carpeta.",
            "Pone el fondo de pantalla en modo presentación con esta carpeta.",
            "Sets the desktop background to a slideshow of this folder."],
        ["win.screensaver.help"] = [
            "Activa el salvapantalles «Fotos» de Windows amb aquesta carpeta.",
            "Activa el salvapantallas «Fotos» de Windows con esta carpeta.",
            "Enables the Windows “Photos” screensaver with this folder."],
        ["win.lock.help"] = [
            "Windows 10 no permet que una app triï la carpeta de la presentació de la pantalla de bloqueig. Pots triar-la tu a Configuració (el botó l'obre i copia la ruta), o marcar l'opció perquè l'app vagi canviant la imatge ella mateixa (cal desar).",
            "Windows 10 no permite que una app elija la carpeta de la presentación de la pantalla de bloqueo. Puedes elegirla tú en Configuración (el botón la abre y copia la ruta), o marcar la opción para que la app vaya cambiando la imagen ella misma (hay que guardar).",
            "Windows 10 does not let apps choose the lock screen slideshow folder. You can pick it in Settings (the button opens it and copies the path), or tick the option so the app changes the picture itself (remember to save)."],
        ["win.wallpaper.ok"] = ["✔ Fons en mode presentació amb aquesta carpeta.", "✔ Fondo en modo presentación con esta carpeta.", "✔ Background set to a slideshow of this folder."],
        ["win.screensaver.ok"] = ["✔ Salvapantalles «Fotos» configurat. Pots comprovar-ho al diàleg del salvapantalles (Vista prèvia).", "✔ Salvapantallas «Fotos» configurado. Puedes comprobarlo en el diálogo del salvapantallas (Vista previa).", "✔ “Photos” screensaver configured. You can check it in the screensaver dialog (Preview)."],
        ["win.screensaver.manual"] = ["S'ha obert el diàleg: tria «Fotos» › Configuració › Examinar i enganxa la ruta (ja és al porta-retalls).", "Se ha abierto el diálogo: elige «Fotos» › Configuración › Examinar y pega la ruta (ya está en el portapapeles).", "The dialog is open: choose “Photos” › Settings › Browse and paste the path (already in the clipboard)."],
        ["win.lock.instructions"] = [
            "Ruta copiada. A Configuració: Fons › «Presentació de diapositives» › Afegeix una carpeta › enganxa la ruta (Ctrl+V). Treu-ne les altres carpetes.",
            "Ruta copiada. En Configuración: Fondo › «Presentación» › Agregar una carpeta › pega la ruta (Ctrl+V). Quita las otras carpetas.",
            "Path copied. In Settings: Background › “Slideshow” › Add a folder › paste the path (Ctrl+V). Remove the other folders."],
        ["win.lock.rotated"] = ["✔ Imatge de la pantalla de bloqueig canviada. Prem Win+L per veure-la.", "✔ Imagen de la pantalla de bloqueo cambiada. Pulsa Win+L para verla.", "✔ Lock screen picture changed. Press Win+L to see it."],
        ["win.lock.empty"] = ["La carpeta encara no té imatges.", "La carpeta aún no tiene imágenes.", "The folder has no images yet."],
        ["win.noImagesYet"] = ["(La carpeta encara és buida: sincronitza primer.)", "(La carpeta aún está vacía: sincroniza primero.)", "(The folder is still empty: sync first.)"],
        ["win.failed"] = ["✖ No s'ha pogut aplicar: {0}", "✖ No se ha podido aplicar: {0}", "✖ Could not apply: {0}"],

        // Redesigned UI
        ["app.tagline"] = ["Fons des d'Immich", "Fondos desde Immich", "Backgrounds from Immich"],
        ["page.connection"] = ["On és el teu servidor Immich i amb quina clau s'hi accedeix", "Dónde está tu servidor Immich y con qué clave se accede", "Where your Immich server is and which key to use"],
        ["page.Wallpaper"] = ["Fotos per al fons de l'escriptori", "Fotos para el fondo del escritorio", "Photos for the desktop background"],
        ["page.Screensaver"] = ["Fotos per al salvapantalles de Windows", "Fotos para el salvapantallas de Windows", "Photos for the Windows screensaver"],
        ["page.LockScreen"] = ["Fotos per a la pantalla de bloqueig", "Fotos para la pantalla de bloqueo", "Photos for the lock screen"],
        ["page.general"] = ["Comportament de l'aplicació", "Comportamiento de la aplicación", "How the app behaves"],
        ["card.addresses"] = ["Adreces del servidor", "Direcciones del servidor", "Server addresses"],
        ["card.apiKey"] = ["Clau API", "Clave API", "API key"],
        ["card.connectionTest"] = ["Comprovació", "Comprobación", "Check"],
        ["card.folder"] = ["Carpeta de destí", "Carpeta de destino", "Destination folder"],
        ["card.photos"] = ["Quines fotos", "Qué fotos", "Which photos"],
        ["card.sync"] = ["Sincronització", "Sincronización", "Sync"],
        ["card.system"] = ["Sistema", "Sistema", "System"],
        ["card.language"] = ["Idioma", "Idioma", "Language"],
        ["card.about"] = ["Quant a", "Acerca de", "About"],
        ["conn.local"] = ["Adreça local:", "Dirección local:", "Local address:"],
        ["conn.remote"] = ["Adreça remota:", "Dirección remota:", "Remote address:"],
        ["conn.localShort"] = ["Local", "Local", "Local"],
        ["conn.remoteShort"] = ["Remota", "Remota", "Remote"],
        ["conn.localHint"] = [
            "Opcional. L'adreça dins de casa (p. ex. http://192.168.1.200:2283). Es prova primer.",
            "Opcional. La dirección dentro de casa (p. ej. http://192.168.1.200:2283). Se prueba primero.",
            "Optional. The address inside your home network (e.g. http://192.168.1.200:2283). Tried first."],
        ["conn.remoteHint"] = [
            "Per quan ets fora de casa (p. ex. https://fotos.exemple.com). Es fa servir si la local no respon.",
            "Para cuando estás fuera de casa (p. ej. https://fotos.ejemplo.com). Se usa si la local no responde.",
            "For when you are away from home (e.g. https://photos.example.com). Used if the local one does not answer."],
        ["conn.albums"] = ["{0} àlbums disponibles", "{0} álbumes disponibles", "{0} albums available"],
        ["conn.hairpin"] = [
            "És normal que l'adreça remota no respongui des de casa: molts routers no permeten accedir a la pròpia adreça pública des de dins (NAT loopback). Fora de casa funcionarà, i a casa es farà servir la local.",
            "Es normal que la dirección remota no responda desde casa: muchos routers no permiten acceder a la propia dirección pública desde dentro (NAT loopback). Fuera de casa funcionará, y en casa se usará la local.",
            "It is normal for the remote address not to answer from home: many routers cannot reach their own public address from inside (NAT loopback). It will work when away, and the local one is used at home."],
        ["conn.hairpinMaybe"] = [
            "Si ets a la mateixa xarxa que el servidor, afegeix-ne l'adreça local: molts routers no permeten accedir a la pròpia adreça pública des de dins (NAT loopback).",
            "Si estás en la misma red que el servidor, añade su dirección local: muchos routers no permiten acceder a la propia dirección pública desde dentro (NAT loopback).",
            "If you are on the same network as the server, add its local address: many routers cannot reach their own public address from inside (NAT loopback)."],
        ["target.enabledFor"] = ["Sincronitzar: {0}", "Sincronizar: {0}", "Sync: {0}"],
        ["target.enabledHint"] = [
            "La carpeta es mantindrà igual que l'origen triat: s'hi afegeixen les fotos noves i s'esborren les que ja no hi són.",
            "La carpeta se mantendrá igual que el origen elegido: se añaden las fotos nuevas y se borran las que ya no están.",
            "The folder is kept identical to the chosen source: new photos are added and removed ones are deleted."],
        ["target.folderHint"] = [
            "Millor una carpeta buida i només per a això. L'app només esborra les fotos que ella mateixa descarrega.",
            "Mejor una carpeta vacía y solo para esto. La app solo borra las fotos que ella misma descarga.",
            "Preferably an empty folder used only for this. The app only deletes photos it downloaded itself."],
        ["general.intervalHint"] = [
            "Cada comprovació és una consulta petita; només es descarrega si hi ha canvis. També se'n fa una en encendre i en tornar de la suspensió.",
            "Cada comprobación es una consulta pequeña; solo se descarga si hay cambios. También se hace una al encender y al volver de la suspensión.",
            "Each check is a small query; downloads only happen when something changed. Also runs at startup and after sleep."],
        ["general.startupHint"] = [
            "L'app s'engega en silenci a la safata del sistema quan inicies sessió.",
            "La app arranca en silencio en la bandeja del sistema al iniciar sesión.",
            "The app starts quietly in the system tray when you sign in."],
        ["general.version"] = ["Versió", "Versión", "Version"],
        ["general.openConfig"] = ["Obrir carpeta de configuració", "Abrir carpeta de configuración", "Open settings folder"],
        ["log.hint"] = ["Els últims 500 missatges. Es guarden 14 dies.", "Los últimos 500 mensajes. Se guardan 14 días.", "Latest 500 messages. Kept for 14 days."],
        ["log.copy"] = ["Copiar", "Copiar", "Copy"],
        ["log.usingAddress"] = ["Connectat a {0}", "Conectado a {0}", "Connected to {0}"],
        ["log.unreachable"] = ["Cap adreça del servidor respon ({0}). Es tornarà a provar al següent cicle.", "Ninguna dirección del servidor responde ({0}). Se volverá a probar en el siguiente ciclo.", "No server address answers ({0}). Will retry next cycle."],
        ["status.unreachable"] = ["{0} – el servidor no respon", "{0} – el servidor no responde", "{0} – server not reachable"],
        ["err.tls"] = ["error del certificat HTTPS", "error del certificado HTTPS", "HTTPS certificate error"],
        ["err.dns"] = ["no es troba l'adreça (DNS)", "no se encuentra la dirección (DNS)", "address not found (DNS)"],
        ["err.refused"] = ["connexió rebutjada (port incorrecte o servidor aturat)", "conexión rechazada (puerto incorrecto o servidor parado)", "connection refused (wrong port or server stopped)"],
        ["err.badUrl"] = ["l'adreça no és vàlida", "la dirección no es válida", "invalid address"],
    };
}
