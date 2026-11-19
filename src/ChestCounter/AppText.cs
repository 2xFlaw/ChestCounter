namespace ChestCounter;

// Supplemental English-keyed UI text; language selection remains owned by L.
internal static class AppText
{
    private static readonly string[] Codes = { "es", "pt", "ru", "fr", "de", "it", "pl", "tr", "ja" };
    private static readonly Dictionary<string, string[]> Texts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dashboard"] = new[] { "Panel", "Painel", "Обзор", "Tableau de bord", "Übersicht", "Panoramica", "Panel", "Gösterge paneli", "ダッシュボード" },
        ["Match history"] = new[] { "Historial de partidas", "Histórico de partidas", "История матчей", "Historique des parties", "Matchverlauf", "Cronologia partite", "Historia meczów", "Maç geçmişi", "マッチ履歴" },
        ["Weekly chances"] = new[] { "Probabilidades semanales", "Chances semanais", "Недельные шансы", "Chances hebdomadaires", "Wöchentliche Chancen", "Probabilità settimanali", "Szanse tygodniowe", "Haftalık şanslar", "毎週の確率" },
        ["Weekly rewards"] = new[] { "Recompensas semanales", "Recompensas semanais", "Недельные награды", "Récompenses hebdomadaires", "Wöchentliche Belohnungen", "Premi settimanali", "Nagrody tygodniowe", "Haftalık ödüller", "毎週の報酬" },
        ["Overlay"] = new[] { "Superposición", "Sobreposição", "Оверлей", "Superposition", "Overlay", "Sovrapposizione", "Nakładka", "Yer paylaşımı", "オーバーレイ" },
        ["Settings"] = new[] { "Ajustes", "Configurações", "Настройки", "Paramètres", "Einstellungen", "Impostazioni", "Ustawienia", "Ayarlar", "設定" },
        ["Performance"] = new[] { "Rendimiento", "Desempenho", "Производительность", "Performances", "Leistung", "Prestazioni", "Wydajność", "Performans", "パフォーマンス" },
        ["Your dashboard"] = new[] { "Tu panel", "Seu painel", "Ваш обзор", "Votre tableau de bord", "Deine Übersicht", "La tua panoramica", "Twój panel", "Gösterge paneliniz", "あなたのダッシュボード" },
        ["Total personal / configured"] = new[] { "Total personal / configurado", "Total pessoal / configurado", "Личный / настроенный итог", "Total personnel / configuré", "Persönliche / konfigurierte Summe", "Totale personale / configurato", "Suma osobista / skonfigurowana", "Kişisel / yapılandırılmış toplam", "個人・設定込みの合計" },
        ["Current / previous match"] = new[] { "Partida actual / anterior", "Partida atual / anterior", "Текущий / предыдущий матч", "Partie actuelle / précédente", "Aktuelles / vorheriges Match", "Partita attuale / precedente", "Bieżący / poprzedni mecz", "Mevcut / önceki maç", "現在・前回のマッチ" },
        ["Today"] = new[] { "Hoy", "Hoje", "Сегодня", "Aujourd’hui", "Heute", "Oggi", "Dzisiaj", "Bugün", "今日" },
        ["Average per match"] = new[] { "Media por partida", "Média por partida", "В среднем за матч", "Moyenne par partie", "Durchschnitt pro Match", "Media per partita", "Średnia na mecz", "Maç başına ortalama", "1マッチあたりの平均" },
        ["Best match"] = new[] { "Mejor partida", "Melhor partida", "Лучший матч", "Meilleure partie", "Bestes Match", "Migliore partita", "Najlepszy mecz", "En iyi maç", "最高のマッチ" },
        ["Treasures"] = new[] { "Tesoros", "Tesouros", "Сокровища", "Trésors", "Schätze", "Tesori", "Skarby", "Hazineler", "お宝" },
        ["LAST 7 DAYS"] = new[] { "ÚLTIMOS 7 DÍAS", "ÚLTIMOS 7 DIAS", "ПОСЛЕДНИЕ 7 ДНЕЙ", "7 DERNIERS JOURS", "LETZTE 7 TAGE", "ULTIMI 7 GIORNI", "OSTATNIE 7 DNI", "SON 7 GÜN", "過去7日間" },
        ["Refresh"] = new[] { "Actualizar", "Atualizar", "Обновить", "Actualiser", "Aktualisieren", "Aggiorna", "Odśwież", "Yenile", "更新" },
    };

    public static string T(string source)
    {
        int index = Array.IndexOf(Codes, L.Code);
        if (index < 0 || !Texts.TryGetValue(source, out var row)) return source;
        var translated = row[index];
        bool upper = source.Any(char.IsLetter) && source == source.ToUpperInvariant();
        return upper ? translated.ToUpper(L.Culture) : translated;
    }
}
