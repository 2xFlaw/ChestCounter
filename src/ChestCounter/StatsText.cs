namespace ChestCounter;

// English-keyed text for the dashboard chest statistics.
internal static class StatsText
{
    private static readonly string[] Codes = { "es", "pt", "ru", "fr", "de", "it", "pl", "tr", "ja" };
    private static readonly Dictionary<string, string[]> Texts = new(StringComparer.Ordinal)
    {
        ["Just you"] = new[] { "Solo tú", "Só você", "Только вы", "Vous seul", "Nur du", "Solo tu", "Tylko ty", "Sadece siz", "自分のみ" },
        ["With teammates"] = new[] { "Con compañeros", "Com a equipe", "С союзниками", "Avec coéquipiers", "Mit Team", "Con compagni", "Z drużyną", "Takımla", "チームメイト込み" },
        ["By you"] = new[] { "Por ti", "Por você", "Ваши", "Par vous", "Von dir", "Da te", "Twoje", "Sizin", "自分" },
        ["{0:N0} opened · {1:N0} broken"] = new[] { "{0:N0} abiertos · {1:N0} rotos", "{0:N0} abertos · {1:N0} quebrados", "Открыто: {0:N0} · Сломано: {1:N0}", "{0:N0} ouverts · {1:N0} cassés", "{0:N0} geöffnet · {1:N0} zerstört", "{0:N0} aperti · {1:N0} distrutti", "Otwarte: {0:N0} · Zniszczone: {1:N0}", "{0:N0} açıldı · {1:N0} kırıldı", "開封 {0:N0} · 破壊 {1:N0}" },
        ["Teammate chests"] = new[] { "Cofres de compañeros", "Baús da equipe", "Сундуки союзников", "Coffres des coéquipiers", "Truhen des Teams", "Forzieri dei compagni", "Skrzynie drużyny", "Takım sandıkları", "チームメイトの宝箱" },
        ["seen nearby (a minimum)"] = new[] { "vistos cerca (como mínimo)", "vistos por perto (no mínimo)", "замечены рядом (минимум)", "vus à proximité (au minimum)", "in der Nähe gesehen (mindestens)", "visti nelle vicinanze (minimo)", "widziane w pobliżu (minimum)", "yakında görüldü (en az)", "近くで確認（最低値）" },
        ["Per match"] = new[] { "Por partida", "Por partida", "За матч", "Par partie", "Pro Match", "Per partita", "Na mecz", "Maç başına", "1マッチあたり" },
        ["average"] = new[] { "media", "média", "в среднем", "moyenne", "Durchschnitt", "media", "średnio", "ortalama", "平均" },
        ["Best match"] = new[] { "Mejor partida", "Melhor partida", "Лучший матч", "Meilleure partie", "Bestes Match", "Migliore partita", "Najlepszy mecz", "En iyi maç", "最高のマッチ" },
        ["Per minute"] = new[] { "Por minuto", "Por minuto", "В минуту", "Par minute", "Pro Minute", "Al minuto", "Na minutę", "Dakika başına", "1分あたり" },
        ["of match time"] = new[] { "de tiempo de partida", "de tempo de partida", "времени матча", "de temps de partie", "Matchzeit", "di tempo di gioco", "czasu meczu", "maç süresi", "試合時間あたり" },
        ["Chests per match"] = new[] { "Cofres por partida", "Baús por partida", "Сундуки за матч", "Coffres par partie", "Truhen pro Match", "Forzieri per partita", "Skrzynie na mecz", "Maç başına sandık", "マッチごとの宝箱" },
        ["Chests per day"] = new[] { "Cofres por día", "Baús por dia", "Сундуки за день", "Coffres par jour", "Truhen pro Tag", "Forzieri al giorno", "Skrzynie dziennie", "Günlük sandık", "日ごとの宝箱" },
        ["Per day"] = new[] { "Por día", "Por dia", "За день", "Par jour", "Pro Tag", "Al giorno", "Dziennie", "Günlük", "1日あたり" },
        ["No matches yet. They appear here once a replay has been analysed."] = new[] { "Aún no hay partidas. Aparecerán aquí cuando se analice una repetición.", "Ainda não há partidas. Elas aparecem aqui depois que um replay for analisado.", "Матчей пока нет. Они появятся здесь после анализа повтора.", "Aucune partie pour l’instant. Elles apparaissent ici après l’analyse d’un replay.", "Noch keine Matches. Sie erscheinen hier, sobald eine Wiederholung analysiert wurde.", "Ancora nessuna partita. Appariranno qui dopo l’analisi di un replay.", "Brak meczów. Pojawią się tu po przeanalizowaniu powtórki.", "Henüz maç yok. Bir tekrar analiz edildiğinde burada görünür.", "まだマッチがありません。リプレイが解析されるとここに表示されます。" },
        ["Oldest to newest. The line marks your average of {0:0.0} per match."] = new[] { "De más antigua a más reciente. La línea marca tu media de {0:0.0} por partida.", "Da mais antiga para a mais recente. A linha marca sua média de {0:0.0} por partida.", "От старых к новым. Линия — ваше среднее: {0:0.0} за матч.", "De la plus ancienne à la plus récente. La ligne indique votre moyenne de {0:0.0} par partie.", "Von alt nach neu. Die Linie zeigt deinen Schnitt von {0:0.0} pro Match.", "Dalla più vecchia alla più recente. La linea indica la tua media di {0:0.0} per partita.", "Od najstarszych do najnowszych. Linia to twoja średnia: {0:0.0} na mecz.", "Eskiden yeniye. Çizgi maç başına {0:0.0} ortalamanızı gösterir.", "古い順。線は1マッチあたりの平均 {0:0.0} を示します。" },
        ["Oldest to newest. The line marks your average of {0:0.0} per day."] = new[] { "De más antiguo a más reciente. La línea marca tu media de {0:0.0} por día.", "Do mais antigo para o mais recente. A linha marca sua média de {0:0.0} por dia.", "От старых к новым. Линия — ваше среднее: {0:0.0} за день.", "Du plus ancien au plus récent. La ligne indique votre moyenne de {0:0.0} par jour.", "Von alt nach neu. Die Linie zeigt deinen Schnitt von {0:0.0} pro Tag.", "Dal più vecchio al più recente. La linea indica la tua media di {0:0.0} al giorno.", "Od najstarszych do najnowszych. Linia to twoja średnia: {0:0.0} dziennie.", "Eskiden yeniye. Çizgi günlük {0:0.0} ortalamanızı gösterir.", "古い順。線は1日あたりの平均 {0:0.0} を示します。" },
        ["You"] = new[] { "Tú", "Você", "Вы", "Vous", "Du", "Tu", "Ty", "Siz", "自分" },
        ["Teammates"] = new[] { "Compañeros", "Equipe", "Союзники", "Coéquipiers", "Team", "Compagni", "Drużyna", "Takım arkadaşları", "チームメイト" },
        ["Showing the latest {0:N0} of {1:N0}"] = new[] { "Mostrando las últimas {0:N0} de {1:N0}", "Mostrando as últimas {0:N0} de {1:N0}", "Показаны последние {0:N0} из {1:N0}", "Affichage des {0:N0} dernières sur {1:N0}", "Die letzten {0:N0} von {1:N0}", "Ultime {0:N0} di {1:N0}", "Ostatnie {0:N0} z {1:N0}", "Son {0:N0} / {1:N0} gösteriliyor", "最新 {0:N0} / {1:N0} 件を表示" },
    };

    public static string T(string source)
    {
        int index = Array.IndexOf(Codes, L.Code);
        return index >= 0 && Texts.TryGetValue(source, out var row) ? row[index] : source;
    }

    public static string F(string source, params object?[] args) => string.Format(L.Culture, T(source), args);
}
