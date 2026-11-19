namespace ChestCounter;

internal static class WeeklyChanceText
{
    private static readonly string[] Codes = { "es", "pt", "ru", "fr", "de", "it", "pl", "tr", "ja" };
    private static readonly Dictionary<string, string[]> Texts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Weekly Gold Outfit"] = new[] { "Atuendo dorado semanal", "Traje dourado semanal", "Недельный золотой костюм", "Tenue dorée hebdomadaire", "Wöchentliches Goldoutfit", "Outfit dorato settimanale", "Cotygodniowy złoty strój", "Haftalık altın kıyafet", "週替わりのゴールドスキン" },
        ["Pirate Shop Access"] = new[] { "Acceso a la tienda pirata", "Acesso à loja pirata", "Доступ в пиратский магазин", "Accès à la boutique pirate", "Zugang zum Piratenshop", "Accesso al negozio pirata", "Dostęp do sklepu pirackiego", "Korsan dükkânı erişimi", "海賊ショップへのアクセス" },
        ["Pirate Shop Access (all time)"] = new[] { "Acceso a la tienda pirata (histórico)", "Acesso à loja pirata (todo o período)", "Доступ в пиратский магазин (за всё время)", "Accès à la boutique pirate (depuis le début)", "Zugang zum Piratenshop (gesamter Zeitraum)", "Accesso al negozio pirata (tutto il periodo)", "Dostęp do sklepu pirackiego (cały okres)", "Korsan dükkânı erişimi (tüm zamanlar)", "海賊ショップへのアクセス（全期間）" },
        ["Weekly Vehicle & Decal"] = new[] { "Vehículo y calcomanía semanales", "Veículo e decalque semanais", "Недельный транспорт и наклейка", "Véhicule et décalque hebdomadaires", "Wöchentliches Fahrzeug und Decal", "Veicolo e decalcomania settimanali", "Cotygodniowy pojazd i naklejka", "Haftalık araç ve çıkartma", "週替わりの乗り物とデカール" },
        ["Weekly Pickaxe or Back Bling"] = new[] { "Pico o accesorio de espalda semanal", "Picareta ou acessório para as costas semanal", "Недельная кирка или украшение на спину", "Pioche ou accessoire de dos hebdomadaire", "Wöchentliche Spitzhacke oder Rückenaccessoire", "Piccone o dorso decorativo settimanale", "Cotygodniowy kilof lub plecak", "Haftalık kazma veya sırt süsü", "週替わりのツルハシまたはバックブリング" },
        ["Weekly Jam Track"] = new[] { "Pista de improvisación semanal", "Música de Jam semanal", "Недельный джем-трек", "Piste musicale hebdomadaire", "Wöchentlicher Jam-Track", "Traccia Jam settimanale", "Cotygodniowy utwór jamowy", "Haftalık Jam parçası", "週替わりのジャムトラック" },
        ["Weekly Loading Screen"] = new[] { "Pantalla de carga semanal", "Tela de carregamento semanal", "Недельный экран загрузки", "Écran de chargement hebdomadaire", "Wöchentlicher Ladebildschirm", "Schermata di caricamento settimanale", "Cotygodniowy ekran ładowania", "Haftalık yükleme ekranı", "週替わりのロード画面" },
        ["Gizmo (Any)"] = new[] { "Artilugio (cualquiera)", "Dispositivo (qualquer)", "Гизмо (любое)", "Gadget (tous types)", "Gizmo (beliebig)", "Dispositivo (qualsiasi)", "Gadżet (dowolny)", "Aygıt (herhangi biri)", "ギズモ（すべて）" },
        ["NEXT CLOSEST"] = new[] { "SIGUIENTE MÁS CERCANO", "PRÓXIMO MAIS PERTO", "БЛИЖАЙШАЯ НАГРАДА", "PROCHAINE LA PLUS PROCHE", "NÄCHSTE BELOHNUNG", "PROSSIMO PIÙ VICINO", "NAJBLIŻSZA NAGRODA", "SIRADAKİ EN YAKIN", "次に近い報酬" },
        ["YOUR PICK"] = new[] { "TU ELECCIÓN", "SUA ESCOLHA", "ВАШ ВЫБОР", "VOTRE CHOIX", "DEINE WAHL", "LA TUA SCELTA", "TWÓJ WYBÓR", "SEÇİMİNİZ", "あなたの選択" },
        ["cumulative chance this week"] = new[] { "probabilidad acumulada esta semana", "chance acumulada nesta semana", "накопленный шанс за неделю", "probabilité cumulée cette semaine", "kumulierte Chance diese Woche", "probabilità cumulativa questa settimana", "skumulowana szansa w tym tygodniu", "bu haftaki birikimli olasılık", "今週の累積確率" },
        ["cumulative chance all time"] = new[] { "probabilidad acumulada histórica", "chance acumulada em todo o período", "накопленный шанс за всё время", "probabilité cumulée depuis le début", "kumulierte Chance im gesamten Zeitraum", "probabilità cumulativa di tutto il periodo", "skumulowana szansa z całego okresu", "tüm zamanların birikimli olasılığı", "全期間の累積確率" },
        ["Past the 50% mark"] = new[] { "Por encima del 50%", "Acima de 50%", "Выше отметки 50%", "Au-delà des 50 %", "Über der 50-%-Marke", "Oltre il 50%", "Powyżej 50%", "%50 eşiği aşıldı", "50%を超えています" },
        ["IF YOU OPEN THIS MANY CHESTS"] = new[] { "SI ABRES ESTA CANTIDAD DE COFRES", "SE VOCÊ ABRIR ESTA QUANTIDADE DE BAÚS", "ЕСЛИ ОТКРЫТЬ СТОЛЬКО СУНДУКОВ", "SI VOUS OUVREZ CE NOMBRE DE COFFRES", "WENN DU SO VIELE TRUHEN ÖFFNEST", "SE APRI QUESTO NUMERO DI FORZIERI", "JEŚLI OTWORZYSZ TYLE SKRZYŃ", "BU KADAR SANDIK AÇARSANIZ", "この数の宝箱を開けた場合" },
        ["NORMAL RATE"] = new[] { "TASA NORMAL", "TAXA NORMAL", "ОБЫЧНАЯ ВЕРОЯТНОСТЬ", "TAUX NORMAL", "NORMALE RATE", "PROBABILITÀ NORMALE", "ZWYKŁA SZANSA", "NORMAL ORAN", "通常確率" },
        ["Boosts not announced yet"] = new[] { "Refuerzos aún no anunciados", "Bônus ainda não anunciados", "Бонусы ещё не объявлены", "Bonus pas encore annoncés", "Boosts noch nicht angekündigt", "Potenziamenti non ancora annunciati", "Bonusy nie zostały jeszcze ogłoszone", "Bonuslar henüz açıklanmadı", "ブーストは未発表です" },
        ["No Treasure Hour boost for this prize"] = new[] { "Sin refuerzo de Hora del Tesoro para este premio", "Sem bônus da Hora do Tesouro para este prêmio", "Для этой награды нет бонуса Часа сокровищ", "Aucun bonus d’Heure du trésor pour cette récompense", "Kein Schatzstunden-Boost für diese Belohnung", "Nessun bonus dell’Ora del Tesoro per questo premio", "Brak bonusu Godziny Skarbów dla tej nagrody", "Bu ödül için Hazine Saati bonusu yok", "この報酬にはトレジャーアワーのブーストがありません" },
    };

    public static string T(string source)
    {
        int index = Array.IndexOf(Codes, L.Code);
        if (index < 0) return source;
        if (!Texts.TryGetValue(source, out var row)) return WeeklyChanceMore.Lookup(source) ?? source;
        bool upper = source.Any(char.IsLetter) && source == source.ToUpperInvariant();
        return upper ? row[index].ToUpper(L.Culture) : row[index];
    }
}
