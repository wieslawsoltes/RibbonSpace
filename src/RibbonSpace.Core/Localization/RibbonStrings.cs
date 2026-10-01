using System.Globalization;

namespace RibbonSpace.Localization;

/// <summary>
/// Localizable strings used by the built-in ribbon UI (menus, tooltips, dialogs). English, German, French, Spanish and
/// Polish are built in; register more with <see cref="Register"/> or override single keys with <see cref="Override"/>.
/// </summary>
public sealed class RibbonStrings
{
    private static readonly Dictionary<string, Dictionary<string, string>> Cultures = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new()
        {
            ["File"] = "File",
            ["CollapseRibbon"] = "Collapse the Ribbon",
            ["PinRibbon"] = "Pin the Ribbon",
            ["RibbonDisplayOptions"] = "Ribbon display options",
            ["FullScreenMode"] = "Full-screen mode",
            ["ShowTabsOnly"] = "Show tabs only",
            ["AlwaysShowRibbon"] = "Always show Ribbon",
            ["ShowPanelButtons"] = "Show panel buttons",
            ["ShowPanelTitles"] = "Show panel titles",
            ["MinimizeToTabs"] = "Minimize to Tabs",
            ["MinimizeToPanelTitles"] = "Minimize to Panel Titles",
            ["MinimizeToPanelButtons"] = "Minimize to Panel Buttons",
            ["CycleThroughAll"] = "Cycle through All",
            ["MinimizeRibbon"] = "Minimize the Ribbon",
            ["ShowFullRibbon"] = "Show Full Ribbon",
            ["ShowTabs"] = "Show Tabs",
            ["ShowPanels"] = "Show Panels",
            ["ShowGroupTitles"] = "Show Panel Titles",
            ["FloatPanel"] = "Float Panel",
            ["ReturnPanelToRibbon"] = "Return Panel to Ribbon",
            ["ReturnPanelsToRibbon"] = "Return Panels to Ribbon",
            ["PinPanel"] = "Keep panel open",
            ["UnpinPanel"] = "Unpin panel",
            ["ExpandPanel"] = "More {0} commands",
            ["RecentDocuments"] = "Recent Documents",
            ["SearchCommands"] = "Search commands",
            ["UseSimplifiedRibbon"] = "Simplified Ribbon",
            ["UseClassicRibbon"] = "Classic Ribbon",
            ["ShowQuickAccessToolbar"] = "Show Quick Access Toolbar",
            ["HideQuickAccessToolbar"] = "Hide Quick Access Toolbar",
            ["ShowAboveRibbon"] = "Show Above the Ribbon",
            ["ShowBelowRibbon"] = "Show Below the Ribbon",
            ["ShowCommandLabels"] = "Show Command Labels",
            ["CustomizeQuickAccessToolbar"] = "Customize Quick Access Toolbar",
            ["MoreCommands"] = "More Commands...",
            ["AddToQuickAccessToolbar"] = "Add to Quick Access Toolbar",
            ["RemoveFromQuickAccessToolbar"] = "Remove from Quick Access Toolbar",
            ["CustomizeRibbon"] = "Customize the Ribbon...",
            ["MoreOptions"] = "More options",
            ["SplitButtonOptions"] = "{0} options",
            ["ZoomIn"] = "Zoom in",
            ["ZoomOut"] = "Zoom out",
            ["Zoom"] = "Zoom",
            ["InvalidImport"] = "The text is not a valid ribbon customization (JSON). Nothing was imported.",
            ["Search"] = "Search",
            ["SearchPlaceholder"] = "Search (Alt+Q)",
            ["SearchNoResults"] = "No results",
            ["SearchRecent"] = "Recently used",
            ["SearchActions"] = "Actions",
            ["Back"] = "Back",
            ["Automatic"] = "Automatic",
            ["NoColor"] = "No Color",
            ["MoreColors"] = "More Colors...",
            ["ThemeColors"] = "Theme Colors",
            ["StandardColors"] = "Standard Colors",
            ["RecentColors"] = "Recent Colors",
            ["ScrollLeft"] = "Scroll left",
            ["ScrollRight"] = "Scroll right",
            ["GalleryUp"] = "Row up",
            ["GalleryDown"] = "Row down",
            ["GalleryMore"] = "More",
            ["GalleryFilter"] = "Filter",
            ["DialogLauncher"] = "{0} Settings",
            ["TablePickerFormat"] = "{0}x{1} Table",
            ["InsertTable"] = "Insert Table",
            ["Ok"] = "OK",
            ["Cancel"] = "Cancel",
            ["Reset"] = "Reset",
            ["NewTab"] = "New Tab",
            ["NewGroup"] = "New Group",
            ["Rename"] = "Rename...",
            ["Add"] = "Add >>",
            ["Remove"] = "<< Remove",
            ["MoveUp"] = "Move Up",
            ["MoveDown"] = "Move Down",
            ["ImportExport"] = "Import/Export",
            ["ChooseCommands"] = "Choose commands",
            ["MainTabs"] = "Main Tabs",
            ["QuickAccessToolbar"] = "Quick Access Toolbar",
            ["CustomGroupSuffix"] = "(Custom)",
            ["KeyTipsHint"] = "Press a key to choose a command. Press Esc to go back.",
            ["Close"] = "Close",
            ["Account"] = "Account",
            ["Options"] = "Options",
            ["CommandPalette"] = "Type a command",
        },
        ["de"] = new()
        {
            ["File"] = "Datei",
            ["CollapseRibbon"] = "Menüband reduzieren",
            ["PinRibbon"] = "Menüband fixieren",
            ["RibbonDisplayOptions"] = "Menüband-Anzeigeoptionen",
            ["FullScreenMode"] = "Vollbildmodus",
            ["ShowTabsOnly"] = "Nur Registerkarten anzeigen",
            ["AlwaysShowRibbon"] = "Menüband immer anzeigen",
            ["ShowPanelButtons"] = "Gruppenschaltflächen anzeigen",
            ["ShowPanelTitles"] = "Gruppentitel anzeigen",
            ["MinimizeToTabs"] = "Auf Registerkarten minimieren",
            ["MinimizeToPanelTitles"] = "Auf Gruppentitel minimieren",
            ["MinimizeToPanelButtons"] = "Auf Gruppenschaltflächen minimieren",
            ["CycleThroughAll"] = "Alle durchlaufen",
            ["MinimizeRibbon"] = "Menüband minimieren",
            ["ShowFullRibbon"] = "Vollständiges Menüband anzeigen",
            ["ShowTabs"] = "Registerkarten anzeigen",
            ["ShowPanels"] = "Gruppen anzeigen",
            ["ShowGroupTitles"] = "Gruppentitel anzeigen",
            ["FloatPanel"] = "Gruppe lösen",
            ["ReturnPanelToRibbon"] = "Gruppe an Menüband zurückgeben",
            ["ReturnPanelsToRibbon"] = "Gruppen an Menüband zurückgeben",
            ["PinPanel"] = "Gruppe geöffnet lassen",
            ["UnpinPanel"] = "Gruppe lösen",
            ["ExpandPanel"] = "Weitere Befehle: {0}",
            ["RecentDocuments"] = "Zuletzt verwendete Dokumente",
            ["SearchCommands"] = "Befehle suchen",
            ["UseSimplifiedRibbon"] = "Vereinfachtes Menüband",
            ["UseClassicRibbon"] = "Klassisches Menüband",
            ["ShowQuickAccessToolbar"] = "Symbolleiste für den Schnellzugriff anzeigen",
            ["HideQuickAccessToolbar"] = "Symbolleiste für den Schnellzugriff ausblenden",
            ["ShowAboveRibbon"] = "Über dem Menüband anzeigen",
            ["ShowBelowRibbon"] = "Unter dem Menüband anzeigen",
            ["ShowCommandLabels"] = "Befehlsbeschriftungen anzeigen",
            ["CustomizeQuickAccessToolbar"] = "Symbolleiste für den Schnellzugriff anpassen",
            ["MoreCommands"] = "Weitere Befehle...",
            ["AddToQuickAccessToolbar"] = "Zu Symbolleiste für den Schnellzugriff hinzufügen",
            ["RemoveFromQuickAccessToolbar"] = "Aus Symbolleiste für den Schnellzugriff entfernen",
            ["CustomizeRibbon"] = "Menüband anpassen...",
            ["MoreOptions"] = "Weitere Optionen",
            ["SplitButtonOptions"] = "{0} – Optionen",
            ["ZoomIn"] = "Vergrößern",
            ["ZoomOut"] = "Verkleinern",
            ["Zoom"] = "Zoom",
            ["InvalidImport"] = "Der Text ist keine gültige Menüband-Anpassung (JSON). Es wurde nichts importiert.",
            ["Search"] = "Suchen",
            ["SearchPlaceholder"] = "Suchen (Alt+Q)",
            ["SearchNoResults"] = "Keine Ergebnisse",
            ["SearchRecent"] = "Zuletzt verwendet",
            ["SearchActions"] = "Aktionen",
            ["Back"] = "Zurück",
            ["Automatic"] = "Automatisch",
            ["NoColor"] = "Keine Farbe",
            ["MoreColors"] = "Weitere Farben...",
            ["ThemeColors"] = "Designfarben",
            ["StandardColors"] = "Standardfarben",
            ["RecentColors"] = "Zuletzt verwendete Farben",
            ["ScrollLeft"] = "Nach links scrollen",
            ["ScrollRight"] = "Nach rechts scrollen",
            ["GalleryUp"] = "Zeile nach oben",
            ["GalleryDown"] = "Zeile nach unten",
            ["GalleryMore"] = "Mehr",
            ["GalleryFilter"] = "Filtern",
            ["DialogLauncher"] = "{0}-Einstellungen",
            ["TablePickerFormat"] = "{0}x{1}-Tabelle",
            ["InsertTable"] = "Tabelle einfügen",
            ["Ok"] = "OK",
            ["Cancel"] = "Abbrechen",
            ["Reset"] = "Zurücksetzen",
            ["NewTab"] = "Neue Registerkarte",
            ["NewGroup"] = "Neue Gruppe",
            ["Rename"] = "Umbenennen...",
            ["Add"] = "Hinzufügen >>",
            ["Remove"] = "<< Entfernen",
            ["MoveUp"] = "Nach oben",
            ["MoveDown"] = "Nach unten",
            ["ImportExport"] = "Importieren/Exportieren",
            ["ChooseCommands"] = "Befehle auswählen",
            ["MainTabs"] = "Hauptregisterkarten",
            ["QuickAccessToolbar"] = "Symbolleiste für den Schnellzugriff",
            ["CustomGroupSuffix"] = "(Benutzerdefiniert)",
            ["KeyTipsHint"] = "Drücken Sie eine Taste, um einen Befehl auszuwählen. Esc für zurück.",
            ["Close"] = "Schließen",
            ["Account"] = "Konto",
            ["Options"] = "Optionen",
            ["CommandPalette"] = "Befehl eingeben",
        },
        ["fr"] = new()
        {
            ["File"] = "Fichier",
            ["CollapseRibbon"] = "Réduire le ruban",
            ["PinRibbon"] = "Épingler le ruban",
            ["RibbonDisplayOptions"] = "Options d'affichage du ruban",
            ["FullScreenMode"] = "Mode plein écran",
            ["ShowTabsOnly"] = "Afficher uniquement les onglets",
            ["AlwaysShowRibbon"] = "Toujours afficher le ruban",
            ["ShowPanelButtons"] = "Afficher les boutons des groupes",
            ["ShowPanelTitles"] = "Afficher les titres des groupes",
            ["MinimizeToTabs"] = "Réduire aux onglets",
            ["MinimizeToPanelTitles"] = "Réduire aux titres des groupes",
            ["MinimizeToPanelButtons"] = "Réduire aux boutons des groupes",
            ["CycleThroughAll"] = "Parcourir tous les états",
            ["MinimizeRibbon"] = "Réduire le ruban",
            ["ShowFullRibbon"] = "Afficher le ruban complet",
            ["ShowTabs"] = "Afficher les onglets",
            ["ShowPanels"] = "Afficher les groupes",
            ["ShowGroupTitles"] = "Afficher les titres des groupes",
            ["FloatPanel"] = "Détacher le groupe",
            ["ReturnPanelToRibbon"] = "Remettre le groupe dans le ruban",
            ["ReturnPanelsToRibbon"] = "Remettre les groupes dans le ruban",
            ["PinPanel"] = "Garder le groupe ouvert",
            ["UnpinPanel"] = "Libérer le groupe",
            ["ExpandPanel"] = "Autres commandes : {0}",
            ["RecentDocuments"] = "Documents récents",
            ["SearchCommands"] = "Rechercher des commandes",
            ["UseSimplifiedRibbon"] = "Ruban simplifié",
            ["UseClassicRibbon"] = "Ruban classique",
            ["ShowQuickAccessToolbar"] = "Afficher la barre d'outils Accès rapide",
            ["HideQuickAccessToolbar"] = "Masquer la barre d'outils Accès rapide",
            ["ShowAboveRibbon"] = "Afficher au-dessus du ruban",
            ["ShowBelowRibbon"] = "Afficher sous le ruban",
            ["ShowCommandLabels"] = "Afficher les étiquettes de commande",
            ["CustomizeQuickAccessToolbar"] = "Personnaliser la barre d'outils Accès rapide",
            ["MoreCommands"] = "Autres commandes...",
            ["AddToQuickAccessToolbar"] = "Ajouter à la barre d'outils Accès rapide",
            ["RemoveFromQuickAccessToolbar"] = "Supprimer de la barre d'outils Accès rapide",
            ["CustomizeRibbon"] = "Personnaliser le ruban...",
            ["MoreOptions"] = "Plus d'options",
            ["SplitButtonOptions"] = "Options de {0}",
            ["ZoomIn"] = "Zoom avant",
            ["ZoomOut"] = "Zoom arrière",
            ["Zoom"] = "Zoom",
            ["InvalidImport"] = "Le texte n'est pas une personnalisation du ruban valide (JSON). Rien n'a été importé.",
            ["Search"] = "Rechercher",
            ["SearchPlaceholder"] = "Rechercher (Alt+Q)",
            ["SearchNoResults"] = "Aucun résultat",
            ["SearchRecent"] = "Récemment utilisées",
            ["SearchActions"] = "Actions",
            ["Back"] = "Retour",
            ["Automatic"] = "Automatique",
            ["NoColor"] = "Aucune couleur",
            ["MoreColors"] = "Autres couleurs...",
            ["ThemeColors"] = "Couleurs du thème",
            ["StandardColors"] = "Couleurs standard",
            ["RecentColors"] = "Couleurs récentes",
            ["ScrollLeft"] = "Défiler vers la gauche",
            ["ScrollRight"] = "Défiler vers la droite",
            ["GalleryUp"] = "Ligne précédente",
            ["GalleryDown"] = "Ligne suivante",
            ["GalleryMore"] = "Plus",
            ["GalleryFilter"] = "Filtrer",
            ["DialogLauncher"] = "Paramètres {0}",
            ["TablePickerFormat"] = "Tableau {0}x{1}",
            ["InsertTable"] = "Insérer un tableau",
            ["Ok"] = "OK",
            ["Cancel"] = "Annuler",
            ["Reset"] = "Réinitialiser",
            ["NewTab"] = "Nouvel onglet",
            ["NewGroup"] = "Nouveau groupe",
            ["Rename"] = "Renommer...",
            ["Add"] = "Ajouter >>",
            ["Remove"] = "<< Supprimer",
            ["MoveUp"] = "Monter",
            ["MoveDown"] = "Descendre",
            ["ImportExport"] = "Importer/Exporter",
            ["ChooseCommands"] = "Choisir les commandes",
            ["MainTabs"] = "Onglets principaux",
            ["QuickAccessToolbar"] = "Barre d'outils Accès rapide",
            ["CustomGroupSuffix"] = "(Personnalisé)",
            ["KeyTipsHint"] = "Appuyez sur une touche pour choisir une commande. Échap pour revenir.",
            ["Close"] = "Fermer",
            ["Account"] = "Compte",
            ["Options"] = "Options",
            ["CommandPalette"] = "Tapez une commande",
        },
        ["es"] = new()
        {
            ["File"] = "Archivo",
            ["CollapseRibbon"] = "Contraer la cinta de opciones",
            ["PinRibbon"] = "Anclar la cinta de opciones",
            ["RibbonDisplayOptions"] = "Opciones de presentación de la cinta",
            ["FullScreenMode"] = "Modo de pantalla completa",
            ["ShowTabsOnly"] = "Mostrar solo pestañas",
            ["AlwaysShowRibbon"] = "Mostrar siempre la cinta",
            ["ShowPanelButtons"] = "Mostrar botones de grupos",
            ["ShowPanelTitles"] = "Mostrar títulos de grupos",
            ["MinimizeToTabs"] = "Minimizar a pestañas",
            ["MinimizeToPanelTitles"] = "Minimizar a títulos de grupos",
            ["MinimizeToPanelButtons"] = "Minimizar a botones de grupos",
            ["CycleThroughAll"] = "Recorrer todos",
            ["MinimizeRibbon"] = "Minimizar la cinta",
            ["ShowFullRibbon"] = "Mostrar la cinta completa",
            ["ShowTabs"] = "Mostrar pestañas",
            ["ShowPanels"] = "Mostrar grupos",
            ["ShowGroupTitles"] = "Mostrar títulos de grupos",
            ["FloatPanel"] = "Desacoplar grupo",
            ["ReturnPanelToRibbon"] = "Devolver grupo a la cinta",
            ["ReturnPanelsToRibbon"] = "Devolver grupos a la cinta",
            ["PinPanel"] = "Mantener grupo abierto",
            ["UnpinPanel"] = "Desanclar grupo",
            ["ExpandPanel"] = "Más comandos de {0}",
            ["RecentDocuments"] = "Documentos recientes",
            ["SearchCommands"] = "Buscar comandos",
            ["UseSimplifiedRibbon"] = "Cinta de opciones simplificada",
            ["UseClassicRibbon"] = "Cinta de opciones clásica",
            ["ShowQuickAccessToolbar"] = "Mostrar barra de herramientas de acceso rápido",
            ["HideQuickAccessToolbar"] = "Ocultar barra de herramientas de acceso rápido",
            ["ShowAboveRibbon"] = "Mostrar encima de la cinta",
            ["ShowBelowRibbon"] = "Mostrar debajo de la cinta",
            ["ShowCommandLabels"] = "Mostrar etiquetas de comandos",
            ["CustomizeQuickAccessToolbar"] = "Personalizar barra de herramientas de acceso rápido",
            ["MoreCommands"] = "Más comandos...",
            ["AddToQuickAccessToolbar"] = "Agregar a la barra de herramientas de acceso rápido",
            ["RemoveFromQuickAccessToolbar"] = "Quitar de la barra de herramientas de acceso rápido",
            ["CustomizeRibbon"] = "Personalizar la cinta de opciones...",
            ["MoreOptions"] = "Más opciones",
            ["SplitButtonOptions"] = "Opciones de {0}",
            ["ZoomIn"] = "Acercar",
            ["ZoomOut"] = "Alejar",
            ["Zoom"] = "Zoom",
            ["InvalidImport"] = "El texto no es una personalización de la cinta válida (JSON). No se importó nada.",
            ["Search"] = "Buscar",
            ["SearchPlaceholder"] = "Buscar (Alt+Q)",
            ["SearchNoResults"] = "No hay resultados",
            ["SearchRecent"] = "Usados recientemente",
            ["SearchActions"] = "Acciones",
            ["Back"] = "Atrás",
            ["Automatic"] = "Automático",
            ["NoColor"] = "Sin color",
            ["MoreColors"] = "Más colores...",
            ["ThemeColors"] = "Colores del tema",
            ["StandardColors"] = "Colores estándar",
            ["RecentColors"] = "Colores recientes",
            ["ScrollLeft"] = "Desplazar a la izquierda",
            ["ScrollRight"] = "Desplazar a la derecha",
            ["GalleryUp"] = "Fila anterior",
            ["GalleryDown"] = "Fila siguiente",
            ["GalleryMore"] = "Más",
            ["GalleryFilter"] = "Filtrar",
            ["DialogLauncher"] = "Configuración de {0}",
            ["TablePickerFormat"] = "Tabla de {0}x{1}",
            ["InsertTable"] = "Insertar tabla",
            ["Ok"] = "Aceptar",
            ["Cancel"] = "Cancelar",
            ["Reset"] = "Restablecer",
            ["NewTab"] = "Nueva pestaña",
            ["NewGroup"] = "Nuevo grupo",
            ["Rename"] = "Cambiar nombre...",
            ["Add"] = "Agregar >>",
            ["Remove"] = "<< Quitar",
            ["MoveUp"] = "Subir",
            ["MoveDown"] = "Bajar",
            ["ImportExport"] = "Importar/Exportar",
            ["ChooseCommands"] = "Comandos disponibles",
            ["MainTabs"] = "Pestañas principales",
            ["QuickAccessToolbar"] = "Barra de herramientas de acceso rápido",
            ["CustomGroupSuffix"] = "(Personalizado)",
            ["KeyTipsHint"] = "Presione una tecla para elegir un comando. Esc para volver.",
            ["Close"] = "Cerrar",
            ["Account"] = "Cuenta",
            ["Options"] = "Opciones",
            ["CommandPalette"] = "Escriba un comando",
        },
        ["pl"] = new()
        {
            ["File"] = "Plik",
            ["CollapseRibbon"] = "Zwiń wstążkę",
            ["PinRibbon"] = "Przypnij wstążkę",
            ["RibbonDisplayOptions"] = "Opcje wyświetlania wstążki",
            ["FullScreenMode"] = "Tryb pełnoekranowy",
            ["ShowTabsOnly"] = "Pokaż tylko karty",
            ["AlwaysShowRibbon"] = "Zawsze pokazuj wstążkę",
            ["ShowPanelButtons"] = "Pokaż przyciski grup",
            ["ShowPanelTitles"] = "Pokaż tytuły grup",
            ["MinimizeToTabs"] = "Minimalizuj do kart",
            ["MinimizeToPanelTitles"] = "Minimalizuj do tytułów grup",
            ["MinimizeToPanelButtons"] = "Minimalizuj do przycisków grup",
            ["CycleThroughAll"] = "Przełączaj wszystkie",
            ["MinimizeRibbon"] = "Minimalizuj wstążkę",
            ["ShowFullRibbon"] = "Pokaż pełną wstążkę",
            ["ShowTabs"] = "Pokaż karty",
            ["ShowPanels"] = "Pokaż grupy",
            ["ShowGroupTitles"] = "Pokaż tytuły grup",
            ["FloatPanel"] = "Odepnij grupę",
            ["ReturnPanelToRibbon"] = "Przywróć grupę do wstążki",
            ["ReturnPanelsToRibbon"] = "Przywróć grupy do wstążki",
            ["PinPanel"] = "Zostaw grupę otwartą",
            ["UnpinPanel"] = "Odepnij grupę",
            ["ExpandPanel"] = "Więcej poleceń: {0}",
            ["RecentDocuments"] = "Ostatnie dokumenty",
            ["SearchCommands"] = "Szukaj poleceń",
            ["UseSimplifiedRibbon"] = "Uproszczona wstążka",
            ["UseClassicRibbon"] = "Klasyczna wstążka",
            ["ShowQuickAccessToolbar"] = "Pokaż pasek narzędzi Szybki dostęp",
            ["HideQuickAccessToolbar"] = "Ukryj pasek narzędzi Szybki dostęp",
            ["ShowAboveRibbon"] = "Pokaż nad wstążką",
            ["ShowBelowRibbon"] = "Pokaż pod wstążką",
            ["ShowCommandLabels"] = "Pokaż etykiety poleceń",
            ["CustomizeQuickAccessToolbar"] = "Dostosuj pasek narzędzi Szybki dostęp",
            ["MoreCommands"] = "Więcej poleceń...",
            ["AddToQuickAccessToolbar"] = "Dodaj do paska narzędzi Szybki dostęp",
            ["RemoveFromQuickAccessToolbar"] = "Usuń z paska narzędzi Szybki dostęp",
            ["CustomizeRibbon"] = "Dostosuj wstążkę...",
            ["MoreOptions"] = "Więcej opcji",
            ["SplitButtonOptions"] = "{0} – opcje",
            ["ZoomIn"] = "Powiększ",
            ["ZoomOut"] = "Pomniejsz",
            ["Zoom"] = "Powiększenie",
            ["InvalidImport"] = "Tekst nie jest prawidłowym dostosowaniem wstążki (JSON). Nic nie zaimportowano.",
            ["Search"] = "Wyszukaj",
            ["SearchPlaceholder"] = "Wyszukaj (Alt+Q)",
            ["SearchNoResults"] = "Brak wyników",
            ["SearchRecent"] = "Ostatnio używane",
            ["SearchActions"] = "Akcje",
            ["Back"] = "Wstecz",
            ["Automatic"] = "Automatyczny",
            ["NoColor"] = "Brak koloru",
            ["MoreColors"] = "Więcej kolorów...",
            ["ThemeColors"] = "Kolory motywu",
            ["StandardColors"] = "Kolory standardowe",
            ["RecentColors"] = "Ostatnie kolory",
            ["ScrollLeft"] = "Przewiń w lewo",
            ["ScrollRight"] = "Przewiń w prawo",
            ["GalleryUp"] = "Wiersz w górę",
            ["GalleryDown"] = "Wiersz w dół",
            ["GalleryMore"] = "Więcej",
            ["GalleryFilter"] = "Filtruj",
            ["DialogLauncher"] = "Ustawienia: {0}",
            ["TablePickerFormat"] = "Tabela {0}x{1}",
            ["InsertTable"] = "Wstaw tabelę",
            ["Ok"] = "OK",
            ["Cancel"] = "Anuluj",
            ["Reset"] = "Resetuj",
            ["NewTab"] = "Nowa karta",
            ["NewGroup"] = "Nowa grupa",
            ["Rename"] = "Zmień nazwę...",
            ["Add"] = "Dodaj >>",
            ["Remove"] = "<< Usuń",
            ["MoveUp"] = "Przenieś w górę",
            ["MoveDown"] = "Przenieś w dół",
            ["ImportExport"] = "Importuj/Eksportuj",
            ["ChooseCommands"] = "Wybierz polecenia",
            ["MainTabs"] = "Karty główne",
            ["QuickAccessToolbar"] = "Pasek narzędzi Szybki dostęp",
            ["CustomGroupSuffix"] = "(Niestandardowa)",
            ["KeyTipsHint"] = "Naciśnij klawisz, aby wybrać polecenie. Esc – wstecz.",
            ["Close"] = "Zamknij",
            ["Account"] = "Konto",
            ["Options"] = "Opcje",
            ["CommandPalette"] = "Wpisz polecenie",
        },
    };

    private static readonly Lock CulturesGate = new();
    private static RibbonStrings _current = ForCulture(CultureInfo.CurrentUICulture);
    private readonly Dictionary<string, string> _values;
    private readonly Dictionary<string, string> _overrides = [];

    private RibbonStrings(Dictionary<string, string> values, CultureInfo culture)
    {
        _values = values;
        Culture = culture;
    }

    /// <summary>
    /// Raised when <see cref="Current"/> changes, when <see cref="Override"/> changes the current instance, or when
    /// <see cref="Register"/> extends the current culture. Subscribers must unsubscribe (the event is static);
    /// RibbonSpace controls subscribe only while loaded.
    /// </summary>
    public static event EventHandler? CurrentChanged;

    /// <summary>Strings used by all ribbons.</summary>
    public static RibbonStrings Current
    {
        get => _current;
        set
        {
            _current = value ?? throw new ArgumentNullException(nameof(value));
            CurrentChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>Culture of this instance.</summary>
    public CultureInfo Culture { get; }

    /// <summary>Supported built-in culture names.</summary>
    public static IReadOnlyCollection<string> BuiltInCultures => Cultures.Keys;

    /// <summary>All keys.</summary>
    public static IReadOnlyCollection<string> Keys => Cultures["en"].Keys;

    /// <summary>Gets a string by key (falls back to English, then to the key).</summary>
    public string this[string key] => _values.TryGetValue(key, out var value) ? value
        : Cultures["en"].TryGetValue(key, out var english) ? english : key;

    /// <summary>Creates strings for a culture (neutral culture fallback, then English).</summary>
    public static RibbonStrings ForCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        lock (CulturesGate)
        {
            if (!Cultures.TryGetValue(culture.Name, out var values) && !Cultures.TryGetValue(culture.TwoLetterISOLanguageName, out values))
            {
                values = Cultures["en"];
            }

            return new RibbonStrings(new Dictionary<string, string>(values), culture);
        }
    }

    /// <summary>Registers (or extends) a culture. Missing keys fall back to English.</summary>
    public static void Register(string cultureName, IReadOnlyDictionary<string, string> values)
    {
        ArgumentException.ThrowIfNullOrEmpty(cultureName);
        ArgumentNullException.ThrowIfNull(values);
        lock (CulturesGate)
        {
            if (!Cultures.TryGetValue(cultureName, out var existing))
            {
                existing = new Dictionary<string, string>();
                Cultures[cultureName] = existing;
            }

            foreach (var pair in values)
            {
                existing[pair.Key] = pair.Value;
            }
        }

        // Refresh the current strings when the registration affects their culture (keeping instance overrides).
        var current = _current;
        if (string.Equals(current.Culture.Name, cultureName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(current.Culture.TwoLetterISOLanguageName, cultureName, StringComparison.OrdinalIgnoreCase))
        {
            var refreshed = ForCulture(current.Culture);
            foreach (var (key, value) in current._overrides)
            {
                refreshed.Override(key, value);
            }

            Current = refreshed;
        }
    }

    /// <summary>Overrides a key on this instance (e.g. rename "File" to "Home" for the application button).</summary>
    public RibbonStrings Override(string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);
        _values[key] = value;
        _overrides[key] = value;
        if (ReferenceEquals(this, _current))
        {
            CurrentChanged?.Invoke(null, EventArgs.Empty);
        }

        return this;
    }

    /// <summary>Formats a composite string.</summary>
    public string Format(string key, params object?[] args) => string.Format(Culture, this[key], args);

    /// <summary>File</summary>
    public string File => this[nameof(File)];
    /// <summary>Collapse the Ribbon</summary>
    public string CollapseRibbon => this[nameof(CollapseRibbon)];
    /// <summary>Pin the Ribbon</summary>
    public string PinRibbon => this[nameof(PinRibbon)];
    /// <summary>Ribbon display options</summary>
    public string RibbonDisplayOptions => this[nameof(RibbonDisplayOptions)];
    /// <summary>Full-screen mode</summary>
    public string FullScreenMode => this[nameof(FullScreenMode)];
    /// <summary>Show tabs only</summary>
    public string ShowTabsOnly => this[nameof(ShowTabsOnly)];
    /// <summary>Always show Ribbon</summary>
    public string AlwaysShowRibbon => this[nameof(AlwaysShowRibbon)];
    /// <summary>Show panel buttons</summary>
    public string ShowPanelButtons => this[nameof(ShowPanelButtons)];
    /// <summary>Show panel titles</summary>
    public string ShowPanelTitles => this[nameof(ShowPanelTitles)];
    /// <summary>Minimize to Tabs</summary>
    public string MinimizeToTabs => this[nameof(MinimizeToTabs)];
    /// <summary>Minimize to Panel Titles</summary>
    public string MinimizeToPanelTitles => this[nameof(MinimizeToPanelTitles)];
    /// <summary>Minimize to Panel Buttons</summary>
    public string MinimizeToPanelButtons => this[nameof(MinimizeToPanelButtons)];
    /// <summary>Cycle through All</summary>
    public string CycleThroughAll => this[nameof(CycleThroughAll)];
    /// <summary>Minimize the Ribbon</summary>
    public string MinimizeRibbon => this[nameof(MinimizeRibbon)];
    /// <summary>Show Full Ribbon</summary>
    public string ShowFullRibbon => this[nameof(ShowFullRibbon)];
    /// <summary>Show Tabs</summary>
    public string ShowTabs => this[nameof(ShowTabs)];
    /// <summary>Show Panels</summary>
    public string ShowPanels => this[nameof(ShowPanels)];
    /// <summary>Show Panel Titles</summary>
    public string ShowGroupTitles => this[nameof(ShowGroupTitles)];
    /// <summary>Float Panel</summary>
    public string FloatPanel => this[nameof(FloatPanel)];
    /// <summary>Return Panel to Ribbon</summary>
    public string ReturnPanelToRibbon => this[nameof(ReturnPanelToRibbon)];
    /// <summary>Return Panels to Ribbon</summary>
    public string ReturnPanelsToRibbon => this[nameof(ReturnPanelsToRibbon)];
    /// <summary>Keep panel open</summary>
    public string PinPanel => this[nameof(PinPanel)];
    /// <summary>Unpin panel</summary>
    public string UnpinPanel => this[nameof(UnpinPanel)];
    /// <summary>More {0} commands</summary>
    public string ExpandPanel => this[nameof(ExpandPanel)];
    /// <summary>Recent Documents</summary>
    public string RecentDocuments => this[nameof(RecentDocuments)];
    /// <summary>Search commands</summary>
    public string SearchCommands => this[nameof(SearchCommands)];
    /// <summary>Simplified Ribbon</summary>
    public string UseSimplifiedRibbon => this[nameof(UseSimplifiedRibbon)];
    /// <summary>Classic Ribbon</summary>
    public string UseClassicRibbon => this[nameof(UseClassicRibbon)];
    /// <summary>Show Quick Access Toolbar</summary>
    public string ShowQuickAccessToolbar => this[nameof(ShowQuickAccessToolbar)];
    /// <summary>Hide Quick Access Toolbar</summary>
    public string HideQuickAccessToolbar => this[nameof(HideQuickAccessToolbar)];
    /// <summary>Show Above the Ribbon</summary>
    public string ShowAboveRibbon => this[nameof(ShowAboveRibbon)];
    /// <summary>Show Below the Ribbon</summary>
    public string ShowBelowRibbon => this[nameof(ShowBelowRibbon)];
    /// <summary>Show Command Labels</summary>
    public string ShowCommandLabels => this[nameof(ShowCommandLabels)];
    /// <summary>Customize Quick Access Toolbar</summary>
    public string CustomizeQuickAccessToolbar => this[nameof(CustomizeQuickAccessToolbar)];
    /// <summary>More Commands...</summary>
    public string MoreCommands => this[nameof(MoreCommands)];
    /// <summary>Add to Quick Access Toolbar</summary>
    public string AddToQuickAccessToolbar => this[nameof(AddToQuickAccessToolbar)];
    /// <summary>Remove from Quick Access Toolbar</summary>
    public string RemoveFromQuickAccessToolbar => this[nameof(RemoveFromQuickAccessToolbar)];
    /// <summary>Customize the Ribbon...</summary>
    public string CustomizeRibbon => this[nameof(CustomizeRibbon)];
    /// <summary>More options</summary>
    public string MoreOptions => this[nameof(MoreOptions)];
    /// <summary>{0} options (name of a split button's drop-down part)</summary>
    public string SplitButtonOptions => this[nameof(SplitButtonOptions)];
    /// <summary>Zoom in</summary>
    public string ZoomIn => this[nameof(ZoomIn)];
    /// <summary>Zoom out</summary>
    public string ZoomOut => this[nameof(ZoomOut)];
    /// <summary>Zoom</summary>
    public string Zoom => this[nameof(Zoom)];
    /// <summary>Message shown when an imported customization is not valid JSON.</summary>
    public string InvalidImport => this[nameof(InvalidImport)];
    /// <summary>Search</summary>
    public string Search => this[nameof(Search)];
    /// <summary>Search (Alt+Q)</summary>
    public string SearchPlaceholder => this[nameof(SearchPlaceholder)];
    /// <summary>No results</summary>
    public string SearchNoResults => this[nameof(SearchNoResults)];
    /// <summary>Recently used</summary>
    public string SearchRecent => this[nameof(SearchRecent)];
    /// <summary>Actions</summary>
    public string SearchActions => this[nameof(SearchActions)];
    /// <summary>Back</summary>
    public string Back => this[nameof(Back)];
    /// <summary>Automatic</summary>
    public string Automatic => this[nameof(Automatic)];
    /// <summary>No Color</summary>
    public string NoColor => this[nameof(NoColor)];
    /// <summary>More Colors...</summary>
    public string MoreColors => this[nameof(MoreColors)];
    /// <summary>Theme Colors</summary>
    public string ThemeColors => this[nameof(ThemeColors)];
    /// <summary>Standard Colors</summary>
    public string StandardColors => this[nameof(StandardColors)];
    /// <summary>Recent Colors</summary>
    public string RecentColors => this[nameof(RecentColors)];
    /// <summary>Scroll left</summary>
    public string ScrollLeft => this[nameof(ScrollLeft)];
    /// <summary>Scroll right</summary>
    public string ScrollRight => this[nameof(ScrollRight)];
    /// <summary>Row up</summary>
    public string GalleryUp => this[nameof(GalleryUp)];
    /// <summary>Row down</summary>
    public string GalleryDown => this[nameof(GalleryDown)];
    /// <summary>More</summary>
    public string GalleryMore => this[nameof(GalleryMore)];
    /// <summary>Filter</summary>
    public string GalleryFilter => this[nameof(GalleryFilter)];
    /// <summary>{0} Settings</summary>
    public string DialogLauncher => this[nameof(DialogLauncher)];
    /// <summary>{0}x{1} Table</summary>
    public string TablePickerFormat => this[nameof(TablePickerFormat)];
    /// <summary>Insert Table</summary>
    public string InsertTable => this[nameof(InsertTable)];
    /// <summary>OK</summary>
    public string Ok => this[nameof(Ok)];
    /// <summary>Cancel</summary>
    public string Cancel => this[nameof(Cancel)];
    /// <summary>Reset</summary>
    public string Reset => this[nameof(Reset)];
    /// <summary>New Tab</summary>
    public string NewTab => this[nameof(NewTab)];
    /// <summary>New Group</summary>
    public string NewGroup => this[nameof(NewGroup)];
    /// <summary>Rename...</summary>
    public string Rename => this[nameof(Rename)];
    /// <summary>Add &gt;&gt;</summary>
    public string Add => this[nameof(Add)];
    /// <summary>&lt;&lt; Remove</summary>
    public string Remove => this[nameof(Remove)];
    /// <summary>Move Up</summary>
    public string MoveUp => this[nameof(MoveUp)];
    /// <summary>Move Down</summary>
    public string MoveDown => this[nameof(MoveDown)];
    /// <summary>Import/Export</summary>
    public string ImportExport => this[nameof(ImportExport)];
    /// <summary>Choose commands</summary>
    public string ChooseCommands => this[nameof(ChooseCommands)];
    /// <summary>Main Tabs</summary>
    public string MainTabs => this[nameof(MainTabs)];
    /// <summary>Quick Access Toolbar</summary>
    public string QuickAccessToolbar => this[nameof(QuickAccessToolbar)];
    /// <summary>(Custom)</summary>
    public string CustomGroupSuffix => this[nameof(CustomGroupSuffix)];
    /// <summary>Press a key to choose a command. Press Esc to go back.</summary>
    public string KeyTipsHint => this[nameof(KeyTipsHint)];
    /// <summary>Close</summary>
    public string Close => this[nameof(Close)];
    /// <summary>Account</summary>
    public string Account => this[nameof(Account)];
    /// <summary>Options</summary>
    public string Options => this[nameof(Options)];
    /// <summary>Type a command</summary>
    public string CommandPalette => this[nameof(CommandPalette)];
}
