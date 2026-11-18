import { $, api } from '/app.js';
import { init } from '/i18n.js';
const languages=['en','es','pt','ru','fr','de','it','pl','tr','ja'];
const messages={
link:['Link this PC','Vincular este PC','Vincular este PC','Привязать этот ПК','Lier ce PC','Diesen PC verknüpfen','Collega questo PC','Połącz ten komputer','Bu bilgisayarı bağla','このPCを連携'],
warning:['Only approve this if you just pressed the link button in the Chest Counter app on your own computer.','Aprueba esta solicitud solo si acabas de pulsar el botón de vinculación en la aplicación Chest Counter de tu propio ordenador.','Só aprove esta solicitação se você acabou de pressionar o botão de vinculação no app Chest Counter no seu próprio computador.','Подтверждайте запрос, только если вы только что нажали кнопку привязки в приложении Chest Counter на своём компьютере.','N’approuvez cette demande que si vous venez de cliquer sur le bouton de liaison dans l’application Chest Counter sur votre propre ordinateur.','Bestätige dies nur, wenn du gerade auf deinem eigenen Computer in der Chest-Counter-App auf die Schaltfläche zum Verknüpfen geklickt hast.','Approva questa richiesta solo se hai appena premuto il pulsante di collegamento nell’app Chest Counter sul tuo computer.','Zatwierdź tę prośbę tylko wtedy, gdy przed chwilą naciśnięto przycisk łączenia w aplikacji Chest Counter na Twoim komputerze.','Yalnızca kendi bilgisayarındaki Chest Counter uygulamasında bağlantı düğmesine az önce bastıysan bu isteği onayla.','自分のパソコンのChest Counterアプリで連携ボタンを押した直後の場合にのみ、このリクエストを承認してください。'],
request:['Request','Solicitud','Solicitação','Запрос','Demande','Anfrage','Richiesta','Prośba','İstek','リクエスト'],
disclosure:['The app will upload match IDs, start times, modes and chest counts to this profile. It never uploads your Epic login, player name, teammates or replay files. You can unlink at any time.','La aplicación subirá a este perfil los identificadores de las partidas, las horas de inicio, los modos y los recuentos de cofres. Nunca sube tus credenciales de Epic, tu nombre de jugador, información sobre tus compañeros ni archivos de repetición. Puedes desvincular el PC en cualquier momento.','O app enviará os IDs das partidas, horários de início, modos e contagens de baús para este perfil. Ele nunca envia suas credenciais da Epic, seu nome de jogador, informações de colegas de equipe ou arquivos de replay. Você pode desvincular o PC a qualquer momento.','Приложение загрузит в этот профиль идентификаторы матчей, время начала, режимы и количество сундуков. Оно никогда не загружает ваши учётные данные Epic, имя игрока, сведения о напарниках или файлы повторов. Вы можете отменить привязку в любое время.','L’application enverra à ce profil les identifiants des parties, leurs heures de début, les modes et les nombres de coffres. Elle n’envoie jamais vos identifiants de connexion Epic, votre nom de joueur, des informations sur vos coéquipiers ou des fichiers de replay. Vous pouvez dissocier le PC à tout moment.','Die App lädt Match-IDs, Startzeiten, Modi und Truhenzahlen in dieses Profil hoch. Deine Epic-Zugangsdaten, dein Spielername, Informationen über Teamkollegen oder Replay-Dateien werden nie hochgeladen. Du kannst die Verknüpfung jederzeit aufheben.','L’app caricherà su questo profilo gli ID delle partite, gli orari di inizio, le modalità e i conteggi dei forzieri. Non carica mai le tue credenziali Epic, il nome del giocatore, informazioni sui compagni di squadra o file replay. Puoi scollegare il PC in qualsiasi momento.','Aplikacja prześle do tego profilu identyfikatory meczów, godziny rozpoczęcia, tryby i liczby skrzyń. Nigdy nie przesyła danych logowania Epic, nazwy gracza, informacji o członkach drużyny ani plików powtórek. Możesz odłączyć komputer w dowolnym momencie.','Uygulama, maç kimliklerini, başlangıç saatlerini, modları ve sandık sayılarını bu profile yükleyecek. Epic giriş bilgilerini, oyuncu adını, takım arkadaşlarına ait bilgileri veya tekrar dosyalarını asla yüklemez. Bilgisayar bağlantısını istediğin zaman kaldırabilirsin.','アプリはマッチID、開始時刻、モード、チェスト数をこのプロフィールにアップロードします。Epicのログイン情報、プレイヤー名、チームメイトの情報、リプレイファイルは一切アップロードしません。連携はいつでも解除できます。'],
missing:['Missing code','Falta el código','Código ausente','Код отсутствует','Code manquant','Code fehlt','Codice mancante','Brak kodu','Kod eksik','コードがありません'],
open:['Open this page from the Chest Counter app.','Abre esta página desde la aplicación Chest Counter.','Abra esta página pelo app Chest Counter.','Откройте эту страницу из приложения Chest Counter.','Ouvrez cette page depuis l’application Chest Counter.','Öffne diese Seite über die Chest-Counter-App.','Apri questa pagina dall’app Chest Counter.','Otwórz tę stronę z aplikacji Chest Counter.','Bu sayfayı Chest Counter uygulamasından aç.','Chest Counterアプリからこのページを開いてください。'],
done:['Done','Listo','Concluído','Готово','Terminé','Fertig','Fatto','Gotowe','Tamamlandı','完了'],
approved:['This PC is approved','Este PC está autorizado','Este PC foi aprovado','Этот ПК подтверждён','Ce PC est approuvé','Dieser PC wurde bestätigt','Questo PC è stato approvato','Ten komputer został zatwierdzony','Bu bilgisayar onaylandı','このPCが承認されました'],
return:['Return to Chest Counter. It finishes linking automatically.','Vuelve a Chest Counter. La vinculación se completará automáticamente.','Volte ao Chest Counter. A vinculação será concluída automaticamente.','Вернитесь в Chest Counter. Привязка завершится автоматически.','Retournez dans Chest Counter. La liaison se terminera automatiquement.','Kehre zu Chest Counter zurück. Die Verknüpfung wird automatisch abgeschlossen.','Torna a Chest Counter. Il collegamento verrà completato automaticamente.','Wróć do Chest Counter. Łączenie zakończy się automatycznie.','Chest Counter’a dön. Bağlantı otomatik olarak tamamlanacak.','Chest Counterに戻ってください。連携は自動的に完了します。'],
stats:['Open my stats','Abrir mis estadísticas','Abrir minhas estatísticas','Открыть мою статистику','Ouvrir mes statistiques','Meine Statistiken öffnen','Apri le mie statistiche','Otwórz moje statystyki','İstatistiklerimi aç','自分の統計を開く']
};
const code=(new URLSearchParams(location.search).get('code')||'').toUpperCase();
let approved=false,error='';
const column=()=>Math.max(0,languages.indexOf(document.documentElement.lang.slice(0,2).toLowerCase()));
const t=key=>messages[key][column()]||messages[key][0];
function paint(){
 document.querySelectorAll('[data-msg]').forEach(el=>{el.textContent=t(el.dataset.msg);});
 document.title=t('link')+' - Chest Counter';
 if(!approved){$('#code').textContent=code||t('missing');$('#err').textContent=error||(!code?t('open'):'');}
}
init();paint();
new MutationObserver(paint).observe(document.documentElement,{attributes:true,attributeFilter:['lang']});
if(code){
 try{await api('/api/profile',{method:'POST'});$('#approve').disabled=false;}
 catch(e){error=e.message;paint();}
 $('#approve').onclick=async()=>{
  try{
   $('#approve').disabled=true;
   await api('/api/link/approve',{method:'POST',body:{code}});
   approved=true;
   $('#box').innerHTML='<div class="lbl" data-msg="done"></div><h3 style="font-size:28px;margin:10px 0" data-msg="approved"></h3><p class="muted" data-msg="return"></p><a class="btn on" href="/stats.html" style="display:inline-block;text-decoration:none" data-msg="stats"></a>';
   paint();
  }catch(e){error=e.message;paint();$('#approve').disabled=false;}
 };
}
