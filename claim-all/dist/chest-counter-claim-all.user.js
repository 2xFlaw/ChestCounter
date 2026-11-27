// ==UserScript==
// @name         Chest Counter: Pirate Shop Claim All
// @name:es      Chest Counter: Reclamar todo de la Tienda pirata
// @name:pt-BR   Chest Counter: Resgatar tudo da Loja Pirata
// @name:ru      Chest Counter: забрать всё в Пиратском магазине
// @name:fr      Chest Counter : Tout récupérer dans la Boutique pirate
// @name:de      Chest Counter: Piratenshop alles abholen
// @name:it      Chest Counter: Riscatta tutto nel Negozio pirata
// @name:pl      Chest Counter: Odbierz wszystko w Sklepie pirackim
// @name:tr      Chest Counter: Korsan Dükkânı Hepsini al
// @name:ja      Chest Counter: 海賊ショップまとめて入手
// @namespace    https://chestcounter.io
// @version      2.6.1
// @description  Already own the Pirate Shop Access Token? Claim the whole Fortnite Item Shop with one button. Never spends V-Bucks.
// @author       2xFlaw
// @license      MIT
// @homepageURL  https://chestcounter.io/claim
// @supportURL   https://github.com/2xFlaw/ChestCounter/issues
// @icon         https://raw.githubusercontent.com/2xFlaw/ChestCounter/main/claim-all/extension/icons/icon48.png
// @match        https://www.fortnite.com/*
// @grant        none
// @inject-into  page
// @run-at       document-idle
// @noframes
// @updateURL    https://raw.githubusercontent.com/2xFlaw/ChestCounter/main/claim-all/dist/chest-counter-claim-all.user.js
// @downloadURL  https://raw.githubusercontent.com/2xFlaw/ChestCounter/main/claim-all/dist/chest-counter-claim-all.user.js
// ==/UserScript==
// Chest Counter Claim All v2.6.1 (userscript). Built from claim-all/src, do not edit by hand.
(() => {
const CC_VERSION = "2.6.1";
const CC_PLATFORM = "userscript";
const CC_STRINGS = {"en":{"brand":"Claim All","claimAll":"Claim all free","claiming":"Claiming…","allClaimed":"All claimed","tipSignin":"Sign in to claim","tipNoToken":"The Pirate Shop isn’t active on this account","tipReady":{"one":"Claim {n} free item. Never spends V-Bucks","other":"Claim {n} free items. Never spends V-Bucks"},"tipNone":"You own every free item in today’s shop","reading":"Reading the shop…","demoBadge":"Demo: nothing is sent to Epic","reviewTitle":{"one":"{n} free item","other":"{n} free items"},"reviewWorth":"About {v} V-Bucks of items","reviewUntil":"Pirate Shop active until {d}","reviewPick":"Choose what to claim","selectAll":"All","selectNone":"None","cat_sidekicks":"Sidekicks","cat_bundles":"Bundles","cat_outfits":"Outfits","cat_emotes":"Emotes","cat_pickaxes":"Pickaxes","cat_backblings":"Back Blings","cat_gliders":"Gliders","cat_wraps":"Wraps","cat_contrails":"Contrails","cat_jam":"Jam Tracks","cat_instruments":"Instruments","cat_cars":"Car cosmetics","cat_kicks":"Kicks","cat_other":"Other","skTitle":"Pick your one free sidekick","skText":"The Pirate Shop only gives you one free sidekick you don’t already own, so choose it here. Anything else with a sidekick in it is left out, and you can claim those items separately.","skBundle":"Bundle with a sidekick","skSingle":"Sidekick","skNone":"Not now","skNoneSub":"Keep my free sidekick for later","safety":"Never spends V-Bucks. Every claim is sent with a price of 0, and it stops straight away if your balance drops.","balanceNow":"Balance: {b} V-Bucks","quickToggle":"Skip this screen next time","start":{"one":"Claim {n} item","other":"Claim {n} items"},"cancel":"Cancel","stop":"Stop","tagBundle":"Bundle","stSidekick":"Claiming your sidekick…","stBundles":{"one":"Opening {n} bundle…","other":"Opening {n} bundles…"},"stChecking":"Checking what the bundles gave you…","stSingles":{"one":"Grabbing {n} more item…","other":"Grabbing {n} more items…"},"stItems":{"one":"Grabbing {n} item…","other":"Grabbing {n} items…"},"stStopping":"Stopping after the current claims…","doneTitle":"All yours","stoppedTitle":"Stopped","nothingClaimed":"Nothing claimed","nothingTitle":"Nothing left to claim","nothingText":"You already own every free item in today’s shop. New items arrive when the shop refreshes at 00:00 UTC.","doneText":{"one":"{n} cosmetic added to your locker in {t}.","other":"{n} cosmetics added to your locker in {t}."},"stoppedText":{"one":"Got {n} item before stopping.","other":"Got {n} items before stopping."},"secs":"{s} seconds","mins":"{m} min {s} s","chipVbOk":"V-Bucks untouched: {b}","chipVbBad":"V-Bucks changed: {a} → {b}","chipWorth":"About {v} V-Bucks of items","chipClaimed":{"one":"{n} claim","other":"{n} claims"},"chipBundle":{"one":"{n} came with bundles","other":"{n} came with bundles"},"chipSkipped":{"one":"{n} skipped","other":"{n} skipped"},"chipLeftOut":{"one":"{n} left out by you","other":"{n} left out by you"},"whySkipped":"Why some were skipped","leftOutList":"What you left out","notCovered":{"one":"{n} item the Pirate Shop doesn’t cover","other":"{n} items the Pirate Shop doesn’t cover"},"rmPack":"real-money pack","vbPrice":"{v} V-Bucks","rExcluded":"not part of the Pirate Shop","rNoLongerFree":"no longer free","rSidekickUsed":"your free sidekick is already used","rSidekick":"has a sidekick","rSlow":"Epic asked us to slow down. Try again in a minute","saveList":"Save list","retry":{"one":"Retry {n}","other":"Retry {n}"},"reviewAgain":"Claim the rest","done":"Done","close":"Close","tryAgain":"Try again","gotIt":"Got it","safetyTitle":"Safety stop","safetyText":"Your V-Bucks went from {a} to {b}, so Claim All stopped straight away. Check your balance and purchase history on Epic before you run it again.","snagTitle":"Hit a snag","snagText":"{msg}. Nothing was charged.","signinTitle":"Sign in first","signinText":"You’re not signed in on fortnite.com. Sign in with the account that has the Pirate Shop active, then try again.","signin":"Sign in","noTokenTitle":"The Pirate Shop isn’t active","noTokenText":"Items on this account still cost V-Bucks, so nothing was claimed. If you own the Pirate Shop Access Token, activate it in the in-game Fortnite Item Shop first. It lasts 14 days from activation.","notActiveTitle":"Token not activated yet","notActiveText":"You have the Pirate Shop Access Token, but it isn’t activated yet. Activate it in the in-game Fortnite Item Shop, then come back here. It lasts 14 days, so activate it when you’re ready. The 2026 token must be activated by 16 Dec, 00:00 GMT.","errShop":"Couldn’t read the Item Shop","errCsrf":"Couldn’t get a security token from Epic","errHttp":"Epic’s site answered with an error (HTTP {s})","errSafety":"Safety stop: your V-Bucks went from {a} to {b}","listHeader":"Pirate Shop claims on {d}","pTitle":"Pirate Shop Claim All","pOpen":"Open the Item Shop","pClaim":"Claim all free","pWaiting":{"one":"{n} free item waiting","other":"{n} free items waiting"},"pUntil":"Active until {d}","pNoShop":"Open fortnite.com/item-shop while signed in. The button appears next to Buy V-Bucks.","pSignin":"Sign in on fortnite.com to see your free items.","pNoToken":"The Pirate Shop isn’t active on this account yet.","pNotActive":"Token owned but not activated yet. Activate it in the in-game Item Shop.","pAllDone":"You own every free item in today’s shop.","pRunning":"Claiming…","pReminder":"Remind me when the shop refreshes","pReminderSub":"Daily at 00:00 UTC while your Pirate Shop is active","pQuick":"Skip the review screen","pLifetime":{"one":"{n} item claimed so far, about {v} V-Bucks","other":"{n} items claimed so far, about {v} V-Bucks"},"pUpdate":"Version {v} is available","pUpdateGet":"Get it","pFooter":"Made by 2xFlaw · Chest Counter","nTitle":"Item Shop refreshed","nText":"New items are free while your Pirate Shop is active. Click to claim them."},"es":{"brand":"Reclamar todo","claimAll":"Reclamar todo gratis","claiming":"Reclamando…","allClaimed":"Todo reclamado","tipSignin":"Inicia sesión para reclamar","tipNoToken":"La tienda pirata no está activa en esta cuenta","tipReady":{"one":"Reclama {n} objeto gratis. Nunca gasta paVos","other":"Reclama {n} objetos gratis. Nunca gasta paVos"},"tipNone":"Ya tienes todos los objetos gratis de la tienda de hoy","reading":"Leyendo la tienda…","demoBadge":"Demo: no se envía nada a Epic","reviewTitle":{"one":"{n} objeto gratis","other":"{n} objetos gratis"},"reviewWorth":"Unos {v} paVos en objetos","reviewUntil":"Tienda pirata activa hasta {d}","reviewPick":"Elige qué reclamar","selectAll":"Todo","selectNone":"Nada","cat_sidekicks":"Compañeros","cat_bundles":"Lotes","cat_outfits":"Trajes","cat_emotes":"Gestos","cat_pickaxes":"Picos","cat_backblings":"Mochilas","cat_gliders":"Planeadores","cat_wraps":"Papeles","cat_contrails":"Estelas","cat_jam":"Pistas de improvisación","cat_instruments":"Instrumentos","cat_cars":"Cosméticos de coche","cat_kicks":"Zapatillas","cat_other":"Otros","skTitle":"Elige tu único compañero gratis","skText":"La tienda pirata solo te da un compañero gratis que aún no tengas, así que elígelo aquí. Todo lo demás que incluya un compañero se deja fuera, y puedes reclamar esos objetos por separado.","skBundle":"Lote con compañero","skSingle":"Compañero","skNone":"Ahora no","skNoneSub":"Guardar mi compañero gratis para más tarde","safety":"Nunca gasta paVos. Cada reclamación se envía con precio 0 y se detiene al instante si tu saldo baja.","balanceNow":"Saldo: {b} paVos","quickToggle":"Saltar esta pantalla la próxima vez","start":{"one":"Reclamar {n} objeto","other":"Reclamar {n} objetos"},"cancel":"Cancelar","stop":"Detener","tagBundle":"Lote","stSidekick":"Reclamando tu compañero…","stBundles":{"one":"Abriendo {n} lote…","other":"Abriendo {n} lotes…"},"stChecking":"Comprobando qué te han dado los lotes…","stSingles":{"one":"Reclamando {n} objeto más…","other":"Reclamando {n} objetos más…"},"stItems":{"one":"Reclamando {n} objeto…","other":"Reclamando {n} objetos…"},"stStopping":"Deteniendo tras las reclamaciones en curso…","doneTitle":"Todo tuyo","stoppedTitle":"Detenido","nothingClaimed":"No se reclamó nada","nothingTitle":"No queda nada por reclamar","nothingText":"Ya tienes todos los objetos gratis de la tienda de hoy. Llegan objetos nuevos cuando la tienda se renueva a las 00:00 UTC.","doneText":{"one":"{n} cosmético añadido a tu taquilla en {t}.","other":"{n} cosméticos añadidos a tu taquilla en {t}."},"stoppedText":{"one":"Conseguiste {n} objeto antes de detenerlo.","other":"Conseguiste {n} objetos antes de detenerlo."},"secs":"{s} segundos","mins":"{m} min {s} s","chipVbOk":"paVos intactos: {b}","chipVbBad":"Los paVos cambiaron: {a} → {b}","chipWorth":"Unos {v} paVos en objetos","chipClaimed":{"one":"{n} reclamación","other":"{n} reclamaciones"},"chipBundle":{"one":"{n} venía en lotes","other":"{n} venían en lotes"},"chipSkipped":{"one":"{n} omitido","other":"{n} omitidos"},"chipLeftOut":{"one":"{n} dejado fuera por ti","other":"{n} dejados fuera por ti"},"whySkipped":"Por qué se omitieron algunos","leftOutList":"Lo que dejaste fuera","notCovered":{"one":"{n} objeto que la tienda pirata no cubre","other":"{n} objetos que la tienda pirata no cubre"},"rmPack":"pack de dinero real","vbPrice":"{v} paVos","rExcluded":"no forma parte de la tienda pirata","rNoLongerFree":"ya no es gratis","rSidekickUsed":"ya usaste tu compañero gratis","rSidekick":"incluye un compañero","rSlow":"Epic nos pidió ir más despacio. Inténtalo de nuevo en un minuto","saveList":"Guardar lista","retry":{"one":"Reintentar {n}","other":"Reintentar {n}"},"reviewAgain":"Reclamar el resto","done":"Listo","close":"Cerrar","tryAgain":"Reintentar","gotIt":"Entendido","safetyTitle":"Parada de seguridad","safetyText":"Tus paVos pasaron de {a} a {b}, así que Reclamar todo se detuvo al instante. Revisa tu saldo y tu historial de compras en Epic antes de volver a usarlo.","snagTitle":"Algo salió mal","snagText":"{msg}. No se cobró nada.","signinTitle":"Inicia sesión primero","signinText":"No has iniciado sesión en fortnite.com. Inicia sesión con la cuenta que tiene la tienda pirata activa y vuelve a intentarlo.","signin":"Iniciar sesión","noTokenTitle":"La tienda pirata no está activa","noTokenText":"Los objetos de esta cuenta siguen costando paVos, así que no se reclamó nada. Si tienes la ficha de acceso a la tienda pirata, actívala primero en la tienda de objetos de Fortnite dentro del juego. Dura 14 días desde la activación.","notActiveTitle":"Ficha aún sin activar","notActiveText":"Tienes la ficha de acceso a la tienda pirata, pero aún no está activada. Actívala en la tienda de objetos de Fortnite dentro del juego y vuelve aquí. Dura 14 días, así que actívala cuando quieras. La ficha de 2026 debe activarse antes del 16 dic, 00:00 GMT.","errShop":"No se pudo leer la tienda de objetos","errCsrf":"No se pudo obtener un token de seguridad de Epic","errHttp":"El sitio de Epic respondió con un error (HTTP {s})","errSafety":"Parada de seguridad: tus paVos pasaron de {a} a {b}","listHeader":"Reclamaciones de la tienda pirata del {d}","pTitle":"Reclamar todo de la tienda pirata","pOpen":"Abrir la tienda de objetos","pClaim":"Reclamar todo gratis","pWaiting":{"one":"{n} objeto gratis esperando","other":"{n} objetos gratis esperando"},"pUntil":"Activa hasta {d}","pNoShop":"Abre fortnite.com/item-shop con la sesión iniciada. El botón aparece junto a Comprar paVos.","pSignin":"Inicia sesión en fortnite.com para ver tus objetos gratis.","pNoToken":"La tienda pirata aún no está activa en esta cuenta.","pNotActive":"Tienes la ficha, pero sin activar. Actívala en la tienda de objetos del juego.","pAllDone":"Ya tienes todos los objetos gratis de la tienda de hoy.","pRunning":"Reclamando…","pReminder":"Avisarme cuando se renueve la tienda","pReminderSub":"Cada día a las 00:00 UTC mientras tu tienda pirata esté activa","pQuick":"Saltar la pantalla de revisión","pLifetime":{"one":"{n} objeto reclamado hasta ahora, unos {v} paVos","other":"{n} objetos reclamados hasta ahora, unos {v} paVos"},"pUpdate":"La versión {v} está disponible","pUpdateGet":"Descargar","pFooter":"Hecho por 2xFlaw · Chest Counter","nTitle":"La tienda de objetos se ha renovado","nText":"Los objetos nuevos son gratis mientras tu tienda pirata esté activa. Haz clic para reclamarlos."},"pt":{"brand":"Resgatar tudo","claimAll":"Resgatar tudo grátis","claiming":"Resgatando…","allClaimed":"Tudo resgatado","tipSignin":"Entre para resgatar","tipNoToken":"A loja pirata não está ativa nesta conta","tipReady":{"one":"Resgate {n} item grátis. Nunca gasta V-Bucks","other":"Resgate {n} itens grátis. Nunca gasta V-Bucks"},"tipNone":"Você já tem todos os itens grátis da loja de hoje","reading":"Lendo a loja…","demoBadge":"Demo: nada é enviado à Epic","reviewTitle":{"one":"{n} item grátis","other":"{n} itens grátis"},"reviewWorth":"Cerca de {v} V-Bucks em itens","reviewUntil":"Loja pirata ativa até {d}","reviewPick":"Escolha o que resgatar","selectAll":"Tudo","selectNone":"Nada","cat_sidekicks":"Parceiros","cat_bundles":"Pacotes","cat_outfits":"Trajes","cat_emotes":"Gestos","cat_pickaxes":"Picaretas","cat_backblings":"Acessórios para as Costas","cat_gliders":"Asas-deltas","cat_wraps":"Envelopamentos","cat_contrails":"Rastros de fumaça","cat_jam":"Faixas de improviso","cat_instruments":"Instrumentos","cat_cars":"Cosméticos de carro","cat_kicks":"Tênis","cat_other":"Outros","skTitle":"Escolha seu único parceiro grátis","skText":"A loja pirata só dá um parceiro grátis que você ainda não tenha, então escolha aqui. Todo o resto que inclui um parceiro fica de fora, e você pode resgatar esses itens separadamente.","skBundle":"Pacote com parceiro","skSingle":"Parceiro","skNone":"Agora não","skNoneSub":"Guardar meu parceiro grátis para depois","safety":"Nunca gasta V-Bucks. Cada resgate é enviado com preço 0 e para na hora se o seu saldo cair.","balanceNow":"Saldo: {b} V-Bucks","quickToggle":"Pular esta tela da próxima vez","start":{"one":"Resgatar {n} item","other":"Resgatar {n} itens"},"cancel":"Cancelar","stop":"Parar","tagBundle":"Pacote","stSidekick":"Resgatando seu parceiro…","stBundles":{"one":"Abrindo {n} pacote…","other":"Abrindo {n} pacotes…"},"stChecking":"Conferindo o que os pacotes deram…","stSingles":{"one":"Pegando mais {n} item…","other":"Pegando mais {n} itens…"},"stItems":{"one":"Pegando {n} item…","other":"Pegando {n} itens…"},"stStopping":"Parando depois dos resgates em andamento…","doneTitle":"Tudo seu","stoppedTitle":"Parado","nothingClaimed":"Nada foi resgatado","nothingTitle":"Nada para resgatar","nothingText":"Você já tem todos os itens grátis da loja de hoje. Itens novos chegam quando a loja atualiza às 00:00 UTC.","doneText":{"one":"{n} cosmético adicionado ao seu armário em {t}.","other":"{n} cosméticos adicionados ao seu armário em {t}."},"stoppedText":{"one":"Você pegou {n} item antes de parar.","other":"Você pegou {n} itens antes de parar."},"secs":"{s} segundos","mins":"{m} min {s} s","chipVbOk":"V-Bucks intactos: {b}","chipVbBad":"Os V-Bucks mudaram: {a} → {b}","chipWorth":"Cerca de {v} V-Bucks em itens","chipClaimed":{"one":"{n} resgate","other":"{n} resgates"},"chipBundle":{"one":"{n} veio em pacotes","other":"{n} vieram em pacotes"},"chipSkipped":{"one":"{n} pulado","other":"{n} pulados"},"chipLeftOut":{"one":"{n} deixado de fora por você","other":"{n} deixados de fora por você"},"whySkipped":"Por que alguns foram pulados","leftOutList":"O que você deixou de fora","notCovered":{"one":"{n} item que a loja pirata não cobre","other":"{n} itens que a loja pirata não cobre"},"rmPack":"pacote de dinheiro real","vbPrice":"{v} V-Bucks","rExcluded":"não faz parte da loja pirata","rNoLongerFree":"não está mais grátis","rSidekickUsed":"seu parceiro grátis já foi usado","rSidekick":"inclui um parceiro","rSlow":"A Epic pediu para irmos mais devagar. Tente de novo em um minuto","saveList":"Salvar lista","retry":{"one":"Tentar {n} de novo","other":"Tentar {n} de novo"},"reviewAgain":"Resgatar o resto","done":"Concluir","close":"Fechar","tryAgain":"Tentar de novo","gotIt":"Entendi","safetyTitle":"Parada de segurança","safetyText":"Seus V-Bucks foram de {a} para {b}, então o Resgatar tudo parou na hora. Confira seu saldo e o histórico de compras na Epic antes de usar de novo.","snagTitle":"Algo deu errado","snagText":"{msg}. Nada foi cobrado.","signinTitle":"Entre primeiro","signinText":"Você não entrou no fortnite.com. Entre com a conta que tem a loja pirata ativa e tente de novo.","signin":"Entrar","noTokenTitle":"A loja pirata não está ativa","noTokenText":"Os itens desta conta ainda custam V-Bucks, então nada foi resgatado. Se você tem a ficha de acesso à loja pirata, ative-a primeiro na Loja de Itens do Fortnite dentro do jogo. Ela dura 14 dias a partir da ativação.","notActiveTitle":"Ficha ainda não ativada","notActiveText":"Você tem a ficha de acesso à loja pirata, mas ela ainda não foi ativada. Ative-a na Loja de Itens do Fortnite dentro do jogo e volte aqui. Ela dura 14 dias, então ative quando quiser. A ficha de 2026 precisa ser ativada até 16 de dez., 00:00 GMT.","errShop":"Não foi possível ler a Loja de Itens","errCsrf":"Não foi possível obter um token de segurança da Epic","errHttp":"O site da Epic respondeu com um erro (HTTP {s})","errSafety":"Parada de segurança: seus V-Bucks foram de {a} para {b}","listHeader":"Resgates da loja pirata em {d}","pTitle":"Resgatar tudo da loja pirata","pOpen":"Abrir a Loja de Itens","pClaim":"Resgatar tudo grátis","pWaiting":{"one":"{n} item grátis esperando","other":"{n} itens grátis esperando"},"pUntil":"Ativa até {d}","pNoShop":"Abra fortnite.com/item-shop com a conta conectada. O botão aparece ao lado de Comprar V-Bucks.","pSignin":"Entre no fortnite.com para ver seus itens grátis.","pNoToken":"A loja pirata ainda não está ativa nesta conta.","pNotActive":"Ficha obtida, mas não ativada. Ative-a na Loja de Itens do jogo.","pAllDone":"Você já tem todos os itens grátis da loja de hoje.","pRunning":"Resgatando…","pReminder":"Me avisar quando a loja atualizar","pReminderSub":"Todo dia às 00:00 UTC enquanto sua loja pirata estiver ativa","pQuick":"Pular a tela de revisão","pLifetime":{"one":"{n} item resgatado até agora, cerca de {v} V-Bucks","other":"{n} itens resgatados até agora, cerca de {v} V-Bucks"},"pUpdate":"A versão {v} está disponível","pUpdateGet":"Baixar","pFooter":"Feito por 2xFlaw · Chest Counter","nTitle":"A Loja de Itens atualizou","nText":"Os itens novos estão grátis enquanto sua loja pirata estiver ativa. Clique para resgatá-los."},"ru":{"brand":"Забрать всё","claimAll":"Забрать всё бесплатно","claiming":"Забираем…","allClaimed":"Всё получено","tipSignin":"Войдите, чтобы забрать","tipNoToken":"Пиратский магазин не активен на этой учётной записи","tipReady":{"one":"Забрать {n} бесплатный предмет. В-баксы не тратятся","few":"Забрать {n} бесплатных предмета. В-баксы не тратятся","many":"Забрать {n} бесплатных предметов. В-баксы не тратятся","other":"Забрать {n} бесплатного предмета. В-баксы не тратятся"},"tipNone":"У вас уже есть все бесплатные предметы сегодняшнего магазина","reading":"Читаем магазин…","demoBadge":"Демо: в Epic ничего не отправляется","reviewTitle":{"one":"{n} бесплатный предмет","few":"{n} бесплатных предмета","many":"{n} бесплатных предметов","other":"{n} бесплатного предмета"},"reviewWorth":"Предметов примерно на {v} В-баксов","reviewUntil":"Пиратский магазин активен до {d}","reviewPick":"Выберите, что забрать","selectAll":"Все","selectNone":"Ничего","cat_sidekicks":"Компаньоны","cat_bundles":"Наборы","cat_outfits":"Экипировка","cat_emotes":"Эмоции","cat_pickaxes":"Кирки","cat_backblings":"Украшения на спину","cat_gliders":"Дельтапланы","cat_wraps":"Обёртки","cat_contrails":"Шлейфы","cat_jam":"Джем-треки","cat_instruments":"Инструменты","cat_cars":"Предметы для машин","cat_kicks":"Кроссовки","cat_other":"Другое","skTitle":"Выберите своего единственного бесплатного компаньона","skText":"Пиратский магазин даёт только одного бесплатного компаньона, которого у вас ещё нет, поэтому выберите его здесь. Всё остальное с компаньоном не забирается, эти предметы можно получить отдельно.","skBundle":"Набор с компаньоном","skSingle":"Компаньон","skNone":"Не сейчас","skNoneSub":"Оставить бесплатного компаньона на потом","safety":"В-баксы никогда не тратятся. Каждый запрос отправляется с ценой 0, а при уменьшении баланса всё сразу останавливается.","balanceNow":"Баланс: {b} В-баксов","quickToggle":"Пропускать этот экран в следующий раз","start":{"one":"Забрать {n} предмет","few":"Забрать {n} предмета","many":"Забрать {n} предметов","other":"Забрать {n} предмета"},"cancel":"Отмена","stop":"Стоп","tagBundle":"Набор","stSidekick":"Забираем вашего компаньона…","stBundles":{"one":"Открываем {n} набор…","few":"Открываем {n} набора…","many":"Открываем {n} наборов…","other":"Открываем {n} набора…"},"stChecking":"Проверяем, что дали наборы…","stSingles":{"one":"Забираем ещё {n} предмет…","few":"Забираем ещё {n} предмета…","many":"Забираем ещё {n} предметов…","other":"Забираем ещё {n} предмета…"},"stItems":{"one":"Забираем {n} предмет…","few":"Забираем {n} предмета…","many":"Забираем {n} предметов…","other":"Забираем {n} предмета…"},"stStopping":"Остановимся после текущих запросов…","doneTitle":"Всё ваше","stoppedTitle":"Остановлено","nothingClaimed":"Ничего не получено","nothingTitle":"Забирать больше нечего","nothingText":"У вас уже есть все бесплатные предметы сегодняшнего магазина. Новые появятся после обновления магазина в 00:00 UTC.","doneText":{"one":"{n} предмет добавлен в шкафчик за {t}.","few":"{n} предмета добавлены в шкафчик за {t}.","many":"{n} предметов добавлено в шкафчик за {t}.","other":"{n} предмета добавлено в шкафчик за {t}."},"stoppedText":{"one":"Получен {n} предмет до остановки.","few":"Получено {n} предмета до остановки.","many":"Получено {n} предметов до остановки.","other":"Получено {n} предмета до остановки."},"secs":"{s} с","mins":"{m} мин {s} с","chipVbOk":"В-баксы не тронуты: {b}","chipVbBad":"В-баксы изменились: {a} → {b}","chipWorth":"Предметов примерно на {v} В-баксов","chipClaimed":{"one":"{n} запрос","few":"{n} запроса","many":"{n} запросов","other":"{n} запроса"},"chipBundle":{"one":"{n} пришёл с наборами","few":"{n} пришли с наборами","many":"{n} пришли с наборами","other":"{n} пришли с наборами"},"chipSkipped":{"one":"{n} пропущен","few":"{n} пропущено","many":"{n} пропущено","other":"{n} пропущено"},"chipLeftOut":{"one":"{n} не выбран вами","few":"{n} не выбраны вами","many":"{n} не выбраны вами","other":"{n} не выбраны вами"},"whySkipped":"Почему некоторые пропущены","leftOutList":"Что вы не выбрали","notCovered":{"one":"{n} предмет не входит в пиратский магазин","few":"{n} предмета не входят в пиратский магазин","many":"{n} предметов не входят в пиратский магазин","other":"{n} предмета не входят в пиратский магазин"},"rmPack":"набор за реальные деньги","vbPrice":"{v} В-баксов","rExcluded":"не входит в пиратский магазин","rNoLongerFree":"больше не бесплатно","rSidekickUsed":"бесплатный компаньон уже использован","rSidekick":"есть компаньон","rSlow":"Epic попросила замедлиться. Попробуйте через минуту","saveList":"Сохранить список","retry":{"one":"Повторить {n}","few":"Повторить {n}","many":"Повторить {n}","other":"Повторить {n}"},"reviewAgain":"Забрать остальное","done":"Готово","close":"Закрыть","tryAgain":"Ещё раз","gotIt":"Понятно","safetyTitle":"Аварийная остановка","safetyText":"В-баксы изменились с {a} до {b}, поэтому «Забрать всё» сразу остановилось. Проверьте баланс и историю покупок в Epic, прежде чем запускать снова.","snagTitle":"Что-то пошло не так","snagText":"{msg}. Ничего не списано.","signinTitle":"Сначала войдите","signinText":"Вы не вошли на fortnite.com. Войдите с учётной записью, где активен пиратский магазин, и попробуйте снова.","signin":"Войти","noTokenTitle":"Пиратский магазин не активен","noTokenText":"Предметы на этой учётной записи всё ещё стоят В-баксы, поэтому ничего не получено. Если у вас есть жетон доступа в пиратский магазин, сначала активируйте его в магазине предметов внутри Fortnite. Он действует 14 дней с момента активации.","notActiveTitle":"Жетон ещё не активирован","notActiveText":"У вас есть жетон доступа в пиратский магазин, но он ещё не активирован. Активируйте его в магазине предметов внутри Fortnite и вернитесь сюда. Он действует 14 дней, так что активируйте, когда будете готовы. Жетон 2026 года нужно активировать до 16 дек., 00:00 GMT.","errShop":"Не удалось прочитать магазин предметов","errCsrf":"Не удалось получить токен безопасности от Epic","errHttp":"Сайт Epic ответил ошибкой (HTTP {s})","errSafety":"Аварийная остановка: В-баксы изменились с {a} до {b}","listHeader":"Полученное в пиратском магазине {d}","pTitle":"Забрать всё в пиратском магазине","pOpen":"Открыть магазин предметов","pClaim":"Забрать всё бесплатно","pWaiting":{"one":"Ждёт {n} бесплатный предмет","few":"Ждут {n} бесплатных предмета","many":"Ждут {n} бесплатных предметов","other":"Ждут {n} бесплатного предмета"},"pUntil":"Активен до {d}","pNoShop":"Откройте fortnite.com/item-shop, войдя в учётную запись. Кнопка появится рядом с покупкой В-баксов.","pSignin":"Войдите на fortnite.com, чтобы увидеть бесплатные предметы.","pNoToken":"Пиратский магазин ещё не активен на этой учётной записи.","pNotActive":"Жетон есть, но не активирован. Активируйте его в магазине предметов в игре.","pAllDone":"У вас уже есть все бесплатные предметы сегодняшнего магазина.","pRunning":"Забираем…","pReminder":"Напоминать об обновлении магазина","pReminderSub":"Каждый день в 00:00 UTC, пока активен пиратский магазин","pQuick":"Пропускать экран выбора","pLifetime":{"one":"Получен {n} предмет, примерно на {v} В-баксов","few":"Получено {n} предмета, примерно на {v} В-баксов","many":"Получено {n} предметов, примерно на {v} В-баксов","other":"Получено {n} предмета, примерно на {v} В-баксов"},"pUpdate":"Доступна версия {v}","pUpdateGet":"Скачать","pFooter":"Сделано 2xFlaw · Chest Counter","nTitle":"Магазин предметов обновился","nText":"Новые предметы бесплатны, пока активен пиратский магазин. Нажмите, чтобы забрать их."},"fr":{"brand":"Tout récupérer","claimAll":"Tout récupérer gratuitement","claiming":"Récupération…","allClaimed":"Tout est récupéré","tipSignin":"Connectez-vous pour récupérer","tipNoToken":"La boutique pirate n’est pas active sur ce compte","tipReady":{"one":"Récupérer {n} objet gratuit. Ne dépense jamais de V-bucks","other":"Récupérer {n} objets gratuits. Ne dépense jamais de V-bucks"},"tipNone":"Vous possédez tous les objets gratuits de la boutique du jour","reading":"Lecture de la boutique…","demoBadge":"Démo : rien n’est envoyé à Epic","reviewTitle":{"one":"{n} objet gratuit","other":"{n} objets gratuits"},"reviewWorth":"Environ {v} V-bucks d’objets","reviewUntil":"Boutique pirate active jusqu’au {d}","reviewPick":"Choisissez quoi récupérer","selectAll":"Tout","selectNone":"Rien","cat_sidekicks":"Acolytes","cat_bundles":"Lots","cat_outfits":"Tenues","cat_emotes":"Emotes","cat_pickaxes":"Pioches","cat_backblings":"Accessoires de dos","cat_gliders":"Planeurs","cat_wraps":"Revêtements","cat_contrails":"Traînées","cat_jam":"Pistes de jam","cat_instruments":"Instruments","cat_cars":"Cosmétiques de voiture","cat_kicks":"Baskets","cat_other":"Autres","skTitle":"Choisissez votre unique acolyte gratuit","skText":"La boutique pirate ne donne qu’un seul acolyte gratuit que vous ne possédez pas encore, alors choisissez-le ici. Tout le reste qui contient un acolyte est laissé de côté, et vous pouvez récupérer ces objets séparément.","skBundle":"Lot avec un acolyte","skSingle":"Acolyte","skNone":"Pas maintenant","skNoneSub":"Garder mon acolyte gratuit pour plus tard","safety":"Ne dépense jamais de V-bucks. Chaque demande est envoyée avec un prix de 0, et tout s’arrête aussitôt si votre solde baisse.","balanceNow":"Solde : {b} V-bucks","quickToggle":"Passer cet écran la prochaine fois","start":{"one":"Récupérer {n} objet","other":"Récupérer {n} objets"},"cancel":"Annuler","stop":"Arrêter","tagBundle":"Lot","stSidekick":"Récupération de votre acolyte…","stBundles":{"one":"Ouverture de {n} lot…","other":"Ouverture de {n} lots…"},"stChecking":"Vérification de ce que les lots ont donné…","stSingles":{"one":"Récupération de {n} objet de plus…","other":"Récupération de {n} objets de plus…"},"stItems":{"one":"Récupération de {n} objet…","other":"Récupération de {n} objets…"},"stStopping":"Arrêt après les demandes en cours…","doneTitle":"Tout est à vous","stoppedTitle":"Arrêté","nothingClaimed":"Rien n’a été récupéré","nothingTitle":"Plus rien à récupérer","nothingText":"Vous possédez déjà tous les objets gratuits de la boutique du jour. De nouveaux objets arrivent quand la boutique se renouvelle à 00:00 UTC.","doneText":{"one":"{n} cosmétique ajouté à votre casier en {t}.","other":"{n} cosmétiques ajoutés à votre casier en {t}."},"stoppedText":{"one":"{n} objet obtenu avant l’arrêt.","other":"{n} objets obtenus avant l’arrêt."},"secs":"{s} secondes","mins":"{m} min {s} s","chipVbOk":"V-bucks intacts : {b}","chipVbBad":"V-bucks modifiés : {a} → {b}","chipWorth":"Environ {v} V-bucks d’objets","chipClaimed":{"one":"{n} demande","other":"{n} demandes"},"chipBundle":{"one":"{n} venu avec des lots","other":"{n} venus avec des lots"},"chipSkipped":{"one":"{n} ignoré","other":"{n} ignorés"},"chipLeftOut":{"one":"{n} laissé de côté par vous","other":"{n} laissés de côté par vous"},"whySkipped":"Pourquoi certains ont été ignorés","leftOutList":"Ce que vous avez laissé de côté","notCovered":{"one":"{n} objet non couvert par la boutique pirate","other":"{n} objets non couverts par la boutique pirate"},"rmPack":"pack en argent réel","vbPrice":"{v} V-bucks","rExcluded":"ne fait pas partie de la boutique pirate","rNoLongerFree":"n’est plus gratuit","rSidekickUsed":"votre acolyte gratuit est déjà utilisé","rSidekick":"contient un acolyte","rSlow":"Epic nous a demandé de ralentir. Réessayez dans une minute","saveList":"Enregistrer la liste","retry":{"one":"Réessayer {n}","other":"Réessayer {n}"},"reviewAgain":"Récupérer le reste","done":"Terminé","close":"Fermer","tryAgain":"Réessayer","gotIt":"Compris","safetyTitle":"Arrêt de sécurité","safetyText":"Vos V-bucks sont passés de {a} à {b}, donc Tout récupérer s’est arrêté aussitôt. Vérifiez votre solde et votre historique d’achats chez Epic avant de relancer.","snagTitle":"Petit souci","snagText":"{msg}. Rien n’a été facturé.","signinTitle":"Connectez-vous d’abord","signinText":"Vous n’êtes pas connecté sur fortnite.com. Connectez-vous avec le compte dont la boutique pirate est active, puis réessayez.","signin":"Se connecter","noTokenTitle":"La boutique pirate n’est pas active","noTokenText":"Les objets de ce compte coûtent encore des V-bucks, donc rien n’a été récupéré. Si vous avez le jeton d’accès à la boutique pirate, activez-le d’abord dans la boutique Fortnite en jeu. Il dure 14 jours à partir de l’activation.","notActiveTitle":"Jeton pas encore activé","notActiveText":"Vous avez le jeton d’accès à la boutique pirate, mais il n’est pas encore activé. Activez-le dans la boutique Fortnite en jeu, puis revenez ici. Il dure 14 jours, alors activez-le quand vous êtes prêt. Le jeton 2026 doit être activé avant le 16 déc., 00:00 GMT.","errShop":"Impossible de lire la boutique","errCsrf":"Impossible d’obtenir un jeton de sécurité d’Epic","errHttp":"Le site d’Epic a répondu par une erreur (HTTP {s})","errSafety":"Arrêt de sécurité : vos V-bucks sont passés de {a} à {b}","listHeader":"Récupérations de la boutique pirate du {d}","pTitle":"Tout récupérer dans la boutique pirate","pOpen":"Ouvrir la boutique","pClaim":"Tout récupérer gratuitement","pWaiting":{"one":"{n} objet gratuit en attente","other":"{n} objets gratuits en attente"},"pUntil":"Active jusqu’au {d}","pNoShop":"Ouvrez fortnite.com/item-shop en étant connecté. Le bouton apparaît à côté d’Acheter des V-bucks.","pSignin":"Connectez-vous sur fortnite.com pour voir vos objets gratuits.","pNoToken":"La boutique pirate n’est pas encore active sur ce compte.","pNotActive":"Jeton obtenu mais pas activé. Activez-le dans la boutique en jeu.","pAllDone":"Vous possédez tous les objets gratuits de la boutique du jour.","pRunning":"Récupération…","pReminder":"Me prévenir quand la boutique se renouvelle","pReminderSub":"Chaque jour à 00:00 UTC tant que votre boutique pirate est active","pQuick":"Passer l’écran de sélection","pLifetime":{"one":"{n} objet récupéré jusqu’ici, environ {v} V-bucks","other":"{n} objets récupérés jusqu’ici, environ {v} V-bucks"},"pUpdate":"La version {v} est disponible","pUpdateGet":"Télécharger","pFooter":"Créé par 2xFlaw · Chest Counter","nTitle":"La boutique s’est renouvelée","nText":"Les nouveaux objets sont gratuits tant que votre boutique pirate est active. Cliquez pour les récupérer."},"de":{"brand":"Alles abholen","claimAll":"Alles gratis abholen","claiming":"Wird abgeholt…","allClaimed":"Alles abgeholt","tipSignin":"Zum Abholen anmelden","tipNoToken":"Der Piratenshop ist auf diesem Konto nicht aktiv","tipReady":{"one":"{n} Gratis-Gegenstand abholen. Gibt nie V-Bucks aus","other":"{n} Gratis-Gegenstände abholen. Gibt nie V-Bucks aus"},"tipNone":"Du besitzt alle Gratis-Gegenstände im heutigen Shop","reading":"Shop wird gelesen…","demoBadge":"Demo: Es wird nichts an Epic gesendet","reviewTitle":{"one":"{n} Gratis-Gegenstand","other":"{n} Gratis-Gegenstände"},"reviewWorth":"Gegenstände im Wert von etwa {v} V-Bucks","reviewUntil":"Piratenshop aktiv bis {d}","reviewPick":"Wähle, was du abholen willst","selectAll":"Alle","selectNone":"Keine","cat_sidekicks":"Begleiter","cat_bundles":"Bundles","cat_outfits":"Outfits","cat_emotes":"Emotes","cat_pickaxes":"Spitzhacken","cat_backblings":"Rücken-Accessoires","cat_gliders":"Hängegleiter","cat_wraps":"Lackierungen","cat_contrails":"Kondensstreifen","cat_jam":"Jam-Tracks","cat_instruments":"Instrumente","cat_cars":"Auto-Kosmetik","cat_kicks":"Sneaker","cat_other":"Sonstiges","skTitle":"Wähle deinen einen Gratis-Begleiter","skText":"Der Piratenshop gibt dir nur einen Gratis-Begleiter, den du noch nicht besitzt, also wähle ihn hier aus. Alles andere mit einem Begleiter bleibt außen vor, diese Gegenstände kannst du separat abholen.","skBundle":"Bundle mit Begleiter","skSingle":"Begleiter","skNone":"Jetzt nicht","skNoneSub":"Gratis-Begleiter für später aufheben","safety":"Gibt nie V-Bucks aus. Jede Anfrage wird mit Preis 0 gesendet, und alles stoppt sofort, wenn dein Guthaben sinkt.","balanceNow":"Guthaben: {b} V-Bucks","quickToggle":"Diesen Bildschirm nächstes Mal überspringen","start":{"one":"{n} Gegenstand abholen","other":"{n} Gegenstände abholen"},"cancel":"Abbrechen","stop":"Stopp","tagBundle":"Bundle","stSidekick":"Dein Begleiter wird abgeholt…","stBundles":{"one":"{n} Bundle wird geöffnet…","other":"{n} Bundles werden geöffnet…"},"stChecking":"Prüfe, was die Bundles gebracht haben…","stSingles":{"one":"Hole {n} weiteren Gegenstand…","other":"Hole {n} weitere Gegenstände…"},"stItems":{"one":"Hole {n} Gegenstand…","other":"Hole {n} Gegenstände…"},"stStopping":"Stoppt nach den laufenden Anfragen…","doneTitle":"Alles deins","stoppedTitle":"Gestoppt","nothingClaimed":"Nichts abgeholt","nothingTitle":"Nichts mehr abzuholen","nothingText":"Du besitzt bereits alle Gratis-Gegenstände im heutigen Shop. Neue Gegenstände kommen, wenn sich der Shop um 00:00 UTC erneuert.","doneText":{"one":"{n} Kosmetikgegenstand in {t} zu deinem Spind hinzugefügt.","other":"{n} Kosmetikgegenstände in {t} zu deinem Spind hinzugefügt."},"stoppedText":{"one":"{n} Gegenstand vor dem Stopp erhalten.","other":"{n} Gegenstände vor dem Stopp erhalten."},"secs":"{s} Sekunden","mins":"{m} Min. {s} s","chipVbOk":"V-Bucks unberührt: {b}","chipVbBad":"V-Bucks geändert: {a} → {b}","chipWorth":"Gegenstände im Wert von etwa {v} V-Bucks","chipClaimed":{"one":"{n} Anfrage","other":"{n} Anfragen"},"chipBundle":{"one":"{n} kam mit Bundles","other":"{n} kamen mit Bundles"},"chipSkipped":{"one":"{n} übersprungen","other":"{n} übersprungen"},"chipLeftOut":{"one":"{n} von dir ausgelassen","other":"{n} von dir ausgelassen"},"whySkipped":"Warum einige übersprungen wurden","leftOutList":"Was du ausgelassen hast","notCovered":{"one":"{n} Gegenstand, den der Piratenshop nicht abdeckt","other":"{n} Gegenstände, die der Piratenshop nicht abdeckt"},"rmPack":"Echtgeld-Paket","vbPrice":"{v} V-Bucks","rExcluded":"nicht Teil des Piratenshops","rNoLongerFree":"nicht mehr gratis","rSidekickUsed":"dein Gratis-Begleiter ist schon vergeben","rSidekick":"enthält einen Begleiter","rSlow":"Epic hat uns gebeten, langsamer zu machen. Versuch es in einer Minute erneut","saveList":"Liste speichern","retry":{"one":"{n} erneut versuchen","other":"{n} erneut versuchen"},"reviewAgain":"Den Rest abholen","done":"Fertig","close":"Schließen","tryAgain":"Erneut versuchen","gotIt":"Verstanden","safetyTitle":"Sicherheitsstopp","safetyText":"Deine V-Bucks sind von {a} auf {b} gefallen, darum hat Alles abholen sofort gestoppt. Prüfe dein Guthaben und deinen Kaufverlauf bei Epic, bevor du es erneut startest.","snagTitle":"Kleines Problem","snagText":"{msg}. Es wurde nichts berechnet.","signinTitle":"Erst anmelden","signinText":"Du bist auf fortnite.com nicht angemeldet. Melde dich mit dem Konto an, auf dem der Piratenshop aktiv ist, und versuch es erneut.","signin":"Anmelden","noTokenTitle":"Der Piratenshop ist nicht aktiv","noTokenText":"Gegenstände auf diesem Konto kosten noch V-Bucks, daher wurde nichts abgeholt. Wenn du die Zugangsmarke für den Piratenshop hast, aktiviere sie zuerst im Item-Shop im Spiel. Sie gilt 14 Tage ab der Aktivierung.","notActiveTitle":"Marke noch nicht aktiviert","notActiveText":"Du hast die Zugangsmarke für den Piratenshop, aber sie ist noch nicht aktiviert. Aktiviere sie im Item-Shop im Spiel und komm dann hierher zurück. Sie gilt 14 Tage, also aktiviere sie, wenn du bereit bist. Die Marke für 2026 muss bis 16. Dez., 00:00 GMT aktiviert werden.","errShop":"Der Item-Shop konnte nicht gelesen werden","errCsrf":"Kein Sicherheitstoken von Epic erhalten","errHttp":"Epics Website hat mit einem Fehler geantwortet (HTTP {s})","errSafety":"Sicherheitsstopp: Deine V-Bucks sind von {a} auf {b} gefallen","listHeader":"Piratenshop-Abholungen am {d}","pTitle":"Piratenshop: Alles abholen","pOpen":"Item-Shop öffnen","pClaim":"Alles gratis abholen","pWaiting":{"one":"{n} Gratis-Gegenstand wartet","other":"{n} Gratis-Gegenstände warten"},"pUntil":"Aktiv bis {d}","pNoShop":"Öffne fortnite.com/item-shop, während du angemeldet bist. Der Button erscheint neben V-Bucks kaufen.","pSignin":"Melde dich auf fortnite.com an, um deine Gratis-Gegenstände zu sehen.","pNoToken":"Der Piratenshop ist auf diesem Konto noch nicht aktiv.","pNotActive":"Marke vorhanden, aber nicht aktiviert. Aktiviere sie im Item-Shop im Spiel.","pAllDone":"Du besitzt alle Gratis-Gegenstände im heutigen Shop.","pRunning":"Wird abgeholt…","pReminder":"Erinnern, wenn sich der Shop erneuert","pReminderSub":"Täglich um 00:00 UTC, solange dein Piratenshop aktiv ist","pQuick":"Auswahlbildschirm überspringen","pLifetime":{"one":"Bisher {n} Gegenstand abgeholt, etwa {v} V-Bucks","other":"Bisher {n} Gegenstände abgeholt, etwa {v} V-Bucks"},"pUpdate":"Version {v} ist verfügbar","pUpdateGet":"Holen","pFooter":"Erstellt von 2xFlaw · Chest Counter","nTitle":"Der Item-Shop hat sich erneuert","nText":"Neue Gegenstände sind gratis, solange dein Piratenshop aktiv ist. Klicke, um sie abzuholen."},"it":{"brand":"Riscatta tutto","claimAll":"Riscatta tutto gratis","claiming":"Riscatto in corso…","allClaimed":"Tutto riscattato","tipSignin":"Accedi per riscattare","tipNoToken":"Il negozio pirata non è attivo su questo account","tipReady":{"one":"Riscatta {n} oggetto gratis. Non spende mai V-buck","other":"Riscatta {n} oggetti gratis. Non spende mai V-buck"},"tipNone":"Hai già tutti gli oggetti gratis del negozio di oggi","reading":"Lettura del negozio…","demoBadge":"Demo: non viene inviato nulla a Epic","reviewTitle":{"one":"{n} oggetto gratis","other":"{n} oggetti gratis"},"reviewWorth":"Circa {v} V-buck di oggetti","reviewUntil":"Negozio pirata attivo fino al {d}","reviewPick":"Scegli cosa riscattare","selectAll":"Tutto","selectNone":"Niente","cat_sidekicks":"Compagni","cat_bundles":"Bundle","cat_outfits":"Costumi","cat_emotes":"Emote","cat_pickaxes":"Picconi","cat_backblings":"Dorsi decorativi","cat_gliders":"Deltaplani","cat_wraps":"Coperture","cat_contrails":"Scie","cat_jam":"Brani jam","cat_instruments":"Strumenti","cat_cars":"Cosmetici per auto","cat_kicks":"Scarpe","cat_other":"Altro","skTitle":"Scegli il tuo unico compagno gratis","skText":"Il negozio pirata ti dà un solo compagno gratis che non hai ancora, quindi sceglilo qui. Tutto il resto che contiene un compagno viene escluso, e puoi riscattare quegli oggetti separatamente.","skBundle":"Bundle con compagno","skSingle":"Compagno","skNone":"Non ora","skNoneSub":"Tieni il compagno gratis per dopo","safety":"Non spende mai V-buck. Ogni riscatto viene inviato con prezzo 0 e si ferma subito se il tuo saldo scende.","balanceNow":"Saldo: {b} V-buck","quickToggle":"Salta questa schermata la prossima volta","start":{"one":"Riscatta {n} oggetto","other":"Riscatta {n} oggetti"},"cancel":"Annulla","stop":"Ferma","tagBundle":"Bundle","stSidekick":"Riscatto del tuo compagno…","stBundles":{"one":"Apertura di {n} bundle…","other":"Apertura di {n} bundle…"},"stChecking":"Controllo di cosa ti hanno dato i bundle…","stSingles":{"one":"Riscatto di altri {n} oggetto…","other":"Riscatto di altri {n} oggetti…"},"stItems":{"one":"Riscatto di {n} oggetto…","other":"Riscatto di {n} oggetti…"},"stStopping":"Arresto dopo i riscatti in corso…","doneTitle":"Tutto tuo","stoppedTitle":"Fermato","nothingClaimed":"Nulla riscattato","nothingTitle":"Niente da riscattare","nothingText":"Hai già tutti gli oggetti gratis del negozio di oggi. I nuovi oggetti arrivano quando il negozio si aggiorna alle 00:00 UTC.","doneText":{"one":"{n} oggetto cosmetico aggiunto al tuo armadietto in {t}.","other":"{n} oggetti cosmetici aggiunti al tuo armadietto in {t}."},"stoppedText":{"one":"{n} oggetto ottenuto prima dell’arresto.","other":"{n} oggetti ottenuti prima dell’arresto."},"secs":"{s} secondi","mins":"{m} min {s} s","chipVbOk":"V-buck intatti: {b}","chipVbBad":"V-buck cambiati: {a} → {b}","chipWorth":"Circa {v} V-buck di oggetti","chipClaimed":{"one":"{n} riscatto","other":"{n} riscatti"},"chipBundle":{"one":"{n} arrivato con i bundle","other":"{n} arrivati con i bundle"},"chipSkipped":{"one":"{n} saltato","other":"{n} saltati"},"chipLeftOut":{"one":"{n} escluso da te","other":"{n} esclusi da te"},"whySkipped":"Perché alcuni sono stati saltati","leftOutList":"Cosa hai escluso","notCovered":{"one":"{n} oggetto non coperto dal negozio pirata","other":"{n} oggetti non coperti dal negozio pirata"},"rmPack":"pacchetto a soldi veri","vbPrice":"{v} V-buck","rExcluded":"non fa parte del negozio pirata","rNoLongerFree":"non è più gratis","rSidekickUsed":"il tuo compagno gratis è già stato usato","rSidekick":"contiene un compagno","rSlow":"Epic ci ha chiesto di rallentare. Riprova tra un minuto","saveList":"Salva elenco","retry":{"one":"Riprova {n}","other":"Riprova {n}"},"reviewAgain":"Riscatta il resto","done":"Fatto","close":"Chiudi","tryAgain":"Riprova","gotIt":"Ho capito","safetyTitle":"Arresto di sicurezza","safetyText":"I tuoi V-buck sono passati da {a} a {b}, quindi Riscatta tutto si è fermato subito. Controlla il saldo e la cronologia acquisti su Epic prima di riavviarlo.","snagTitle":"Qualcosa è andato storto","snagText":"{msg}. Non è stato addebitato nulla.","signinTitle":"Prima accedi","signinText":"Non hai effettuato l’accesso su fortnite.com. Accedi con l’account che ha il negozio pirata attivo, poi riprova.","signin":"Accedi","noTokenTitle":"Il negozio pirata non è attivo","noTokenText":"Gli oggetti di questo account costano ancora V-buck, quindi non è stato riscattato nulla. Se hai il gettone d’accesso al negozio pirata, attivalo prima nel Negozio oggetti di Fortnite in gioco. Dura 14 giorni dall’attivazione.","notActiveTitle":"Gettone non ancora attivato","notActiveText":"Hai il gettone d’accesso al negozio pirata, ma non è ancora attivato. Attivalo nel Negozio oggetti di Fortnite in gioco, poi torna qui. Dura 14 giorni, quindi attivalo quando sei pronto. Il gettone 2026 va attivato entro il 16 dic., 00:00 GMT.","errShop":"Impossibile leggere il Negozio oggetti","errCsrf":"Impossibile ottenere un token di sicurezza da Epic","errHttp":"Il sito di Epic ha risposto con un errore (HTTP {s})","errSafety":"Arresto di sicurezza: i tuoi V-buck sono passati da {a} a {b}","listHeader":"Riscatti del negozio pirata del {d}","pTitle":"Riscatta tutto nel negozio pirata","pOpen":"Apri il Negozio oggetti","pClaim":"Riscatta tutto gratis","pWaiting":{"one":"{n} oggetto gratis in attesa","other":"{n} oggetti gratis in attesa"},"pUntil":"Attivo fino al {d}","pNoShop":"Apri fortnite.com/item-shop dopo aver effettuato l’accesso. Il pulsante compare accanto ad Acquista V-buck.","pSignin":"Accedi su fortnite.com per vedere i tuoi oggetti gratis.","pNoToken":"Il negozio pirata non è ancora attivo su questo account.","pNotActive":"Gettone ottenuto ma non attivato. Attivalo nel Negozio oggetti in gioco.","pAllDone":"Hai già tutti gli oggetti gratis del negozio di oggi.","pRunning":"Riscatto in corso…","pReminder":"Avvisami quando il negozio si aggiorna","pReminderSub":"Ogni giorno alle 00:00 UTC finché il negozio pirata è attivo","pQuick":"Salta la schermata di scelta","pLifetime":{"one":"{n} oggetto riscattato finora, circa {v} V-buck","other":"{n} oggetti riscattati finora, circa {v} V-buck"},"pUpdate":"È disponibile la versione {v}","pUpdateGet":"Scarica","pFooter":"Creato da 2xFlaw · Chest Counter","nTitle":"Il Negozio oggetti si è aggiornato","nText":"I nuovi oggetti sono gratis finché il negozio pirata è attivo. Clicca per riscattarli."},"pl":{"brand":"Odbierz wszystko","claimAll":"Odbierz wszystko za darmo","claiming":"Odbieranie…","allClaimed":"Wszystko odebrane","tipSignin":"Zaloguj się, aby odebrać","tipNoToken":"Sklep piracki nie jest aktywny na tym koncie","tipReady":{"one":"Odbierz {n} darmowy przedmiot. Nigdy nie wydaje V-dolców","few":"Odbierz {n} darmowe przedmioty. Nigdy nie wydaje V-dolców","many":"Odbierz {n} darmowych przedmiotów. Nigdy nie wydaje V-dolców","other":"Odbierz {n} darmowego przedmiotu. Nigdy nie wydaje V-dolców"},"tipNone":"Masz już wszystkie darmowe przedmioty z dzisiejszego sklepu","reading":"Odczytywanie sklepu…","demoBadge":"Demo: nic nie jest wysyłane do Epic","reviewTitle":{"one":"{n} darmowy przedmiot","few":"{n} darmowe przedmioty","many":"{n} darmowych przedmiotów","other":"{n} darmowego przedmiotu"},"reviewWorth":"Przedmioty warte około {v} V-dolców","reviewUntil":"Sklep piracki aktywny do {d}","reviewPick":"Wybierz, co odebrać","selectAll":"Wszystko","selectNone":"Nic","cat_sidekicks":"Towarzysze","cat_bundles":"Zestawy","cat_outfits":"Stroje","cat_emotes":"Emotki","cat_pickaxes":"Kilofy","cat_backblings":"Plecaki","cat_gliders":"Lotnie","cat_wraps":"Malowania","cat_contrails":"Smugi","cat_jam":"Utwory do jamowania","cat_instruments":"Instrumenty","cat_cars":"Kosmetyki do aut","cat_kicks":"Buty","cat_other":"Inne","skTitle":"Wybierz swojego jedynego darmowego towarzysza","skText":"Sklep piracki daje tylko jednego darmowego towarzysza, którego jeszcze nie masz, więc wybierz go tutaj. Wszystko inne z towarzyszem zostaje pominięte, a te przedmioty możesz odebrać osobno.","skBundle":"Zestaw z towarzyszem","skSingle":"Towarzysz","skNone":"Nie teraz","skNoneSub":"Zostaw darmowego towarzysza na później","safety":"Nigdy nie wydaje V-dolców. Każde odebranie jest wysyłane z ceną 0, a wszystko zatrzymuje się od razu, gdy saldo spadnie.","balanceNow":"Saldo: {b} V-dolców","quickToggle":"Pomiń ten ekran następnym razem","start":{"one":"Odbierz {n} przedmiot","few":"Odbierz {n} przedmioty","many":"Odbierz {n} przedmiotów","other":"Odbierz {n} przedmiotu"},"cancel":"Anuluj","stop":"Zatrzymaj","tagBundle":"Zestaw","stSidekick":"Odbieranie twojego towarzysza…","stBundles":{"one":"Otwieranie {n} zestawu…","few":"Otwieranie {n} zestawów…","many":"Otwieranie {n} zestawów…","other":"Otwieranie {n} zestawu…"},"stChecking":"Sprawdzanie, co dały zestawy…","stSingles":{"one":"Odbieranie jeszcze {n} przedmiotu…","few":"Odbieranie jeszcze {n} przedmiotów…","many":"Odbieranie jeszcze {n} przedmiotów…","other":"Odbieranie jeszcze {n} przedmiotu…"},"stItems":{"one":"Odbieranie {n} przedmiotu…","few":"Odbieranie {n} przedmiotów…","many":"Odbieranie {n} przedmiotów…","other":"Odbieranie {n} przedmiotu…"},"stStopping":"Zatrzymanie po bieżących odebraniach…","doneTitle":"Wszystko twoje","stoppedTitle":"Zatrzymano","nothingClaimed":"Nic nie odebrano","nothingTitle":"Nie ma nic do odebrania","nothingText":"Masz już wszystkie darmowe przedmioty z dzisiejszego sklepu. Nowe pojawią się po odświeżeniu sklepu o 00:00 UTC.","doneText":{"one":"{n} kosmetyk dodany do szafki w {t}.","few":"{n} kosmetyki dodane do szafki w {t}.","many":"{n} kosmetyków dodanych do szafki w {t}.","other":"{n} kosmetyku dodanego do szafki w {t}."},"stoppedText":{"one":"Odebrano {n} przedmiot przed zatrzymaniem.","few":"Odebrano {n} przedmioty przed zatrzymaniem.","many":"Odebrano {n} przedmiotów przed zatrzymaniem.","other":"Odebrano {n} przedmiotu przed zatrzymaniem."},"secs":"{s} s","mins":"{m} min {s} s","chipVbOk":"V-dolce nietknięte: {b}","chipVbBad":"V-dolce się zmieniły: {a} → {b}","chipWorth":"Przedmioty warte około {v} V-dolców","chipClaimed":{"one":"{n} odebranie","few":"{n} odebrania","many":"{n} odebrań","other":"{n} odebrania"},"chipBundle":{"one":"{n} z zestawów","few":"{n} z zestawów","many":"{n} z zestawów","other":"{n} z zestawów"},"chipSkipped":{"one":"{n} pominięty","few":"{n} pominięte","many":"{n} pominiętych","other":"{n} pominiętego"},"chipLeftOut":{"one":"{n} pominięty przez ciebie","few":"{n} pominięte przez ciebie","many":"{n} pominiętych przez ciebie","other":"{n} pominiętego przez ciebie"},"whySkipped":"Dlaczego niektóre pominięto","leftOutList":"Co pominąłeś","notCovered":{"one":"{n} przedmiot nieobjęty sklepem pirackim","few":"{n} przedmioty nieobjęte sklepem pirackim","many":"{n} przedmiotów nieobjętych sklepem pirackim","other":"{n} przedmiotu nieobjętego sklepem pirackim"},"rmPack":"pakiet za prawdziwe pieniądze","vbPrice":"{v} V-dolców","rExcluded":"nie jest częścią sklepu pirackiego","rNoLongerFree":"już nie jest darmowy","rSidekickUsed":"darmowy towarzysz został już wykorzystany","rSidekick":"zawiera towarzysza","rSlow":"Epic poprosił o zwolnienie. Spróbuj ponownie za minutę","saveList":"Zapisz listę","retry":{"one":"Ponów {n}","few":"Ponów {n}","many":"Ponów {n}","other":"Ponów {n}"},"reviewAgain":"Odbierz resztę","done":"Gotowe","close":"Zamknij","tryAgain":"Spróbuj ponownie","gotIt":"Rozumiem","safetyTitle":"Zatrzymanie bezpieczeństwa","safetyText":"Twoje V-dolce zmieniły się z {a} na {b}, więc Odbierz wszystko od razu się zatrzymało. Sprawdź saldo i historię zakupów w Epic, zanim uruchomisz je ponownie.","snagTitle":"Coś poszło nie tak","snagText":"{msg}. Nic nie zostało pobrane.","signinTitle":"Najpierw się zaloguj","signinText":"Nie jesteś zalogowany na fortnite.com. Zaloguj się na konto z aktywnym sklepem pirackim i spróbuj ponownie.","signin":"Zaloguj się","noTokenTitle":"Sklep piracki nie jest aktywny","noTokenText":"Przedmioty na tym koncie nadal kosztują V-dolce, więc nic nie odebrano. Jeśli masz żeton dostępu do sklepu pirackiego, najpierw aktywuj go w sklepie z przedmiotami w grze Fortnite. Działa przez 14 dni od aktywacji.","notActiveTitle":"Żeton jeszcze nieaktywny","notActiveText":"Masz żeton dostępu do sklepu pirackiego, ale nie jest jeszcze aktywowany. Aktywuj go w sklepie z przedmiotami w grze Fortnite i wróć tutaj. Działa przez 14 dni, więc aktywuj go, kiedy będziesz gotowy. Żeton na 2026 trzeba aktywować do 16 gru, 00:00 GMT.","errShop":"Nie udało się odczytać sklepu z przedmiotami","errCsrf":"Nie udało się pobrać tokenu bezpieczeństwa od Epic","errHttp":"Strona Epic odpowiedziała błędem (HTTP {s})","errSafety":"Zatrzymanie bezpieczeństwa: V-dolce zmieniły się z {a} na {b}","listHeader":"Odebrane w sklepie pirackim {d}","pTitle":"Sklep piracki: odbierz wszystko","pOpen":"Otwórz sklep z przedmiotami","pClaim":"Odbierz wszystko za darmo","pWaiting":{"one":"Czeka {n} darmowy przedmiot","few":"Czekają {n} darmowe przedmioty","many":"Czeka {n} darmowych przedmiotów","other":"Czeka {n} darmowego przedmiotu"},"pUntil":"Aktywny do {d}","pNoShop":"Otwórz fortnite.com/item-shop po zalogowaniu. Przycisk pojawi się obok Kup V-dolce.","pSignin":"Zaloguj się na fortnite.com, aby zobaczyć darmowe przedmioty.","pNoToken":"Sklep piracki nie jest jeszcze aktywny na tym koncie.","pNotActive":"Masz żeton, ale nie jest aktywowany. Aktywuj go w sklepie w grze.","pAllDone":"Masz już wszystkie darmowe przedmioty z dzisiejszego sklepu.","pRunning":"Odbieranie…","pReminder":"Przypominaj o odświeżeniu sklepu","pReminderSub":"Codziennie o 00:00 UTC, dopóki sklep piracki jest aktywny","pQuick":"Pomiń ekran wyboru","pLifetime":{"one":"Odebrano {n} przedmiot, około {v} V-dolców","few":"Odebrano {n} przedmioty, około {v} V-dolców","many":"Odebrano {n} przedmiotów, około {v} V-dolców","other":"Odebrano {n} przedmiotu, około {v} V-dolców"},"pUpdate":"Dostępna jest wersja {v}","pUpdateGet":"Pobierz","pFooter":"Stworzone przez 2xFlaw · Chest Counter","nTitle":"Sklep z przedmiotami się odświeżył","nText":"Nowe przedmioty są darmowe, dopóki sklep piracki jest aktywny. Kliknij, aby je odebrać."},"tr":{"brand":"Hepsini al","claimAll":"Hepsini ücretsiz al","claiming":"Alınıyor…","allClaimed":"Hepsi alındı","tipSignin":"Almak için giriş yap","tipNoToken":"Korsan dükkânı bu hesapta etkin değil","tipReady":{"one":"{n} ücretsiz eşyayı al. Asla V-Papel harcamaz","other":"{n} ücretsiz eşyayı al. Asla V-Papel harcamaz"},"tipNone":"Bugünkü dükkândaki tüm ücretsiz eşyalara zaten sahipsin","reading":"Dükkân okunuyor…","demoBadge":"Demo: Epic’e hiçbir şey gönderilmez","reviewTitle":{"one":"{n} ücretsiz eşya","other":"{n} ücretsiz eşya"},"reviewWorth":"Yaklaşık {v} V-Papel değerinde eşya","reviewUntil":"Korsan dükkânı {d} tarihine kadar etkin","reviewPick":"Neleri alacağını seç","selectAll":"Tümü","selectNone":"Hiçbiri","cat_sidekicks":"Yardımcılar","cat_bundles":"Paketler","cat_outfits":"Kıyafetler","cat_emotes":"İfadeler","cat_pickaxes":"Kazmalar","cat_backblings":"Sırt süsleri","cat_gliders":"Planörler","cat_wraps":"Kaplamalar","cat_contrails":"Duman izleri","cat_jam":"Doğaçlama parçaları","cat_instruments":"Enstrümanlar","cat_cars":"Araba kozmetikleri","cat_kicks":"Ayakkabılar","cat_other":"Diğer","skTitle":"Tek ücretsiz yardımcını seç","skText":"Korsan dükkânı sahip olmadığın yalnızca bir yardımcıyı ücretsiz verir, o yüzden onu burada seç. Yardımcı içeren diğer her şey dışarıda bırakılır, bu eşyaları ayrıca alabilirsin.","skBundle":"Yardımcılı paket","skSingle":"Yardımcı","skNone":"Şimdi değil","skNoneSub":"Ücretsiz yardımcımı sonraya sakla","safety":"Asla V-Papel harcamaz. Her alım 0 fiyatla gönderilir ve bakiyen düşerse hemen durur.","balanceNow":"Bakiye: {b} V-Papel","quickToggle":"Bir dahaki sefere bu ekranı atla","start":{"one":"{n} eşyayı al","other":"{n} eşyayı al"},"cancel":"İptal","stop":"Durdur","tagBundle":"Paket","stSidekick":"Yardımcın alınıyor…","stBundles":{"one":"{n} paket açılıyor…","other":"{n} paket açılıyor…"},"stChecking":"Paketlerden ne geldiği kontrol ediliyor…","stSingles":{"one":"{n} eşya daha alınıyor…","other":"{n} eşya daha alınıyor…"},"stItems":{"one":"{n} eşya alınıyor…","other":"{n} eşya alınıyor…"},"stStopping":"Süren alımlardan sonra duruluyor…","doneTitle":"Hepsi senin","stoppedTitle":"Durduruldu","nothingClaimed":"Hiçbir şey alınmadı","nothingTitle":"Alınacak bir şey kalmadı","nothingText":"Bugünkü dükkândaki tüm ücretsiz eşyalara zaten sahipsin. Dükkân 00:00 UTC’de yenilendiğinde yeni eşyalar gelir.","doneText":{"one":"{t} içinde dolabına {n} kozmetik eklendi.","other":"{t} içinde dolabına {n} kozmetik eklendi."},"stoppedText":{"one":"Durmadan önce {n} eşya alındı.","other":"Durmadan önce {n} eşya alındı."},"secs":"{s} saniye","mins":"{m} dk {s} sn","chipVbOk":"V-Papel’e dokunulmadı: {b}","chipVbBad":"V-Papel değişti: {a} → {b}","chipWorth":"Yaklaşık {v} V-Papel değerinde eşya","chipClaimed":{"one":"{n} alım","other":"{n} alım"},"chipBundle":{"one":"{n} paketlerle geldi","other":"{n} paketlerle geldi"},"chipSkipped":{"one":"{n} atlandı","other":"{n} atlandı"},"chipLeftOut":{"one":"{n} senin tarafından dışarıda bırakıldı","other":"{n} senin tarafından dışarıda bırakıldı"},"whySkipped":"Bazıları neden atlandı","leftOutList":"Dışarıda bıraktıkların","notCovered":{"one":"Korsan dükkânının kapsamadığı {n} eşya","other":"Korsan dükkânının kapsamadığı {n} eşya"},"rmPack":"gerçek parayla satılan paket","vbPrice":"{v} V-Papel","rExcluded":"korsan dükkânına dahil değil","rNoLongerFree":"artık ücretsiz değil","rSidekickUsed":"ücretsiz yardımcın zaten kullanıldı","rSidekick":"yardımcı içeriyor","rSlow":"Epic yavaşlamamızı istedi. Bir dakika sonra tekrar dene","saveList":"Listeyi kaydet","retry":{"one":"{n} tanesini yeniden dene","other":"{n} tanesini yeniden dene"},"reviewAgain":"Kalanları al","done":"Tamam","close":"Kapat","tryAgain":"Tekrar dene","gotIt":"Anladım","safetyTitle":"Güvenlik durdurması","safetyText":"V-Papel’in {a} değerinden {b} değerine düştü, bu yüzden Hepsini al hemen durdu. Tekrar çalıştırmadan önce Epic’te bakiyeni ve satın alma geçmişini kontrol et.","snagTitle":"Bir sorun çıktı","snagText":"{msg}. Hiçbir ücret alınmadı.","signinTitle":"Önce giriş yap","signinText":"fortnite.com’da giriş yapmamışsın. Korsan dükkânı etkin olan hesapla giriş yap ve tekrar dene.","signin":"Giriş yap","noTokenTitle":"Korsan dükkânı etkin değil","noTokenText":"Bu hesaptaki eşyalar hâlâ V-Papel tutuyor, bu yüzden hiçbir şey alınmadı. Korsan dükkânı erişim jetonun varsa önce oyun içindeki Fortnite Eşya Dükkânı’nda etkinleştir. Etkinleştirmeden itibaren 14 gün sürer.","notActiveTitle":"Jeton henüz etkin değil","notActiveText":"Korsan dükkânı erişim jetonun var ama henüz etkinleştirilmedi. Oyun içindeki Fortnite Eşya Dükkânı’nda etkinleştir, sonra buraya geri dön. 14 gün sürer, o yüzden hazır olduğunda etkinleştir. 2026 jetonu 16 Ara, 00:00 GMT’ye kadar etkinleştirilmeli.","errShop":"Eşya Dükkânı okunamadı","errCsrf":"Epic’ten güvenlik jetonu alınamadı","errHttp":"Epic’in sitesi bir hatayla yanıt verdi (HTTP {s})","errSafety":"Güvenlik durdurması: V-Papel’in {a} değerinden {b} değerine düştü","listHeader":"{d} tarihli korsan dükkânı alımları","pTitle":"Korsan dükkânı: hepsini al","pOpen":"Eşya Dükkânı’nı aç","pClaim":"Hepsini ücretsiz al","pWaiting":{"one":"{n} ücretsiz eşya bekliyor","other":"{n} ücretsiz eşya bekliyor"},"pUntil":"{d} tarihine kadar etkin","pNoShop":"Giriş yapmışken fortnite.com/item-shop adresini aç. Düğme V-Papel Satın Al’ın yanında çıkar.","pSignin":"Ücretsiz eşyalarını görmek için fortnite.com’da giriş yap.","pNoToken":"Korsan dükkânı bu hesapta henüz etkin değil.","pNotActive":"Jeton var ama etkin değil. Oyun içi Eşya Dükkânı’nda etkinleştir.","pAllDone":"Bugünkü dükkândaki tüm ücretsiz eşyalara zaten sahipsin.","pRunning":"Alınıyor…","pReminder":"Dükkân yenilenince bana hatırlat","pReminderSub":"Korsan dükkânın etkin olduğu sürece her gün 00:00 UTC’de","pQuick":"Seçim ekranını atla","pLifetime":{"one":"Şu ana kadar {n} eşya alındı, yaklaşık {v} V-Papel","other":"Şu ana kadar {n} eşya alındı, yaklaşık {v} V-Papel"},"pUpdate":"{v} sürümü yayında","pUpdateGet":"İndir","pFooter":"2xFlaw tarafından · Chest Counter","nTitle":"Eşya Dükkânı yenilendi","nText":"Korsan dükkânın etkin olduğu sürece yeni eşyalar ücretsiz. Almak için tıkla."},"ja":{"brand":"まとめて入手","claimAll":"無料アイテムをまとめて入手","claiming":"入手中…","allClaimed":"すべて入手済み","tipSignin":"サインインして入手","tipNoToken":"このアカウントでは海賊ショップが有効ではありません","tipReady":{"other":"無料アイテム{n}個を入手。V-Bucksは一切使いません"},"tipNone":"今日のショップの無料アイテムはすべて所持しています","reading":"ショップを読み込み中…","demoBadge":"デモ: Epicには何も送信されません","reviewTitle":{"other":"無料アイテム{n}個"},"reviewWorth":"約{v} V-Bucks分のアイテム","reviewUntil":"海賊ショップは{d}まで有効","reviewPick":"入手するものを選択","selectAll":"すべて","selectNone":"なし","cat_sidekicks":"相棒","cat_bundles":"バンドル","cat_outfits":"コスチューム","cat_emotes":"エモート","cat_pickaxes":"収集ツール","cat_backblings":"バックアクセサリー","cat_gliders":"グライダー","cat_wraps":"ラップ","cat_contrails":"コントレイル","cat_jam":"ジャムトラック","cat_instruments":"楽器","cat_cars":"車のコスメティック","cat_kicks":"キックス","cat_other":"その他","skTitle":"無料の相棒を1体選んでください","skText":"海賊ショップで無料になる相棒は、まだ持っていない1体だけです。ここで選んでください。相棒を含むほかのアイテムは除外されるので、それらは別に入手できます。","skBundle":"相棒入りバンドル","skSingle":"相棒","skNone":"今はしない","skNoneSub":"無料の相棒は後で選ぶ","safety":"V-Bucksは一切使いません。すべての入手は価格0で送信され、残高が減ったらすぐに停止します。","balanceNow":"残高: {b} V-Bucks","quickToggle":"次回からこの画面をスキップ","start":{"other":"{n}個を入手"},"cancel":"キャンセル","stop":"停止","tagBundle":"バンドル","stSidekick":"相棒を入手中…","stBundles":{"other":"バンドル{n}個を開封中…"},"stChecking":"バンドルで入手したものを確認中…","stSingles":{"other":"残り{n}個を入手中…"},"stItems":{"other":"{n}個を入手中…"},"stStopping":"処理中の入手が終わったら停止します…","doneTitle":"すべてあなたのもの","stoppedTitle":"停止しました","nothingClaimed":"入手したものはありません","nothingTitle":"入手できるものはもうありません","nothingText":"今日のショップの無料アイテムはすべて所持しています。ショップが00:00 UTCに更新されると新しいアイテムが届きます。","doneText":{"other":"{t}でコスメティック{n}個をロッカーに追加しました。"},"stoppedText":{"other":"停止までに{n}個を入手しました。"},"secs":"{s}秒","mins":"{m}分{s}秒","chipVbOk":"V-Bucksはそのまま: {b}","chipVbBad":"V-Bucksが変化: {a} → {b}","chipWorth":"約{v} V-Bucks分のアイテム","chipClaimed":{"other":"入手{n}回"},"chipBundle":{"other":"バンドルに含まれていた{n}個"},"chipSkipped":{"other":"スキップ{n}個"},"chipLeftOut":{"other":"除外した{n}個"},"whySkipped":"スキップした理由","leftOutList":"除外したもの","notCovered":{"other":"海賊ショップの対象外のアイテム{n}個"},"rmPack":"リアルマネーのパック","vbPrice":"{v} V-Bucks","rExcluded":"海賊ショップの対象外","rNoLongerFree":"無料ではなくなりました","rSidekickUsed":"無料の相棒はすでに使用済み","rSidekick":"相棒を含む","rSlow":"Epicから速度を落とすよう求められました。1分後にもう一度お試しください","saveList":"リストを保存","retry":{"other":"{n}個を再試行"},"reviewAgain":"残りを入手","done":"完了","close":"閉じる","tryAgain":"もう一度","gotIt":"わかりました","safetyTitle":"安全停止","safetyText":"V-Bucksが{a}から{b}に変わったため、まとめて入手をすぐに停止しました。もう一度実行する前に、Epicで残高と購入履歴を確認してください。","snagTitle":"問題が発生しました","snagText":"{msg}。料金は一切かかっていません。","signinTitle":"先にサインインしてください","signinText":"fortnite.comにサインインしていません。海賊ショップが有効なアカウントでサインインしてから、もう一度お試しください。","signin":"サインイン","noTokenTitle":"海賊ショップが有効ではありません","noTokenText":"このアカウントのアイテムはまだV-Bucksが必要なため、何も入手していません。海賊ショップアクセストークンを持っている場合は、先にゲーム内のFortniteアイテムショップで有効化してください。有効化から14日間使えます。","notActiveTitle":"トークンがまだ有効化されていません","notActiveText":"海賊ショップアクセストークンを持っていますが、まだ有効化されていません。ゲーム内のFortniteアイテムショップで有効化してから、ここに戻ってきてください。有効期間は14日なので、準備ができたら有効化しましょう。2026年のトークンは12月16日 00:00 GMTまでに有効化する必要があります。","errShop":"アイテムショップを読み込めませんでした","errCsrf":"Epicからセキュリティトークンを取得できませんでした","errHttp":"Epicのサイトがエラーを返しました(HTTP {s})","errSafety":"安全停止: V-Bucksが{a}から{b}に変わりました","listHeader":"{d}の海賊ショップでの入手","pTitle":"海賊ショップ まとめて入手","pOpen":"アイテムショップを開く","pClaim":"無料アイテムをまとめて入手","pWaiting":{"other":"無料アイテム{n}個が待っています"},"pUntil":"{d}まで有効","pNoShop":"サインインした状態でfortnite.com/item-shopを開いてください。ボタンはV-Bucks購入の横に表示されます。","pSignin":"fortnite.comにサインインすると無料アイテムが表示されます。","pNoToken":"このアカウントでは海賊ショップがまだ有効ではありません。","pNotActive":"トークンはありますが未有効化です。ゲーム内アイテムショップで有効化してください。","pAllDone":"今日のショップの無料アイテムはすべて所持しています。","pRunning":"入手中…","pReminder":"ショップ更新時に通知する","pReminderSub":"海賊ショップが有効な間、毎日00:00 UTC","pQuick":"選択画面をスキップ","pLifetime":{"other":"これまでに{n}個を入手、約{v} V-Bucks分"},"pUpdate":"バージョン{v}が利用可能です","pUpdateGet":"入手","pFooter":"制作: 2xFlaw · Chest Counter","nTitle":"アイテムショップが更新されました","nText":"海賊ショップが有効な間、新しいアイテムは無料です。クリックして入手しましょう。"}};
const CC_CSS = "/* Chest Counter Claim All: on-page styles. Everything is scoped to #cc-* ids so the shop is untouched. */\n:root{--cc-acc:#d00080;--cc-acc2:#ff4fa3;--cc-amber:#ffb000;--cc-mint:#3ddc97;--cc-ink:#fff4fb;--cc-mute:#c9b3c8;--cc-bg:#160b1b;--cc-bg2:#24112b;--cc-line:rgba(255,255,255,.12);--cc-head:inherit;--cc-head-fvs:normal}\n#cc-hero-wrap{display:flex;align-items:center;margin-left:auto;order:0}\n@media (min-width:768px){#cc-hero-wrap{order:1}}\n#cc-hero{all:unset;box-sizing:border-box;display:inline-flex;align-items:center;gap:9px;padding:10px 14px 10px 10px;border-radius:12px;cursor:pointer;font:700 14px/1 var(--cc-head),system-ui,sans-serif;font-variation-settings:var(--cc-head-fvs);letter-spacing:.01em;color:#fff;background:linear-gradient(135deg,#ff4fa3 0%,#d00080 55%,#8a0a5c 100%);box-shadow:inset 0 0 0 1px rgba(255,255,255,.28),0 6px 20px rgba(208,0,128,.35);transition:transform .15s,box-shadow .2s,filter .2s;white-space:nowrap}\n#cc-hero:hover{transform:translateY(-1px);box-shadow:inset 0 0 0 1px rgba(255,255,255,.4),0 10px 28px rgba(208,0,128,.5)}\n#cc-hero:active{transform:translateY(0) scale(.98)}\n#cc-hero:focus-visible{outline:3px solid #ffb000;outline-offset:3px}\n#cc-hero.is-running{filter:saturate(.7);cursor:progress}\n#cc-hero.is-done{background:linear-gradient(135deg,#2a1631,#3a1d44);box-shadow:inset 0 0 0 1px rgba(255,255,255,.18)}\n#cc-hero.is-blocked{background:linear-gradient(135deg,#3a1d44,#5b2266)}\n.cc-logo{display:inline-block;width:22px;height:22px;background:var(--cc-logo) center/contain no-repeat;flex:none;filter:drop-shadow(0 1px 2px rgba(0,0,0,.35))}\n.cc-hb-n{display:inline-flex;align-items:center;justify-content:center;min-width:24px;height:22px;padding:0 7px;border-radius:999px;background:#fff;color:#a0005f;font:800 12px/1 var(--cc-head),system-ui,sans-serif;font-variation-settings:var(--cc-head-fvs)}\n.cc-hb-n[hidden]{display:none}\n.cc-hb-n svg{width:14px;height:14px}\n#cc-fab{all:unset;box-sizing:border-box;position:fixed;right:22px;bottom:22px;z-index:2147483000;width:60px;height:60px;border-radius:20px;display:flex;align-items:center;justify-content:center;cursor:pointer;background:linear-gradient(135deg,#ff4fa3,#d00080 60%,#8a0a5c);box-shadow:0 12px 34px rgba(208,0,128,.5),inset 0 0 0 1px rgba(255,255,255,.3);transform:translateY(120px) scale(.6);opacity:0;transition:transform .35s cubic-bezier(.34,1.56,.64,1),opacity .25s}\n#cc-fab.is-on{transform:none;opacity:1}\n#cc-fab .cc-logo{width:36px;height:36px}\n#cc-fab:focus-visible{outline:3px solid #ffb000;outline-offset:3px}\n.cc-fab-n{position:absolute;top:-6px;right:-6px;min-width:22px;height:22px;padding:0 6px;border-radius:999px;background:#ffb000;color:#2b1400;font:800 12px/22px system-ui,sans-serif;text-align:center;box-shadow:0 0 0 3px #160b1b}\n.cc-fab-tip{position:absolute;right:72px;padding:8px 12px;border-radius:10px;background:#160b1b;color:#fff;font:700 13px/1 system-ui,sans-serif;white-space:nowrap;opacity:0;transform:translateX(6px);transition:.2s;pointer-events:none;box-shadow:0 8px 24px rgba(0,0,0,.4)}\n#cc-fab:hover .cc-fab-tip,#cc-fab:focus-visible .cc-fab-tip{opacity:1;transform:none}\n#cc-root{position:fixed;inset:0;z-index:2147483600;display:flex;align-items:center;justify-content:center;font:400 14px/1.45 Inter,system-ui,-apple-system,\"Segoe UI\",sans-serif;color:var(--cc-ink);-webkit-font-smoothing:antialiased}\n#cc-root *{box-sizing:border-box}\n.cc-scrim{position:absolute;inset:0;background:rgba(10,3,14,.6);backdrop-filter:blur(14px) saturate(1.2);opacity:0;transition:opacity .3s}\n#cc-root.is-open .cc-scrim{opacity:1}\n.cc-modal{position:relative;width:min(560px,calc(100vw - 24px));max-height:calc(100vh - 32px);display:flex;flex-direction:column;border-radius:28px;overflow:hidden;background:linear-gradient(180deg,#2a1232 0%,#1a0c20 46%,#130818 100%);box-shadow:0 30px 90px rgba(0,0,0,.6),inset 0 0 0 1px rgba(255,255,255,.1);transform:translateY(16px) scale(.97);opacity:0;transition:transform .35s cubic-bezier(.2,.8,.3,1),opacity .3s}\n#cc-root.is-open .cc-modal{transform:none;opacity:1}\n.cc-m-top{position:relative;padding:26px 26px 14px;text-align:center;overflow:hidden;flex:none}\n.cc-blob{position:absolute;border-radius:50%;filter:blur(40px);opacity:.55;pointer-events:none}\n.cc-blob.b1{width:240px;height:240px;left:-80px;top:-120px;background:#d00080}\n.cc-blob.b2{width:200px;height:200px;right:-70px;top:-60px;background:#7b2cbf}\n.cc-blob.b3{width:160px;height:160px;left:40%;top:40px;background:#ffb000;opacity:.18}\n.cc-demo{position:relative;display:inline-block;margin-bottom:10px;padding:5px 10px;border-radius:999px;background:#ffb000;color:#2b1400;font:800 11px/1.2 system-ui,sans-serif;letter-spacing:.04em;text-transform:uppercase}\n.cc-m-logo{position:relative;width:76px;height:76px;margin:0 auto 6px}\n.cc-m-logo .cc-logo{width:76px;height:76px;filter:drop-shadow(0 8px 18px rgba(0,0,0,.45))}\n#cc-root:not(.is-done):not(.is-review) .cc-m-logo .cc-logo{animation:cc-bob 1.6s ease-in-out infinite}\n.cc-m-tag{position:absolute;right:-6px;bottom:-2px;width:30px;height:30px;border-radius:50%;display:flex;align-items:center;justify-content:center;background:#ffb000;color:#2b1400;box-shadow:0 0 0 4px #24112b}\n.cc-m-tag svg{width:18px;height:18px}\n.cc-m-count{position:relative;font:400 56px/1 var(--cc-head),Impact,\"Arial Black\",sans-serif;font-style:italic;letter-spacing:-.01em;text-transform:uppercase;font-variation-settings:var(--cc-head-fvs)}\n.cc-m-count[hidden]{display:none}\n.cc-m-count b{font-weight:inherit}\n.cc-total{font-size:22px;color:var(--cc-mute);margin-left:6px}\n.cc-m-title{position:relative;margin-top:2px;font:400 28px/1.1 var(--cc-head),Impact,\"Arial Black\",sans-serif;font-style:italic;text-transform:uppercase;letter-spacing:.005em;font-variation-settings:var(--cc-head-fvs)}\n.cc-status{position:relative;margin:8px auto 0;max-width:440px;color:var(--cc-mute);font-size:13.5px;min-height:20px}\n.cc-bar{position:relative;height:8px;margin:14px auto 0;max-width:360px;border-radius:99px;background:rgba(255,255,255,.1);overflow:hidden}\n.cc-bar[hidden]{display:none}\n.cc-bar i{display:block;height:100%;width:0;border-radius:inherit;background:repeating-linear-gradient(-45deg,#ffb000 0 10px,#ffd23f 10px 20px);background-size:28px 28px;animation:cc-stripes .8s linear infinite;transition:width .35s ease}\n.cc-toast-row{position:relative;min-height:0;display:flex;justify-content:center;margin-top:10px}\n.cc-toast{display:inline-flex;align-items:center;gap:8px;max-width:100%;padding:4px 10px 4px 4px;border-radius:999px;background:rgba(255,255,255,.08);box-shadow:inset 0 0 0 1px rgba(255,255,255,.1);animation:cc-pop .35s cubic-bezier(.34,1.56,.64,1)}\n.cc-toast-img{width:26px;height:26px;border-radius:50%;overflow:hidden;background:linear-gradient(135deg,var(--c1),var(--c2));flex:none}\n.cc-toast-img img{width:100%;height:100%;object-fit:cover}\n.cc-toast-t{font-weight:700;font-size:12.5px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}\n.cc-toast-ok{display:inline-flex;width:18px;height:18px;border-radius:50%;background:var(--cc-mint);color:#062b1a;align-items:center;justify-content:center;flex:none}\n.cc-toast-ok svg{width:12px;height:12px}\n.cc-summary:empty{display:none}\n.cc-chips{display:flex;flex-wrap:wrap;justify-content:center;gap:7px;margin-top:14px}\n.cc-chip{display:inline-flex;align-items:center;gap:6px;padding:6px 10px;border-radius:999px;background:rgba(255,255,255,.07);box-shadow:inset 0 0 0 1px rgba(255,255,255,.1);font-size:12.5px;font-weight:600}\n.cc-chip svg{width:15px;height:15px}\n.cc-chip i{width:8px;height:8px;border-radius:50%;background:currentColor}\n.cc-chip-ok{color:var(--cc-mint)}\n.cc-chip-bad{color:#ff6b6b}\n.cc-c-claimed i{background:var(--cc-acc2)}\n.cc-c-bundle i{background:#b892ff}\n.cc-c-failed i{background:#ff6b6b}\n.cc-c-left i{background:var(--cc-mute)}\n.cc-c-worth{color:var(--cc-amber)}\n.cc-more{margin:10px auto 0;max-width:460px;text-align:left;border-radius:14px;background:rgba(255,255,255,.05);box-shadow:inset 0 0 0 1px rgba(255,255,255,.08)}\n.cc-more summary{cursor:pointer;padding:9px 14px;font-size:12.5px;font-weight:700;color:var(--cc-mute)}\n.cc-more ul{list-style:none;margin:0;padding:0 14px 10px;max-height:150px;overflow:auto}\n.cc-more li{display:flex;justify-content:space-between;gap:12px;padding:5px 0;border-top:1px solid rgba(255,255,255,.06);font-size:12.5px}\n.cc-more li span{color:var(--cc-mute);text-align:right}\n.cc-body{padding:4px 22px 6px;overflow:auto;flex:1 1 auto;min-height:0}\n.cc-body[hidden]{display:none}\n.cc-sec-h{display:flex;justify-content:space-between;align-items:baseline;gap:10px;margin:2px 0 10px;font-size:13px}\n.cc-sec-h span{color:var(--cc-mute);font-size:12.5px}\n.cc-link{all:unset;cursor:pointer;color:var(--cc-acc2);font-weight:700}\n.cc-link:hover{text-decoration:underline}\n.cc-link:focus-visible{outline:2px solid var(--cc-amber);outline-offset:2px;border-radius:4px}\n.cc-cats{display:grid;grid-template-columns:repeat(auto-fill,minmax(200px,1fr));gap:8px}\n.cc-cat{all:unset;box-sizing:border-box;display:flex;align-items:center;gap:8px;padding:8px 10px 8px 8px;border-radius:14px;cursor:pointer;background:rgba(255,255,255,.04);box-shadow:inset 0 0 0 1px rgba(255,255,255,.1);color:var(--cc-mute);transition:background .15s,box-shadow .15s,color .15s,opacity .15s;opacity:.65}\n.cc-cat.is-on{background:rgba(208,0,128,.16);box-shadow:inset 0 0 0 1.5px var(--cc-acc2);color:var(--cc-ink);opacity:1}\n.cc-cat:focus-visible{outline:2px solid var(--cc-amber);outline-offset:2px}\n.cc-cat-imgs{display:flex;flex:none}\n.cc-cat-imgs img,.cc-cat-imgs .cc-tname{width:26px;height:26px;border-radius:8px;object-fit:cover;background:#2c1733;box-shadow:0 0 0 2px #1d0e23;font-size:0}\n.cc-cat-imgs>*+*{margin-left:-10px}\n.cc-cat-t{flex:1;min-width:0;font-weight:700;font-size:12.5px;line-height:1.2;overflow-wrap:break-word}\n.cc-cat b{flex:none;margin-left:2px;font:800 13px/1 system-ui,sans-serif}\n.cc-sk{margin:14px 0 0;padding:12px;border:0;border-radius:16px;background:rgba(255,176,0,.08);box-shadow:inset 0 0 0 1px rgba(255,176,0,.35)}\n.cc-sk legend{float:left;width:100%;display:flex;align-items:center;gap:8px;padding:0;font-weight:800;font-size:13.5px;color:var(--cc-amber)}\n.cc-sk p{clear:both;margin:6px 0 10px;font-size:12.5px;color:var(--cc-mute)}\n.cc-ic{display:inline-flex;width:18px;height:18px;flex:none}\n.cc-ic svg{width:100%;height:100%}\n.cc-sk-list{display:grid;grid-template-columns:repeat(auto-fill,minmax(150px,1fr));gap:8px}\n.cc-sk-opt{position:relative;display:flex;align-items:center;gap:8px;padding:7px;border-radius:12px;cursor:pointer;background:rgba(0,0,0,.2);box-shadow:inset 0 0 0 1px rgba(255,255,255,.1)}\n.cc-sk-opt:has(input:checked){box-shadow:inset 0 0 0 2px var(--cc-amber);background:rgba(255,176,0,.12)}\n.cc-sk-opt:has(input:focus-visible){outline:2px solid var(--cc-amber);outline-offset:2px}\n.cc-sk-opt input{position:absolute;opacity:0;pointer-events:none}\n.cc-sk-img{width:38px;height:38px;border-radius:10px;overflow:hidden;flex:none;background:linear-gradient(135deg,var(--c1,#3a2140),var(--c2,#1b0f20));display:flex;align-items:center;justify-content:center;color:var(--cc-mute)}\n.cc-sk-img img{width:100%;height:100%;object-fit:cover}\n.cc-sk-img svg{width:22px;height:22px}\n.cc-sk-t{display:flex;flex-direction:column;min-width:0}\n.cc-sk-t b{font-size:12.5px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}\n.cc-sk-t small{font-size:11px;color:var(--cc-mute)}\n.cc-safe{display:flex;gap:8px;align-items:flex-start;margin:14px 0 6px;padding:10px 12px;border-radius:14px;background:rgba(61,220,151,.08);box-shadow:inset 0 0 0 1px rgba(61,220,151,.3);font-size:12.5px;color:#cdeedd}\n.cc-safe .cc-ic{color:var(--cc-mint)}\n.cc-safe b{color:#fff;white-space:nowrap}\n.cc-quick{display:flex;align-items:center;gap:8px;margin:8px 2px 4px;font-size:12.5px;color:var(--cc-mute);cursor:pointer}\n.cc-quick input{accent-color:#d00080;width:16px;height:16px}\n.cc-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(52px,1fr));gap:7px;padding:6px 22px 12px;overflow:auto;flex:1 1 auto;min-height:0;max-height:330px}\n.cc-grid[hidden]{display:none}\n.cc-tile{position:relative;aspect-ratio:1;border-radius:12px;background:linear-gradient(160deg,var(--c1),var(--c2));opacity:.28;filter:grayscale(.6);transition:opacity .25s,filter .25s,box-shadow .25s;animation:cc-in .4s both;animation-delay:var(--d)}\n.cc-tile img{width:100%;height:100%;object-fit:cover;border-radius:inherit}\n.cc-tile img.is-broken,.cc-cat-imgs img.is-broken,.cc-sk-img img.is-broken,.cc-toast-img img.is-broken{visibility:hidden}\n.cc-tname{position:absolute;inset:4px;display:flex;align-items:center;justify-content:center;text-align:center;font-size:9px;font-weight:700;overflow:hidden}\n.cc-tag{position:absolute;left:3px;top:3px;padding:1px 4px;border-radius:4px;background:rgba(0,0,0,.65);color:#fff;font:800 7.5px/1.3 system-ui,sans-serif;letter-spacing:.04em;text-transform:uppercase}\n.cc-tile.is-sk{box-shadow:inset 0 0 0 2px var(--cc-amber)}\n.cc-tile.s-working{opacity:.9;filter:none;box-shadow:0 0 0 2px var(--cc-acc2),0 0 18px rgba(255,79,163,.6);animation:cc-pulse 1s ease-in-out infinite}\n.cc-tile.s-claimed,.cc-tile.s-bundle{opacity:1;filter:none;box-shadow:inset 0 0 0 2px rgba(255,176,0,.9)}\n.cc-tile.s-bundle{box-shadow:inset 0 0 0 2px #b892ff}\n.cc-tile.s-failed{opacity:.75;filter:grayscale(1);box-shadow:inset 0 0 0 2px #ff6b6b}\n.cc-badge{position:absolute;right:-4px;bottom:-4px;width:18px;height:18px;border-radius:50%;display:none;align-items:center;justify-content:center;background:var(--cc-amber);color:#2b1400;box-shadow:0 0 0 2px #1a0c20}\n.cc-badge svg{width:11px;height:11px}\n.s-claimed .cc-badge,.s-bundle .cc-badge,.s-failed .cc-badge{display:flex}\n.s-bundle .cc-badge{background:#b892ff;color:#1d0b3a}\n.s-failed .cc-badge{background:#ff6b6b;color:#fff}\n.cc-spark{position:absolute;left:50%;top:50%;width:6px;height:6px;margin:-3px;border-radius:50%;pointer-events:none;animation:cc-spark .7s ease-out forwards}\n.cc-m-foot{display:flex;justify-content:center;flex-wrap:wrap;gap:10px;padding:14px 22px 20px;flex:none;border-top:1px solid rgba(255,255,255,.06)}\n.cc-btn{all:unset;box-sizing:border-box;display:inline-flex;align-items:center;gap:8px;padding:11px 18px;border-radius:14px;cursor:pointer;font:700 13.5px/1 Inter,system-ui,sans-serif;color:var(--cc-ink);background:rgba(255,255,255,.08);box-shadow:inset 0 0 0 1px rgba(255,255,255,.14);transition:background .15s,transform .1s}\n.cc-btn:hover{background:rgba(255,255,255,.14)}\n.cc-btn:active{transform:scale(.98)}\n.cc-btn:focus-visible{outline:3px solid var(--cc-amber);outline-offset:2px}\n.cc-btn[disabled]{opacity:.45;cursor:default}\n.cc-btn kbd{font:700 10.5px/1 system-ui,sans-serif;padding:3px 5px;border-radius:5px;background:rgba(255,255,255,.12);color:var(--cc-mute)}\n.cc-primary{background:linear-gradient(135deg,#ff4fa3,#d00080);box-shadow:0 8px 22px rgba(208,0,128,.4),inset 0 0 0 1px rgba(255,255,255,.25);color:#fff}\n.cc-primary:hover{background:linear-gradient(135deg,#ff66b0,#e0058c)}\n.cc-confetti{position:fixed;inset:0;width:100vw;height:100vh;pointer-events:none}\n@keyframes cc-bob{50%{transform:translateY(-6px) rotate(-3deg)}}\n@keyframes cc-stripes{to{background-position:28px 0}}\n@keyframes cc-pop{from{transform:scale(.7);opacity:0}}\n@keyframes cc-in{from{transform:scale(.6);opacity:0}}\n@keyframes cc-pulse{50%{transform:scale(.93)}}\n@keyframes cc-spark{to{transform:translate(var(--x),var(--y)) scale(.2);opacity:0}}\n@media (prefers-reduced-motion:reduce){#cc-root *,#cc-hero,#cc-fab{animation:none!important;transition:none!important}}\n@media (max-width:520px){.cc-m-count{font-size:44px}.cc-m-title{font-size:23px}.cc-cats,.cc-sk-list{grid-template-columns:1fr 1fr}}";
const CC_LOGO = "data:image/webp;base64,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";
// Chest Counter Claim All: claims every free Item Shop offer while the Pirate Shop is active.
// Runs in the fortnite.com page. Every claim is sent with expectedTotalPrice 0, so Epic refuses
// anything that isn't actually free, and the run stops if the V-Bucks balance ever drops.
// Built on the approach of Napzeee's pirate-token-claim-all extension (MIT), see LICENSE.
/* global CC_STRINGS, CC_CSS, CC_LOGO, CC_VERSION, CC_PLATFORM */
(() => {
  if (window.__ccClaimAll) return;
  window.__ccClaimAll = true;

  const VERSION = typeof CC_VERSION === 'string' ? CC_VERSION : 'dev';
  const PLATFORM = typeof CC_PLATFORM === 'string' ? CC_PLATFORM : 'dev';
  const SHOP_RE = /^\/(?:[a-z]{2}(?:-[a-z]{2,4})?\/)?item-shop\/?$/i;
  const ROUTE = 'routes/item-shop._index';
  const BASE_CONCURRENCY = 4;
  const BALANCE_EVERY = 25;
  const SETTINGS_KEY = 'ccClaim.settings';
  const STATS_KEY = 'ccClaim.stats';
  const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
  const nativeFetch = window.fetch.bind(window);
  const reduceMotion = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const DEMO = new URL(location.href).searchParams.has('ccdemo');

  // ---------- storage ----------
  const readJSON = (k, d) => { try { return Object.assign({}, d, JSON.parse(localStorage.getItem(k) || '{}')); } catch (_) { return { ...d }; } };
  const writeJSON = (k, v) => { try { localStorage.setItem(k, JSON.stringify(v)); } catch (_) { /* private mode */ } };
  const settings = () => readJSON(SETTINGS_KEY, { quick: false, lang: '' });
  const saveSettings = (patch) => writeJSON(SETTINGS_KEY, { ...settings(), ...patch });

  // ---------- language ----------
  const LANGS = ['en', 'es', 'pt', 'ru', 'fr', 'de', 'it', 'pl', 'tr', 'ja'];
  function pickLang() {
    const want = [settings().lang, new URL(location.href).searchParams.get('lang'), document.documentElement.lang, navigator.language];
    for (const w of want) {
      const base = String(w || '').toLowerCase().split(/[-_]/)[0];
      if (LANGS.includes(base)) return base;
    }
    return 'en';
  }
  let lang = pickLang();
  const S = typeof CC_STRINGS === 'object' ? CC_STRINGS : { en: {} };
  const fmtN = (n) => Number(n || 0).toLocaleString(lang === 'en' ? 'en-GB' : lang);
  function T(key, vars = {}) {
    let v = (S[lang] && S[lang][key]) ?? S.en[key] ?? key;
    if (v && typeof v === 'object') {
      const n = Number(vars.n ?? 0);
      let form = 'other';
      try { form = new Intl.PluralRules(lang).select(n); } catch (_) { /* old browser */ }
      const en = S.en[key] && typeof S.en[key] === 'object' ? S.en[key] : {};
      v = v[form] ?? v.other ?? en[form] ?? en.other ?? key;
    }
    return String(v).replace(/\{(\w+)\}/g, (_, k) => (k in vars ? (typeof vars[k] === 'number' ? fmtN(vars[k]) : vars[k]) : `{${k}}`));
  }

  // ---------- Epic's turbo-stream (.data) decoder ----------
  function decodeTS(text) {
    const arr = JSON.parse(text);
    const SPECIAL = { '-1': undefined, '-2': null, '-3': NaN, '-4': Infinity, '-5': -Infinity, '-6': -0, '-7': undefined };
    const cache = new Map();
    const h = (i) => {
      if (typeof i !== 'number') return i;
      if (i < 0) return SPECIAL[i];
      if (cache.has(i)) return cache.get(i);
      const v = arr[i];
      if (Array.isArray(v)) {
        if (typeof v[0] === 'string') {
          if (v[0] === 'D') { const d = new Date(v[1]).toISOString(); cache.set(i, d); return d; }
          if (v[0] === 'Z') { const o = {}; cache.set(i, o); return o; }
          return v;
        }
        const out = []; cache.set(i, out);
        for (const x of v) out.push(h(x));
        return out;
      }
      if (v && typeof v === 'object') {
        const out = {}; cache.set(i, out);
        for (const k of Object.keys(v)) out[arr[+k.slice(1)]] = h(v[k]);
        return out;
      }
      return v;
    };
    return h(0);
  }

  // Epic rate-limits the shop data now and then, so back off and try again before giving up
  async function getData(url, tries = 4) {
    for (let a = 0; ; a++) {
      let r = null;
      try { r = await nativeFetch(url, { credentials: 'include' }); } catch (e) { if (a >= tries - 1) throw e; }
      if (r && r.ok) return decodeTS(await r.text());
      const retry = !r || r.status === 429 || r.status >= 500;
      if (!retry || a >= tries - 1) throw new Error(T('errHttp', { s: r ? r.status : 0 }));
      await sleep(1200 * 2 ** a + Math.random() * 400);
    }
  }

  function collectOffers(root) {
    const offers = new Map();
    const seen = new Set();
    (function walk(o, depth) {
      if (!o || depth > 16 || typeof o !== 'object' || seen.has(o)) return;
      seen.add(o);
      if (typeof o.offerId === 'string' && o.pricing) { if (!offers.has(o.offerId)) offers.set(o.offerId, o); return; }
      for (const k of Object.keys(o)) walk(o[k], depth + 1);
    })(root, 0);
    return [...offers.values()];
  }

  // demo mode makes every V-Bucks offer look free and never sends a claim
  const demoOwned = new Set();
  function demoShop(offers) {
    return offers.map((o) => {
      if (o.pricing?.currencyType !== 'MtxCurrency') return o;
      return { ...o, ownershipStatus: demoOwned.has(o.offerId) ? 'owned' : o.ownershipStatus, pricing: { ...o.pricing, finalPrice: 0 } };
    });
  }

  // The "View all" Jam Tracks page lists every track; the main shop only shows some of them
  const JAM_ROUTE = 'routes/item-shop.jam-tracks._index';
  async function loadJamTracks() {
    try {
      const d = await getData(`/item-shop/jam-tracks.data?_routes=${encodeURIComponent(JAM_ROUTE)}`);
      const list = d?.[JAM_ROUTE]?.data?.jamTracks;
      if (!Array.isArray(list)) return [];
      return list.filter((t) => t?.forSale?.offerId && t.forSale.pricing).map((t) => ({
        ...t.forSale,
        assetType: t.assetType,
        title: t.title,
        artist: t.artist,
        ownershipStatus: t.ownershipStatus,
        image: t.media?.albumArtUrl ? { xs: t.media.albumArtUrl } : undefined,
      }));
    } catch (_) { return []; }   // never block the main claim run if this page fails
  }

  async function loadShop({ fallback = false } = {}) {
    let d;
    try { d = await getData(`/item-shop.data?_routes=${encodeURIComponent(ROUTE)}`); }
    catch (e) { const p = fallback && pageShop(); if (p) return p; throw e; }   // the copy already on the page is fine for a first look
    const data = d?.[ROUTE]?.data;
    if (!data) throw new Error(T('errShop'));
    const offers = collectOffers(data);
    const have = new Set(offers.map((o) => o.offerId));
    for (const j of await loadJamTracks()) if (!have.has(j.offerId)) offers.push(j);
    if (DEMO) return { loggedIn: true, offers: demoShop(offers) };
    return { loggedIn: data.isLoggedIn !== false, offers };
  }

  async function getCsrf() {
    if (DEMO) return 'demo';
    const d = await getData('/item-shop/resources/csrf-token.data?_routes=routes%2Fitem-shop.resources.csrf-token');
    const t = d?.['routes/item-shop.resources.csrf-token']?.data?.csrfToken;
    if (!t) throw new Error(T('errCsrf'));
    return t;
  }

  async function getBalance() {
    if (DEMO) return 5100;
    try {
      const d = await getData('/item-shop/resources/get-profile.data?_routes=routes%2Fitem-shop.resources.get-profile');
      const b = d?.['routes/item-shop.resources.get-profile']?.data?.fortniteProfile?.vbucksWallet?.balance;
      return typeof b === 'number' ? b : null;
    } catch (_) { return null; }
  }

  // ---------- offer helpers ----------
  const isFree = (o) => o.pricing?.finalPrice === 0 && o.pricing?.currencyType === 'MtxCurrency';
  const isBundle = (o) => o.assetType === 'dynamicbundle' || /bundle/i.test(o.offerMainType || '');
  const owned = (o) => o.ownershipStatus === 'owned';
  const excluded = (o) => o.delilahExcluded === true;   // Epic's own "not in the Pirate Shop" flag
  const grants = (o) => o.ownershipCalculationData?.itemGrantTemplateIds || [];
  const isSidekick = (o) => o.assetType === 'companion' || grants(o).some((g) => /^CosmeticMimosa:/i.test(g));
  const skIds = (o) => grants(o).filter((g) => /^CosmeticMimosa:/i.test(g)).map((g) => g.toLowerCase());
  // Sidekicks are customizable, so Epic sells them again ("Purchase more") and the shop can still call them unowned.
  // Every sidekick is free once with the Pirate Shop: claim each one once and skip any you already have.
  let bought = { offers: new Set(), items: new Set() };
  async function loadBought() {
    if (DEMO) return bought;
    const read = (fp) => fp?.profile?.stats?.attributes?.mtx_purchase_history?.purchases;
    let list = null;
    try {
      const d = await getData('/item-shop/resources/get-profile.data?_routes=routes%2Fitem-shop.resources.get-profile');
      list = read(d?.['routes/item-shop.resources.get-profile']?.data?.fortniteProfile);
    } catch (_) { /* fall back to the page's copy */ }
    if (!Array.isArray(list)) { try { list = read(window.__reactRouterDataRouter?.state?.loaderData?.root?.fortniteProfile); } catch (_) { list = null; } }
    if (!Array.isArray(list)) return bought;   // keep what we knew; the in-run guard still stops doubles
    const offers = new Set(), items = new Set();
    for (const p of list) {
      if (p?.offerId) offers.add(p.offerId);
      for (const l of p?.lootResult || []) if (l?.itemType) items.add(String(l.itemType).toLowerCase());
    }
    bought = { offers, items };
    return bought;
  }
  // with the Pirate Shop active every V-Bucks item is free, so a sidekick back at full price is one you already own
  const fullPriceSk = (o) => o.pricing?.currencyType === 'MtxCurrency' && o.pricing.finalPrice > 0 && !excluded(o);
  const haveSidekick = (o) => isSidekick(o) && (bought.offers.has(o.offerId) || skIds(o).some((id) => bought.items.has(id)) || fullPriceSk(o));
  const has = (o) => owned(o) || haveSidekick(o);
  const hasToken = (offers) => {
    const vb = offers.filter((o) => o.pricing?.currencyType === 'MtxCurrency' && !excluded(o));
    return vb.length > 0 && vb.filter(isFree).length > vb.length / 2;
  };
  // same resized CDN Epic's own shop uses; the signed ucs-images links often fail under the page's COEP
  const thumb = (o) => {
    const im = Array.isArray(o.image) ? o.image[0] : o.image;
    const cosmo = im?.cosmo?.url || (/\/\/cosmo\.[^/]+\/v1\//.test(im?.xl || '') ? im.xl : '');
    if (cosmo) return cosmo.replace(/^(https:\/\/[^/]+)\/(v1\/)/, '$1/cdn-cgi/image/width=256,format=auto/$2');
    return im?.xs || im?.sm || im?.md || im?.tall || im?.wide || '';
  };
  const CATS = [
    ['bundles', (o) => isBundle(o)],
    ['sidekicks', (o) => isSidekick(o)],
    ['outfits', (o) => o.assetType === 'outfit'],
    ['emotes', (o) => o.assetType === 'emote'],
    ['pickaxes', (o) => o.assetType === 'pickaxe'],
    ['backblings', (o) => o.assetType === 'backbling'],
    ['gliders', (o) => o.assetType === 'glider'],
    ['wraps', (o) => o.assetType === 'wrap'],
    ['contrails', (o) => o.assetType === 'contrail'],
    ['jam', (o) => o.assetType === 'jamtrack'],
    ['instruments', (o) => /^(guitar|bass|drums|keyboard|keytar|microphone)$/.test(o.assetType)],
    ['cars', (o) => /^(wheels|turbo|trail|body|carbody|skin|decal|vehicle)$/.test(o.assetType) || /^Vehicle/i.test(o.offerMainType || '')],
    ['kicks', (o) => o.assetType === 'shoes'],
    ['other', () => true],
  ];
  const catOf = (o) => CATS.find(([, f]) => f(o))[0];

  // value of a set of offers without counting items twice (bundles overlap with singles)
  function worth(list) {
    const singles = list.filter((o) => !isBundle(o));
    let total = singles.reduce((a, o) => a + (o.pricing?.basePrice || 0), 0);
    for (const b of list.filter(isBundle)) {
      const set = new Set(grants(b));
      const inside = singles.filter((s) => grants(s).length && grants(s).every((g) => set.has(g)))
        .reduce((a, s) => a + (s.pricing?.basePrice || 0), 0);
      total += Math.max(0, (b.pricing?.basePrice || 0) - inside);
    }
    return total;
  }

  // ---------- claiming ----------
  const pace = { concurrency: BASE_CONCURRENCY, cooldownUntil: 0 };
  async function claim(offer, csrf) {
    if (DEMO) {
      await sleep(180 + Math.random() * 420);
      demoOwned.add(offer.offerId);
      return { result: 'claimed', granted: Math.max(1, grants(offer).length) };
    }
    for (let attempt = 0; attempt < 5; attempt++) {
      const wait = pace.cooldownUntil - Date.now();
      if (wait > 0) await sleep(wait);
      let r, d = null;
      try {
        r = await nativeFetch('/item-shop/purchase.data', {
          method: 'POST',
          credentials: 'include',
          body: new URLSearchParams({ csrfToken: csrf.value, offerId: offer.offerId, expectedTotalPrice: '0' }),
        });
        d = decodeTS(await r.text())?.data;
      } catch (e) { d = { success: false, error: String(e) }; }
      if (d?.success) {
        const items = (d.data?.notifications || []).flatMap((n) => n.lootResult?.items || []);
        return { result: 'claimed', granted: items.length };
      }
      const err = String(d?.error || `HTTP ${r?.status}`).replace(/^HttpError:\s*/, '');
      if (/already own/i.test(err)) return { result: 'owned' };
      if (/csrf/i.test(err) && attempt === 0) { csrf.value = await getCsrf(); continue; }
      if (/throttl|too many|rate limit|timeout|temporar/i.test(err) || r?.status === 429 || r?.status >= 500) {
        pace.concurrency = Math.max(1, Math.ceil(pace.concurrency / 2));   // ease off for the rest of the run
        pace.cooldownUntil = Date.now() + 1500 * (attempt + 1);
        continue;
      }
      if (/price|cost|expected/i.test(err)) return { result: 'failed', why: T('rNoLongerFree') };
      return { result: 'failed', why: err.slice(0, 140) };
    }
    return { result: 'failed', why: T('rSlow') };
  }

  async function pool(items, worker) {
    let i = 0;
    const lanes = new Set();
    const lane = async () => {
      while (i < items.length && !state.stopped && !state.halt) {
        if (lanes.size > pace.concurrency) break;   // shrink when Epic asks us to slow down
        await worker(items[i++]);
      }
    };
    while (i < items.length && !state.stopped && !state.halt) {
      while (lanes.size < pace.concurrency && i < items.length) {
        const p = lane().finally(() => lanes.delete(p));
        lanes.add(p);
      }
      await Promise.race(lanes);
    }
    await Promise.all(lanes);
  }

  const state = { running: false, stopped: false, halt: null, haltBal: null, remaining: null };

  function saveStats(claimedOffers, granted, value) {
    const s = readJSON(STATS_KEY, { runs: 0, offers: 0, cosmetics: 0, vbucks: 0, last: '' });
    writeJSON(STATS_KEY, { runs: s.runs + 1, offers: s.offers + claimedOffers, cosmetics: s.cosmetics + granted, vbucks: s.vbucks + value, last: new Date().toISOString() });
  }

  // review screen first (unless quick claim is on), then claim what was picked
  async function run({ only = null, forceReview = false } = {}) {
    if (state.running) return;
    state.running = true; state.stopped = false; state.halt = null; state.haltBal = null;
    pace.concurrency = BASE_CONCURRENCY; pace.cooldownUntil = 0;
    syncHero();
    const ui = openModal();
    try {
      ui.status(T('reading'));
      const [shop, csrf0, startBal] = await Promise.all([loadShop({ fallback: true }), getCsrf().catch((e) => e), getBalance()]);
      if (!shop.loggedIn) { ui.notice('signin'); return; }
      if (!hasToken(shop.offers)) { ui.notice(plunderChip() ? 'notactive' : 'notoken'); return; }
      if (csrf0 instanceof Error) throw csrf0;
      const csrf = { value: csrf0 };

      await loadBought();
      const unowned = shop.offers.filter((o) => !has(o));
      let free = unowned.filter((o) => isFree(o) && !excluded(o));
      if (only) free = free.filter((o) => only.has(o.offerId));
      const notCovered = unowned.filter((o) => !isFree(o) || excluded(o));
      if (!free.length) { ui.done({ nothing: true, notCovered, claimed: 0, viaBundle: 0, owned: 0, failed: [], granted: 0 }); state.remaining = 0; return; }

      let plan;
      if (only || (settings().quick && !forceReview)) {
        plan = { picked: free, sidekick: null, leftOut: [] };
      } else {
        state.running = false; syncHero();
        plan = await ui.review(free, { balance: startBal, until: tokenUntil() });
        if (!plan) { closeModal(); return; }
        state.running = true; syncHero();
      }
      const t0 = performance.now();
      const stats = { claimed: 0, viaBundle: 0, owned: 0, failed: [], granted: 0, claimedList: [], leftOut: plan.leftOut };
      const targets = [...plan.picked];
      if (!targets.length) { closeModal(); return; }
      ui.build(targets);

      let sinceCheck = 0;
      const checkBalance = async () => {
        if (startBal == null) return;
        const b = await getBalance();
        if (b != null && b < startBal) { state.halt = T('errSafety', { a: startBal, b }); state.haltBal = b; }
      };
      const count = () => ui.progress(stats.claimed + stats.viaBundle + stats.owned + stats.failed.length);
      // each sidekick only once per account: reserve it before claiming so two offers can't both buy it
      const takenSk = new Set();
      const work = async (o) => {
        const ids = skIds(o);
        if (ids.length && (haveSidekick(o) || ids.some((id) => takenSk.has(id)))) {
          stats.owned++; ui.tile(o.offerId, 'owned'); count(); return;
        }
        ids.forEach((id) => takenSk.add(id));
        ui.tile(o.offerId, 'working');
        const r = await claim(o, csrf);
        if (r.result === 'failed') ids.forEach((id) => takenSk.delete(id));   // let another offer with it try
        if (r.result === 'claimed') { stats.claimed++; stats.granted += r.granted; stats.claimedList.push(o); ui.toast(o); }
        else if (r.result === 'owned') stats.owned++;
        else stats.failed.push({ o, why: r.why });
        ui.tile(o.offerId, r.result, r.why);
        count();
        if (++sinceCheck >= BALANCE_EVERY) { sinceCheck = 0; await checkBalance(); }
      };

      const bundles = plan.picked.filter(isBundle).sort((a, b) => (b.pricing.basePrice || 0) - (a.pricing.basePrice || 0));
      let singles = plan.picked.filter((o) => !isBundle(o));
      // 1. bundles first, biggest first, so most items arrive in a few claims
      if (bundles.length && !state.stopped && !state.halt) {
        ui.status(T('stBundles', { n: bundles.length }));
        await pool(bundles, work);
      }
      // 2. see what the bundles already covered, then grab the rest one by one
      if (singles.length && !state.stopped && !state.halt) {
        if (bundles.length) {
          ui.status(T('stChecking'));
          const [shop2, midBal] = await Promise.all([loadShop().catch(() => null), getBalance(), loadBought()]);
          if (startBal != null && midBal != null && midBal < startBal) { state.halt = T('errSafety', { a: startBal, b: midBal }); state.haltBal = midBal; }
          // if Epic won't send the shop right now, claim the singles anyway: ones a bundle covered just come back as owned
          const fresh = new Map((shop2 ? shop2.offers : []).map((o) => [o.offerId, o]));
          const left = [];
          for (const o of singles) {
            const f = fresh.get(o.offerId);
            if ((f && owned(f)) || haveSidekick(o) || (isSidekick(o) && f && !isFree(f))) { stats.viaBundle++; ui.tile(o.offerId, 'bundle'); }   // an owned sidekick goes back to full price
            else if (f && !isFree(f)) { stats.failed.push({ o, why: T('rNoLongerFree') }); ui.tile(o.offerId, 'failed', T('rNoLongerFree')); }
            else left.push(o);
          }
          count();
          singles = left;
        }
        if (singles.length && !state.stopped && !state.halt) {
          ui.status(T(bundles.length ? 'stSingles' : 'stItems', { n: singles.length }));
          await pool(singles, work);
        }
      }

      const endBal = await getBalance();
      const halted = state.halt ? { a: startBal, b: endBal != null && endBal < startBal ? endBal : (state.haltBal ?? endBal) } : null;
      const done = stats.claimed + stats.viaBundle + stats.owned + stats.failed.length;
      state.remaining = stats.failed.length + (targets.length - done) + plan.leftOut.length;
      const value = worth(stats.claimedList);
      saveStats(stats.claimed, stats.granted, value);
      ui.done({
        ...stats, notCovered, stopped: state.stopped || !!halted, halted, value,
        seconds: (performance.now() - t0) / 1000,
        balance: { start: startBal, end: endBal },
        retry: halted ? new Set() : new Set([...stats.failed.map((f) => f.o.offerId), ...targets.slice(done).map((o) => o.offerId)]),
      });
    } catch (e) {
      ui.error(String(e.message || e));
    } finally {
      state.running = false;
      liveShop = null;
      syncHero();
      refreshLive();
    }
  }

  // ---------- page helpers ----------
  const esc = (s) => String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const ICON = {
    check: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M5 12.5l4.2 4.2L19 7" fill="none" stroke="currentColor" stroke-width="3.2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
    bang: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 6v8" stroke="currentColor" stroke-width="3.2" stroke-linecap="round"/><circle cx="12" cy="18.5" r="1.9" fill="currentColor"/></svg>',
    lock: '<svg viewBox="0 0 24 24" aria-hidden="true"><rect x="5" y="10.5" width="14" height="10" rx="3" fill="currentColor"/><path d="M8.5 10.5V8a3.5 3.5 0 0 1 7 0v2.5" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round"/></svg>',
    ask: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M9.2 9.3a2.9 2.9 0 1 1 4.3 2.5c-.9.5-1.5 1.2-1.5 2.2v.6" fill="none" stroke="currentColor" stroke-width="2.8" stroke-linecap="round" stroke-linejoin="round"/><circle cx="12" cy="18.6" r="1.7" fill="currentColor"/></svg>',
    clock: '<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="8.5" fill="none" stroke="currentColor" stroke-width="2.4"/><path d="M12 7.5V12l3 2" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round"/></svg>',
    shield: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3l7 3v5.5c0 4.3-3 8-7 9.5-4-1.5-7-5.2-7-9.5V6z" fill="currentColor" opacity=".2"/><path d="M8.5 12.2l2.4 2.4 4.6-4.8" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"/></svg>',
    paw: '<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="7" cy="9" r="2.2" fill="currentColor"/><circle cx="12" cy="6.5" r="2.2" fill="currentColor"/><circle cx="17" cy="9" r="2.2" fill="currentColor"/><path d="M12 11.5c-3 0-5.5 3.2-5.5 5.6 0 1.7 1.4 2.4 2.8 2.4 1.1 0 1.7-.6 2.7-.6s1.6.6 2.7.6c1.4 0 2.8-.7 2.8-2.4 0-2.4-2.5-5.6-5.5-5.6z" fill="currentColor"/></svg>',
  };

  function pageShop() {
    try {
      const d = window.__reactRouterDataRouter?.state?.loaderData?.[ROUTE];
      if (!d) return null;
      const offers = collectOffers(d);
      return DEMO ? { loggedIn: true, offers: demoShop(offers) } : { loggedIn: d.isLoggedIn !== false, offers };
    } catch (_) { return null; }
  }

  // "Active until Nov 28, 12:46 PM (GMT+04:00)" next to the V-Bucks balance (English site only)
  function tokenUntil() {
    const el = [...document.querySelectorAll('button, span, div')].find((e) => e.children.length < 3 && /^Active until\s/i.test((e.textContent || '').trim()));
    if (!el) return '';
    const m = el.textContent.trim().match(/^Active until\s+(.+)$/i);
    return m ? m[1].trim().slice(0, 48) : '';
  }
  function tokenUntilDate(txt) {
    if (!txt) return null;
    const clean = txt.replace(/\(GMT([+-]\d{2}):?(\d{2})\)/i, 'GMT$1$2').replace(',', '');
    const y = new Date().getFullYear();
    for (const yy of [y, y + 1]) {
      const d = new Date(clean.replace(/^([A-Za-z]+\s+\d+)/, `$1 ${yy}`));
      if (!isNaN(d) && d.getTime() > Date.now() - 864e5) return d.toISOString();
    }
    return null;
  }
  // the "Plunder the Shop!" prompt shows when the token is owned but not activated yet
  const plunderChip = () => [...document.querySelectorAll('button, a, span')].some((e) => e.children.length < 4 && /plunder the shop/i.test(e.textContent || ''));

  function tileHTML(o) {
    const src = thumb(o);
    return src ? `<img alt="" loading="lazy" decoding="async" referrerpolicy="no-referrer" src="${esc(src)}">` : `<span class="cc-tname">${esc(o.title)}</span>`;
  }

  function countTo(el, to, dur = 450) {
    const from = Number(String(el.dataset.v || 0));
    el.dataset.v = to;
    if (reduceMotion || to === from) { el.textContent = fmtN(to); return; }
    const t0 = performance.now();
    (function step(t) {
      const p = Math.min(1, (t - t0) / dur);
      const e = 1 - Math.pow(1 - p, 3);
      el.textContent = fmtN(Math.round(from + (to - from) * e));
      if (p < 1) requestAnimationFrame(step);
    })(t0);
  }

  // ---------- styles ----------
  function ensureStyle() {
    if (typeof CC_CSS !== 'string' || document.getElementById('cc-claim-style')) return;
    const st = document.createElement('style');
    st.id = 'cc-claim-style';
    st.textContent = CC_CSS;
    (document.head || document.documentElement).appendChild(st);
  }
  function setLogo() {
    if (typeof CC_LOGO === 'string') document.documentElement.style.setProperty('--cc-logo', `url("${CC_LOGO}")`);
  }

  // ---------- hero button + floating button ----------
  let hero = null, heroWrap = null, fab = null, io = null;
  let liveShop = null, liveFetching = false;
  let liveAt = 0;
  async function refreshLive(force = true) {
    if (liveFetching || (!force && Date.now() - liveAt < 60e3)) return;   // React can drop the header often
    liveFetching = true;
    liveAt = Date.now();
    try { [liveShop] = await Promise.all([loadShop(), loadBought()]); state.remaining = null; } catch (_) { /* ignore */ }
    finally { liveFetching = false; if (hero) hero.dataset.key = ''; updateHero(); }
  }

  function heroData() {
    const shop = liveShop || pageShop();
    if (!shop) return { n: state.remaining, block: '' };
    if (!shop.loggedIn) return { n: null, block: 'signin' };
    if (!hasToken(shop.offers)) return { n: null, block: plunderChip() ? 'notactive' : 'notoken' };
    const free = shop.offers.filter((o) => !has(o) && isFree(o) && !excluded(o));
    const n = state.remaining != null ? Math.min(state.remaining, free.length) : free.length;
    return { n, block: '', value: worth(free) };
  }

  function buildHero() {
    const el = document.createElement('button');
    el.id = 'cc-hero';
    el.type = 'button';
    el.innerHTML = '<span class="cc-hb-logo cc-logo"></span><span class="cc-hb-label"></span><span class="cc-hb-n"></span>';
    el.addEventListener('click', () => { if (!state.running && !root) run(); });
    return el;
  }

  function publish(d) {   // read by the extension popup and reminder
    const ds = document.documentElement.dataset;
    const until = tokenUntil();
    ds.ccFree = d.n == null ? '' : String(d.n);
    ds.ccValue = d.value ? String(d.value) : '';
    ds.ccUntil = until;
    ds.ccUntilIso = tokenUntilDate(until) || '';
    ds.ccRunning = state.running ? '1' : '';
    ds.ccBlock = d.block || '';
    ds.ccStats = JSON.stringify(readJSON(STATS_KEY, {}));
    ds.ccVersion = VERSION;
  }

  function updateHero() {
    const d = heroData();
    const { n, block } = d;
    publish(d);
    if (fab) {
      const fn = fab.querySelector('.cc-fab-n');
      fn.textContent = n ? fmtN(n) : '';
      fn.hidden = !n;
      fab.querySelector('.cc-fab-tip').textContent = T('claimAll');
      fab.setAttribute('aria-label', T('claimAll'));
    }
    if (!hero) return;
    const key = `${n}|${state.running}|${block}|${lang}`;
    if (hero.dataset.key === key) return;
    const changed = hero.dataset.key && hero.dataset.key.split('|')[0] !== String(n);
    hero.dataset.key = key;
    const label = hero.querySelector('.cc-hb-label');
    const badge = hero.querySelector('.cc-hb-n');
    hero.classList.toggle('is-running', state.running);
    hero.classList.toggle('is-done', !state.running && n === 0);
    hero.classList.toggle('is-blocked', !!block);
    if (state.running) { label.textContent = T('claiming'); badge.hidden = true; }
    else if (n === 0) { label.textContent = T('allClaimed'); badge.hidden = false; badge.innerHTML = ICON.check; }
    else { label.textContent = T('claimAll'); badge.hidden = n == null; badge.textContent = n == null ? '' : fmtN(n); }
    hero.title = block === 'signin' ? T('tipSignin')
      : block ? T('tipNoToken')
      : n ? T('tipReady', { n }) : n === 0 ? T('tipNone') : T('claimAll');
    if (changed && !reduceMotion) badge.animate([{ transform: 'scale(.4)' }, { transform: 'scale(1.25)' }, { transform: 'scale(1)' }], { duration: 450, easing: 'cubic-bezier(.34,1.56,.64,1)' });
  }

  // the Buy V-Bucks link sits next to the Item Shop heading in every language
  function findRow() {
    const buy = [...document.querySelectorAll('a[href*="/item-shop/v-bucks"]')].find((a) => /\/item-shop\/v-bucks\/?(?:[?#]|$)/.test(a.getAttribute('href') || ''));   // keeps ?lang= on it
    const h1 = document.querySelector('main h1') || document.querySelector('h1');
    if (buy && h1 && buy.parentElement === h1.parentElement) return { row: h1.parentElement, buy, h1 };
    if (h1) return { row: h1.parentElement, buy: [...h1.parentElement.children].find((c) => c !== h1 && c !== heroWrap && /v-?bucks/i.test(c.textContent || '')) || null, h1 };
    return null;
  }

  function mountHero() {
    if (!SHOP_RE.test(location.pathname)) { heroWrap?.remove(); setFab(false); return; }
    ensureStyle();
    const lNow = pickLang();
    if (lNow !== lang) { lang = lNow; if (hero) hero.dataset.key = ''; }
    const found = findRow();
    if (!found) return;
    const { row, buy, h1 } = found;
    if (!hero) {
      heroWrap = document.createElement('div');
      heroWrap.id = 'cc-hero-wrap';
      hero = buildHero();
      heroWrap.appendChild(hero);
      io = new IntersectionObserver(([e]) => setFab(!e.isIntersecting && e.boundingClientRect.top < 0), { threshold: 0 });
      const cs = getComputedStyle(h1);   // Epic's heading font is variable, so copy its width and weight too
      if (cs.fontFamily) document.documentElement.style.setProperty('--cc-head', cs.fontFamily);
      if (cs.fontVariationSettings && cs.fontVariationSettings !== 'normal') document.documentElement.style.setProperty('--cc-head-fvs', cs.fontVariationSettings);
    }
    if (!heroWrap.isConnected && !state.running) refreshLive(false);
    if (heroWrap.parentElement !== row || (buy && heroWrap.nextElementSibling !== buy)) {
      row.insertBefore(heroWrap, buy || null);
      io.disconnect(); io.observe(heroWrap);
    }
    updateHero();
  }

  function setFab(on) {
    if (on && (state.running || root || !SHOP_RE.test(location.pathname) || heroData().n === 0)) on = false;
    if (on && !fab) {
      fab = document.createElement('button');
      fab.id = 'cc-fab';
      fab.type = 'button';
      fab.innerHTML = '<span class="cc-logo"></span><span class="cc-fab-n" hidden></span><span class="cc-fab-tip"></span>';
      fab.addEventListener('click', () => { if (!state.running && !root) run(); });
      document.body.appendChild(fab);
      updateHero();
    }
    if (fab) requestAnimationFrame(() => fab.classList.toggle('is-on', on));
  }

  function syncHero() { if (hero) hero.dataset.key = ''; mountHero(); setFab(false); }

  // ---------- modal ----------
  let root = null;

  function openModal() {
    root?.remove();
    ensureStyle();
    root = document.createElement('div');
    root.id = 'cc-root';
    root.lang = lang;
    root.innerHTML = `
      <div class="cc-scrim"></div>
      <div class="cc-modal" role="dialog" aria-modal="true" aria-labelledby="cc-m-title">
        <div class="cc-m-top">
          <span class="cc-blob b1"></span><span class="cc-blob b2"></span><span class="cc-blob b3"></span>
          ${DEMO ? `<div class="cc-demo">${esc(T('demoBadge'))}</div>` : ''}
          <div class="cc-m-logo"><span class="cc-logo"></span></div>
          <div class="cc-m-count" hidden><b class="cc-n">0</b><span class="cc-total"></span></div>
          <div class="cc-m-title" id="cc-m-title">${esc(T('brand'))}</div>
          <div class="cc-status" aria-live="polite"></div>
          <div class="cc-bar" hidden><i></i></div>
          <div class="cc-toast-row"></div>
          <div class="cc-summary"></div>
        </div>
        <div class="cc-body" hidden></div>
        <div class="cc-grid" hidden></div>
        <div class="cc-m-foot">
          <button type="button" class="cc-btn cc-stop">${esc(T('stop'))} <kbd>Esc</kbd></button>
        </div>
      </div>
      <canvas class="cc-confetti"></canvas>`;
    document.body.appendChild(root);
    setFab(false);
    const prevOverflow = document.documentElement.style.overflow;
    document.documentElement.style.overflow = 'hidden';
    root._restore = () => { document.documentElement.style.overflow = prevOverflow; };
    requestAnimationFrame(() => requestAnimationFrame(() => root && root.classList.add('is-open')));

    const me = root;
    const $ = (s) => me.querySelector(s);
    const tiles = new Map();
    let total = 0;
    const stopBtn = $('.cc-stop');
    stopBtn.onclick = () => { state.stopped = true; ui.status(T('stStopping')); stopBtn.disabled = true; };
    stopBtn.focus({ preventScroll: true });
    const setTitle = (t) => { $('.cc-m-title').textContent = t; };

    const ui = {
      status(text) { $('.cc-status').textContent = text; },

      // pick categories, resolves with the plan or null
      review(free, info) {
        return new Promise((resolve) => {
          me.classList.add('is-review');
          const normal = free;
          const groups = CATS.map(([k]) => [k, normal.filter((o) => catOf(o) === k)]).filter(([, l]) => l.length);
          const on = new Set(groups.map(([k]) => k));
          setTitle(T('reviewTitle', { n: free.length }));
          const meta = [];
          meta.push(T('reviewWorth', { v: worth(free) }));
          if (info.until) meta.push(T('reviewUntil', { d: info.until }));
          ui.status(meta.join(' · '));
          const body = $('.cc-body');
          body.hidden = false;
          body.innerHTML = `
            <div class="cc-sec-h"><b>${esc(T('reviewPick'))}</b>
              <span><button type="button" class="cc-link" data-all="1">${esc(T('selectAll'))}</button> · <button type="button" class="cc-link" data-all="0">${esc(T('selectNone'))}</button></span></div>
            <div class="cc-cats">
              ${groups.map(([k, l]) => `<button type="button" class="cc-cat is-on" data-k="${k}" aria-pressed="true">
                <span class="cc-cat-imgs">${l.slice(0, 3).map(tileHTML).join('')}</span>
                <span class="cc-cat-t">${esc(T('cat_' + k))}</span><b>${fmtN(l.length)}</b></button>`).join('')}
            </div>
            <p class="cc-safe"><span class="cc-ic">${ICON.shield}</span>${esc(T('safety'))}${info.balance != null ? ` <b>${esc(T('balanceNow', { b: info.balance }))}</b>` : ''}</p>
            <label class="cc-quick"><input type="checkbox"${settings().quick ? ' checked' : ''}> ${esc(T('quickToggle'))}</label>`;
          const foot = $('.cc-m-foot');
          foot.innerHTML = `<button type="button" class="cc-btn cc-cancel">${esc(T('cancel'))}</button><button type="button" class="cc-btn cc-primary cc-go"></button>`;
          const go = foot.querySelector('.cc-go');
          const chosen = () => {
            const picked = normal.filter((o) => on.has(catOf(o)));
            return { picked, sidekick: null, leftOut: normal.filter((o) => !on.has(catOf(o))) };
          };
          const refresh = () => {
            const c = chosen();
            const n = c.picked.length;
            go.textContent = T('start', { n });
            go.disabled = n === 0;
            body.querySelectorAll('.cc-cat').forEach((b) => { const v = on.has(b.dataset.k); b.classList.toggle('is-on', v); b.setAttribute('aria-pressed', String(v)); });
          };
          body.querySelectorAll('.cc-cat').forEach((b) => { b.onclick = () => { on.has(b.dataset.k) ? on.delete(b.dataset.k) : on.add(b.dataset.k); refresh(); }; });
          body.querySelectorAll('[data-all]').forEach((b) => { b.onclick = () => { if (b.dataset.all === '1') groups.forEach(([k]) => on.add(k)); else on.clear(); refresh(); }; });
          body.querySelector('.cc-quick input').onchange = (e) => saveSettings({ quick: e.target.checked });
          let settled = false;
          const finish = (v) => {
            if (settled) return;
            settled = true;
            me._cancelReview = null;
            me.classList.remove('is-review');
            body.hidden = true; body.innerHTML = '';
            foot.innerHTML = `<button type="button" class="cc-btn cc-stop">${esc(T('stop'))} <kbd>Esc</kbd></button>`;
            const s = foot.querySelector('.cc-stop');
            s.onclick = () => { state.stopped = true; ui.status(T('stStopping')); s.disabled = true; };
            resolve(v);
          };
          me._cancelReview = () => finish(null);
          foot.querySelector('.cc-cancel').onclick = () => finish(null);
          go.onclick = () => finish(chosen());
          refresh();
          go.focus({ preventScroll: true });
        });
      },

      build(offers) {
        total = offers.length;
        setTitle(T('brand'));
        $('.cc-m-count').hidden = false;
        $('.cc-bar').hidden = false;
        $('.cc-total').textContent = `/ ${fmtN(total)}`;
        const grid = $('.cc-grid');
        grid.hidden = false;
        grid.innerHTML = '';
        offers.forEach((o, i) => {
          const t = document.createElement('div');
          t.className = 'cc-tile' + (isBundle(o) ? ' is-bundle' : '') + (isSidekick(o) ? ' is-sk' : '');
          t.style.setProperty('--c1', o.color1 || '#4a2a52');
          t.style.setProperty('--c2', o.color2 || '#21122a');
          t.style.setProperty('--d', `${Math.min(i, 50) * 18}ms`);
          t.title = o.title;
          t.innerHTML = `${tileHTML(o)}${isBundle(o) ? `<span class="cc-tag">${esc(T('tagBundle'))}</span>` : ''}<span class="cc-badge"></span>`;
          grid.appendChild(t);
          tiles.set(o.offerId, t);
        });
      },
      tile(id, status, why) {
        const t = tiles.get(id);
        if (!t) return;
        t.classList.remove('s-working');
        const s = status === 'owned' ? 'bundle' : status;
        t.classList.add('s-' + s);
        const badge = t.querySelector('.cc-badge');
        if (s === 'claimed' || s === 'bundle') badge.innerHTML = ICON.check;
        if (s === 'failed') badge.innerHTML = ICON.bang;
        if (why) t.title += `: ${why}`;
        if (s === 'working') t.scrollIntoView?.({ block: 'nearest', behavior: reduceMotion ? 'auto' : 'smooth' });
        if (s === 'claimed' && !reduceMotion) {
          t.animate([{ transform: 'scale(.82)' }, { transform: 'scale(1.14)' }, { transform: 'scale(1)' }], { duration: 520, easing: 'cubic-bezier(.34,1.56,.64,1)' });
          sparkle(t);
        }
      },
      progress(done) {
        countTo($('.cc-n'), done);
        $('.cc-bar i').style.width = `${total ? (done / total) * 100 : 0}%`;
      },
      toast(o) {
        $('.cc-toast-row').innerHTML = `<span class="cc-toast"><span class="cc-toast-img" style="--c1:${esc(o.color1 || '#ff4fa3')};--c2:${esc(o.color2 || '#7a0a4c')}">${tileHTML(o)}</span><span class="cc-toast-t">${esc(o.title)}</span><span class="cc-toast-ok">${ICON.check}</span></span>`;
      },
      done(s) {
        me.classList.add('is-done');
        $('.cc-toast-row').innerHTML = '';
        $('.cc-bar').hidden = true;
        const got = s.claimed + s.viaBundle + s.owned;
        if (s.nothing) {
          $('.cc-m-count').hidden = true;
          setTitle(T('nothingTitle'));
          ui.status(T('nothingText'));
        } else {
          countTo($('.cc-n'), got, 300);
          const secs = s.seconds < 60 ? T('secs', { s: s.seconds.toFixed(1) }) : T('mins', { m: Math.floor(s.seconds / 60), s: Math.round(s.seconds % 60) });
          setTitle(s.halted ? T('safetyTitle') : s.stopped ? T('stoppedTitle') : got ? T('doneTitle') : T('nothingClaimed'));
          ui.status(s.halted ? T('safetyText', { a: s.halted.a, b: s.halted.b }) : s.stopped ? T('stoppedText', { n: got }) : T('doneText', { n: s.granted + s.viaBundle, t: secs }));
          if (s.halted) me.classList.add('is-halted');
        }
        const bal = s.balance && s.balance.start != null && s.balance.end != null
          ? (s.balance.end < s.balance.start
            ? `<span class="cc-chip cc-chip-bad">${ICON.bang} ${esc(T('chipVbBad', { a: s.balance.start, b: s.balance.end }))}</span>`
            : `<span class="cc-chip cc-chip-ok">${ICON.shield} ${esc(T('chipVbOk', { b: s.balance.end }))}</span>`) : '';
        const chip = (cls, n, key) => n ? `<span class="cc-chip ${cls}"><i></i>${esc(T(key, { n }))}</span>` : '';
        const chips = [
          bal,
          s.value ? `<span class="cc-chip cc-c-worth"><i></i>${esc(T('chipWorth', { v: s.value }))}</span>` : '',
          chip('cc-c-claimed', s.claimed, 'chipClaimed'),
          chip('cc-c-bundle', s.viaBundle + s.owned, 'chipBundle'),
          chip('cc-c-failed', s.failed?.length, 'chipSkipped'),
          chip('cc-c-left', s.leftOut?.length, 'chipLeftOut'),
        ].join('');
        const li = (title, why) => `<li><b>${esc(title)}</b><span>${esc(why)}</span></li>`;
        const failed = s.failed?.length ? `<details class="cc-more"><summary>${esc(T('whySkipped'))}</summary><ul>${s.failed.map((f) => li(f.o.title, f.why)).join('')}</ul></details>` : '';
        const left = s.leftOut?.length ? `<details class="cc-more"><summary>${esc(T('leftOutList'))}</summary><ul>${s.leftOut.map((o) => li(o.title, T('cat_' + catOf(o)))).join('')}</ul></details>` : '';
        const nc = s.notCovered?.length ? `<details class="cc-more"><summary>${esc(T('notCovered', { n: s.notCovered.length }))}</summary><ul>${s.notCovered.map((o) =>
          li(o.title, o.pricing?.currencyType === 'RealMoney' ? T('rmPack') : excluded(o) ? T('rExcluded') : T('vbPrice', { v: o.pricing?.finalPrice ?? 0 }))).join('')}</ul></details>` : '';
        $('.cc-summary').innerHTML = `<div class="cc-chips">${chips}</div>${failed}${left}${nc}`;
        const foot = $('.cc-m-foot');
        const again = s.retry && s.retry.size;
        foot.innerHTML = `${s.claimedList?.length ? `<button type="button" class="cc-btn cc-save">${esc(T('saveList'))}</button>` : ''}
          ${again ? `<button type="button" class="cc-btn cc-again">${esc(T('retry', { n: s.retry.size }))}</button>` : ''}
          ${s.leftOut?.length && !again && !s.halted ? `<button type="button" class="cc-btn cc-again2">${esc(T('reviewAgain'))}</button>` : ''}
          <button type="button" class="cc-btn cc-primary cc-finish">${esc(T('done'))}</button>`;
        foot.querySelector('.cc-save')?.addEventListener('click', () => saveList(s));
        foot.querySelector('.cc-again')?.addEventListener('click', () => { closeModal(); run({ only: s.retry }); });
        foot.querySelector('.cc-again2')?.addEventListener('click', () => { closeModal(); run({ forceReview: true }); });
        foot.querySelector('.cc-finish').onclick = () => (DEMO || s.nothing ? closeModal() : location.reload());
        foot.querySelector('.cc-finish').focus({ preventScroll: true });
        if (!s.stopped && got > 0) confetti(me.querySelector('.cc-confetti'), $('.cc-m-logo'));
      },
      error(msg) {
        me.classList.add('is-done', 'is-error');
        me.classList.remove('is-review');
        $('.cc-bar').hidden = true;
        $('.cc-toast-row').innerHTML = '';
        setTitle(T('snagTitle'));
        ui.status(T('snagText', { msg: msg.replace(/[.\s]+$/, '') }));
        const foot = $('.cc-m-foot');
        foot.innerHTML = `<button type="button" class="cc-btn">${esc(T('close'))}</button><button type="button" class="cc-btn cc-primary">${esc(T('tryAgain'))}</button>`;
        foot.children[0].onclick = closeModal;
        foot.children[1].onclick = () => { closeModal(); run(); };
        foot.children[1].focus({ preventScroll: true });
      },
      notice(kind) {
        const n = kind === 'signin' ? { icon: ICON.lock, title: T('signinTitle'), text: T('signinText') }
          : kind === 'notactive' ? { icon: ICON.clock, title: T('notActiveTitle'), text: T('notActiveText') }
          : { icon: ICON.ask, title: T('noTokenTitle'), text: T('noTokenText') };
        me.classList.add('is-done', 'is-notice', `is-${kind}`);
        $('.cc-bar').hidden = true;
        $('.cc-toast-row').innerHTML = '';
        $('.cc-m-logo').insertAdjacentHTML('beforeend', `<span class="cc-m-tag">${n.icon}</span>`);
        setTitle(n.title);
        ui.status(n.text);
        const foot = $('.cc-m-foot');
        foot.innerHTML = kind === 'signin'
          ? `<button type="button" class="cc-btn">${esc(T('close'))}</button><button type="button" class="cc-btn cc-primary">${esc(T('signin'))}</button>`
          : `<button type="button" class="cc-btn cc-primary">${esc(T('gotIt'))}</button>`;
        foot.firstElementChild.onclick = closeModal;
        if (kind === 'signin') foot.lastElementChild.onclick = signIn;
        foot.lastElementChild.focus({ preventScroll: true });
      },
    };
    return ui;
  }

  function saveList(s) {
    const day = new Date().toISOString().slice(0, 10);
    const lines = [T('listHeader', { d: day }), ''];
    for (const o of s.claimedList) lines.push(`+ ${o.title}`);
    if (s.failed.length) { lines.push('', T('whySkipped')); for (const f of s.failed) lines.push(`- ${f.o.title}: ${f.why}`); }
    lines.push('', `Chest Counter Claim All ${VERSION} · https://chestcounter.io/claim`);
    const a = document.createElement('a');
    a.href = URL.createObjectURL(new Blob([lines.join('\r\n')], { type: 'text/plain;charset=utf-8' }));
    a.download = `pirate-shop-claims-${day}.txt`;
    document.body.appendChild(a); a.click(); a.remove();
    setTimeout(() => URL.revokeObjectURL(a.href), 4000);
  }

  // Epic's sign-in link lives inside their nav's shadow DOM
  function signIn() {
    const a = document.querySelector('fortnite-navigation')?.shadowRoot?.querySelector('a[href*="/login"]');
    if (a) a.click(); else location.href = '/item-shop/login';
  }

  function closeModal() {
    if (!root) return;
    const r = root; root = null;
    r._cancelReview?.();
    r._restore?.();
    r.classList.remove('is-open');
    setTimeout(() => r.remove(), 300);
    syncHero();
  }

  function sparkle(tile) {
    const colors = ['#ff4fa3', '#ffb000', '#3ddc97', '#7cd4ff'];
    for (let i = 0; i < 7; i++) {
      const s = document.createElement('i');
      s.className = 'cc-spark';
      const a = (i / 7) * Math.PI * 2 + Math.random() * .5;
      const d = 26 + Math.random() * 16;
      s.style.cssText = `--x:${Math.cos(a) * d}px;--y:${Math.sin(a) * d}px;background:${colors[i % colors.length]}`;
      tile.appendChild(s);
      setTimeout(() => s.remove(), 700);
    }
  }

  function confetti(canvas, fromEl) {
    if (reduceMotion || !canvas) return;
    const ctx = canvas.getContext('2d');
    const dpr = Math.min(2, devicePixelRatio || 1);
    const W = canvas.width = innerWidth * dpr, H = canvas.height = innerHeight * dpr;
    const rc = fromEl?.getBoundingClientRect();
    const ox = rc ? (rc.left + rc.width / 2) * dpr : W / 2, oy = rc ? (rc.top + rc.height / 2) * dpr : H / 3;
    const colors = ['#ff4fa3', '#d00080', '#ffb000', '#ffd23f', '#3ddc97', '#7cd4ff'];
    const parts = Array.from({ length: 150 }, (_, i) => {
      const a = Math.random() * Math.PI * 2, sp = (Math.random() * 14 + 6) * dpr;
      return { x: ox, y: oy, vx: Math.cos(a) * sp, vy: Math.sin(a) * sp - 8 * dpr, r: (Math.random() * 5 + 3.5) * dpr,
        rot: Math.random() * 6, vr: (Math.random() - .5) * .3, kind: i % 5 === 0 ? 'coin' : i % 3 === 0 ? 'ring' : 'dot', c: colors[i % colors.length] };
    });
    const t0 = performance.now();
    (function frame(t) {
      ctx.clearRect(0, 0, W, H);
      const life = (t - t0) / 2600;
      for (const p of parts) {
        p.vy += .38 * dpr; p.vx *= .985; p.vy *= .985; p.x += p.vx; p.y += p.vy; p.rot += p.vr;
        ctx.globalAlpha = Math.max(0, 1 - life * life);
        ctx.save(); ctx.translate(p.x, p.y); ctx.rotate(p.rot);
        if (p.kind === 'coin') {
          ctx.scale(Math.abs(Math.cos(p.rot * 2)) * .85 + .15, 1);
          ctx.fillStyle = '#ffb000'; ctx.beginPath(); ctx.arc(0, 0, p.r * 1.3, 0, 7); ctx.fill();
          ctx.fillStyle = '#ffe07a'; ctx.beginPath(); ctx.arc(0, 0, p.r * .7, 0, 7); ctx.fill();
        } else if (p.kind === 'ring') {
          ctx.strokeStyle = p.c; ctx.lineWidth = 2.2 * dpr; ctx.beginPath(); ctx.arc(0, 0, p.r, 0, 7); ctx.stroke();
        } else {
          ctx.fillStyle = p.c; ctx.beginPath(); ctx.arc(0, 0, p.r, 0, 7); ctx.fill();
        }
        ctx.restore();
      }
      if (life < 1) requestAnimationFrame(frame); else ctx.clearRect(0, 0, W, H);
    })(t0);
  }

  document.addEventListener('keydown', (e) => {
    if (e.key !== 'Escape' || !root) return;
    e.stopPropagation();
    if (root.classList.contains('is-review')) closeModal();
    else if (state.running) root.querySelector('.cc-stop')?.click();
    else if (root.classList.contains('is-done')) closeModal();
  }, true);

  // hide broken thumbnails (Epic's image links expire after about a day) and keep the tile colour
  document.addEventListener('error', (e) => {
    const t = e.target;
    if (t && t.tagName === 'IMG' && t.closest && t.closest('#cc-root')) t.classList.add('is-broken');
  }, true);

  // from the extension popup (via the bridge)
  document.addEventListener('cc:claim', () => { if (!state.running && !root) run(); });
  document.addEventListener('cc:refresh', () => { lang = pickLang(); syncHero(); });

  // the popup opens the shop with ?ccclaim=1
  (async () => {
    const url = new URL(location.href);
    if (!url.searchParams.has('ccclaim')) return;
    url.searchParams.delete('ccclaim');
    history.replaceState(history.state, '', url.pathname + url.search + url.hash);
    for (let i = 0; i < 40 && !window.__reactRouterDataRouter; i++) await sleep(250);
    await sleep(900);
    if (!state.running) run();
  })();

  setLogo();
  mountHero();
  setInterval(mountHero, 800);   // React re-renders the header and drops the button
})();

})();
