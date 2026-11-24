export const STATS_PATTERNS=[
[/^Last personal snapshot: (.+?)\. One PC and one selected app profile are authoritative, preventing duplicated histories across PCs\. Website starting total \((.*?)\) is not added again\. Manual, imported and teammate contributions do not enter leaderboard rankings through personal sync\.$/,{
 es:'Última instantánea personal: $1. Un solo PC y un perfil seleccionado de la aplicación actúan como fuente de referencia para evitar historiales duplicados entre equipos. El total inicial del sitio ($2) no se vuelve a sumar. Las contribuciones manuales, importadas y de compañeros no entran en la clasificación mediante la sincronización personal.',
 pt:'Último snapshot pessoal: $1. Um único PC e um perfil selecionado do aplicativo são a fonte de referência, evitando históricos duplicados entre PCs. O total inicial do site ($2) não é somado novamente. As contribuições manuais, importadas e dos colegas de equipe não entram no ranking pela sincronização pessoal.',
 ru:'Последний личный снимок: $1. Источником истины служат один ПК и один выбранный профиль приложения, что предотвращает дублирование истории между ПК. Начальное значение сайта ($2) повторно не прибавляется. Ручные добавления, импортированные данные и вклад товарищей по команде не попадают в таблицу лидеров через личную синхронизацию.',
 fr:'Dernier instantané personnel : $1. Un seul PC et un profil sélectionné dans l’application font référence, ce qui évite les historiques en double entre PC. Le total initial du site ($2) n’est pas ajouté une seconde fois. Les contributions manuelles, importées et des coéquipiers ne sont pas intégrées au classement par la synchronisation personnelle.',
 de:'Letzte persönliche Momentaufnahme: $1. Ein PC und ein ausgewähltes App-Profil dienen als maßgebliche Quelle, um doppelte Verläufe auf mehreren PCs zu verhindern. Der Website-Anfangsgesamtwert ($2) wird nicht erneut addiert. Manuelle und importierte Beiträge sowie Beiträge von Teamkollegen gelangen nicht über die persönliche Synchronisierung in die Bestenliste.',
 it:'Ultima istantanea personale: $1. Un solo PC e un profilo selezionato dell’app costituiscono la fonte di riferimento, evitando cronologie duplicate tra PC. Il totale iniziale del sito ($2) non viene aggiunto nuovamente. I contributi manuali, importati e dei compagni non entrano nella classifica tramite la sincronizzazione personale.',
 pl:'Ostatnia migawka osobista: $1. Jeden komputer i jeden wybrany profil aplikacji stanowią źródło nadrzędne, co zapobiega duplikowaniu historii między komputerami. Wartość początkowa witryny ($2) nie jest dodawana ponownie. Wpisy ręczne, dane importowane i wkład członków drużyny nie trafiają do rankingu przez synchronizację osobistą.',
 tr:'Son kişisel anlık görüntü: $1. Bilgisayarlar arasında geçmişin yinelenmesini önlemek için tek bir bilgisayar ve seçili tek bir uygulama profili esas alınır. Sitenin başlangıç toplamı ($2) yeniden eklenmez. Elle eklenen, içe aktarılan ve takım arkadaşlarından gelen katkılar kişisel eşitleme yoluyla liderlik tablosuna dahil edilmez.',
 ja:'最新の個人スナップショット：$1。1台のPCと選択した1つのアプリのプロフィールを基準とし、PC間での履歴の重複を防ぎます。サイトの開始時の合計値（$2）は再加算しません。手動追加、インポート分、チームメイトの貢献分は、個人同期を通じてリーダーボードに加算されることはありません。'
}],
[/^Showing ([0-9]+) of ([0-9]+) matches\. Manual chest entries are included in the total breakdown, not counted as games\.$/,{
 es:'Mostrando $1 de $2 partidas. Las entradas manuales de cofres se incluyen en el desglose del total, pero no se cuentan como partidas.',
 pt:'Mostrando $1 de $2 partidas. As entradas manuais de baús são incluídas no detalhamento do total, mas não são contadas como partidas.',
 ru:'Показано $1 из $2 матчей. Ручные записи о сундуках включены в разбивку общего итога, но не считаются матчами.',
 fr:'Affichage de $1 parties sur $2. Les entrées manuelles de coffres sont incluses dans le détail du total, mais ne sont pas comptées comme des parties.',
 de:'$1 von $2 Matches werden angezeigt. Manuelle Truheneinträge sind in der Aufschlüsselung der Summe enthalten, zählen aber nicht als Matches.',
 it:'Visualizzate $1 partite su $2. Le voci manuali dei forzieri sono incluse nel dettaglio del totale, ma non vengono contate come partite.',
 pl:'Wyświetlono $1 z $2 meczów. Ręczne wpisy skrzyń są uwzględniane w zestawieniu sumy, ale nie są liczone jako mecze.',
 tr:'$2 maçtan $1 tanesi gösteriliyor. Elle girilen sandık kayıtları toplamın dökümüne dahildir, ancak maç olarak sayılmaz.',
 ja:'全$2試合中$1試合を表示しています。手動入力した宝箱は合計の内訳に含まれますが、試合数には含まれません。'
}],
[/^Week (\d+) ends (.+)$/,{
 es:"La semana $1 termina el $2",
 pt:"A semana $1 termina em $2",
 ru:"Неделя $1 заканчивается $2",
 fr:"La semaine $1 se termine le $2",
 de:"Woche $1 endet am $2",
 it:"La settimana $1 finisce il $2",
 pl:"Tydzień $1 kończy się $2",
 tr:"$1. hafta $2 biter",
 ja:"第$1週は$2に終了"
}],
[/^Week (\d+)$/,{
 es:"Semana $1",
 pt:"Semana $1",
 ru:"Неделя $1",
 fr:"Semaine $1",
 de:"Woche $1",
 it:"Settimana $1",
 pl:"Tydzień $1",
 tr:"Hafta $1",
 ja:"第$1週"
}],
[/^(\d+\/\d+) found$/,{
 es:"$1 encontrados",
 pt:"$1 encontrados",
 ru:"Найдено $1",
 fr:"$1 trouvés",
 de:"$1 gefunden",
 it:"$1 trovati",
 pl:"Znalezione: $1",
 tr:"$1 bulundu",
 ja:"$1 発見"
}],
[/^Starts (.+)$/,{
 es:"Empieza el $1",
 pt:"Começa em $1",
 ru:"Начало $1",
 fr:"Débute le $1",
 de:"Beginnt am $1",
 it:"Inizia il $1",
 pl:"Start $1",
 tr:"$1 başlar",
 ja:"$1開始"
}]
];
