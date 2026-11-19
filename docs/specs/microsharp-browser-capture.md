# Mikrofonaufnahme über MicroSharp.Browser

Die Sprechaufnahme im Browser bezieht die rohen PCM-Blöcke aus dem Paket `MicroSharp.Browser`. Standardmäßig werden die Pakete von NuGet.org wiederhergestellt. Für unveröffentlichte Kandidaten steht `nuget.microsharp-local.config` mit dem relativen Feed `local-feed` zur Verfügung. Die Pakete werden nicht in das Repository übernommen.

Das Projekt zielt auf `net11.0`. Ein zusätzliches Ziel `net10.0` gibt es für diese Integration nicht.

## Grenze

MicroSharp liefert die Browserquelle, das Format, die begrenzte Live-Warteschlange sowie Lebenszyklus und Besitz. Die Anwendung behaelt die Mikrofonsperre, Sprachschwellen, Segmentierung und die weitere Sprachverarbeitung. Der Vorlauf-Ring delegiert Kopie und Cursor an die Bibliothek.

Die Live-Warteschlange fasst 50 Blöcke. Ein voller Puffer verwirft den ältesten Block und zählt ihn. Jeder Block ist eine Kopie, die der Leser besitzt. Der Vorlauf-Ring der Anwendung ist davon getrennt und wird nicht durch ein weiteres Paket ersetzt.

Der Browseradapter unterstützt nur `DropOldest`.

## Verhalten an der Naht

- Eine erfolgreiche Anforderung behält die Quelle. `Stop` trennt den Aufnahmegraphen und gibt das Gerät nicht frei. Dieselbe Quelle kann erneut verbunden werden. `Release` gibt sie frei.
- Eine Freigabe erreicht den Browser, auch wenn das Verbinden derselben Quelle noch nicht zurück ist. Schlägt sie fehl, bleibt dieser Start veröffentlichbar und ein erneuter Freigabeversuch wirksam. Gelingt sie, veröffentlicht dieser Start keine Aufnahme.
- Die Mikrofonsperre erreicht den Browser, solange die Freigabe noch nicht erfolgreich war, auch während der Freigabeaufruf selbst läuft. Nach erfolgreicher Freigabe ist die Quelle für die Sperre nicht mehr offen.
- Läuft die Wartezeit einer Freigabe ab, erreicht der nächste Versuch den Browser erneut. Bis die Freigabe gelingt, bleibt die Quelle für die Sperre auffindbar.
- Das Verwerfen einer bereits beendeten älteren Aufnahme trennt den Graphen der danach verbundenen Aufnahme nicht. Die ältere Aufnahme gibt dabei ihre eigene Browser-Rückmeldung frei.
- Die Spur kann vor dem Verbinden und während der Aufnahme stumm geschaltet werden. Die Sperrentscheidung bleibt in der Anwendung.
- Halten des Audiokontexts ist ein Argument der Anforderung. Ein direkter Bibliotheksstart lässt es aus.
- Das gemeldete Format bleibt `audio/pcm;rate=16000`. Abweichende Raten werden abgelehnt.
- Ein Leseabbruch nimmt den nächsten Block nicht aus der Warteschlange und beendet die Quelle nicht.
- Ein zu kurzer oder zu langer Block beendet die Sitzung mit einem Formatfehler.

## Direkter Lesepfad

Eine Aufnahme liest die begrenzte Paketsitzung und gibt die Nutzlast weiter. Die Produktion hat keine zweite unbegrenzte Warteschlange und keinen Rueckruf `OnChunk`. Pruefungen, die Bloecke ohne Browser einspeisen, nutzen ein Test-Doppel.

Ein Abbruch nach einem gelesenen Block gibt den Leser frei und lässt die Sitzung laufen. Der nächste Leser erhält den folgenden gültigen Block. Ein ausstehendes Lesen wird über das Abbruchtoken beendet, abgewartet und danach freigegeben. Die Sitzung bleibt laufend und kann erneut gelesen werden.

## Prüfung im Browser

`scripts/run-microphone-browser-harness.sh` veröffentlicht `Sprechen.Speech.MicrophoneBrowserHarness` und startet es in headless Chromium mit einer synthetischen Aufnahmedatei. Das Projekt referenziert die Anwendungsprojekte für Mikrofon und Aufnahme-Laufzeit und registriert `AddMicrophoneRecording`. Dieselbe Instanz bedient Rekorder, Lesespur und Sperre.

Die Veröffentlichung verwendet die mitgelieferte WebAssembly-Laufzeit. Eine native Neulinkung ist für die Aufnahme nicht nötig. Laufzeit-Async bleibt aus.

Der Lauf prüft die gehaltene Quelle nach der Anforderung, die geschlossene Sperre vor dem Start, Stummschaltung und erneutes Öffnen während der Aufnahme, Stopp und Neustart auf derselben Quelle, die Freigabe von Spur und Audiokontext und von null verschiedenes PCM im Ring. Die ausgelieferte `microsharp-capture.js` muss dem Hash im lokalen Paket entsprechen. `microphoneRecorder.js` und `pcmRecorderProcessor.js` werden dabei nicht geladen.

Geskriptete Browser-Doubles bleiben eine eigene Prüfung. Das veröffentlichte Bibliotheksbeispiel ist ein eigener Lauf. Der Anwendungs-Harness deckt das geänderte Browserverhalten mit dem JavaScript aus dem lokalen Paket ab.

## Gemessener Pfad

Der Produktionsleser vergleicht seine Zuweisung mit einem Test-Doppel, das Bloecke ohne Browser einspeist. Der Block hat 640 Byte. Die Pruefung verlangt mehr als die halbe Blockgroesse und weniger als das Test-Doppel plus einen Block plus 512 Byte. Das ist keine Laufzeitzusage.

Ring und Segmentierer der Anwendung, kurzer Lauf mit drei Iterationen:

| Pfad | Mittelwert | Zuweisung |
| --- | --- | --- |
| Ring, ein Block schreiben und lesen | 66,63 ns | keine zusätzliche Zuweisung |
| Segmentierer vor bestätigter Sprache | 1.659,94 ns | 2992 Byte |
| Segmentierer nach bestätigter Sprache | 14.208,41 ns | 3032 Byte |
| Typische Äußerung, Stille 960 ms | 14.509,46 ns | 3032 Byte |
| Typische Äußerung, Stille 3000 ms | 28.335,16 ns | 3032 Byte |

Diese Zeiten messen Ring und Segmentierer. Sie messen nicht den Paketleser. Bei drei Iterationen ist das Konfidenzintervall weit. Die Werte sind keine Laufzeitzusage. Die Live-Warteschlange der Bibliothek bleibt bei 50 Blöcken mit `DropOldest`. Der Vorlauf-Ring bleibt in der Anwendung.

## Grenzen dieser Prüfung

Die Prüfung verwendet eine nachgebildete Browser-Laufzeit und ein synthetisches PCM-Signal. Sie öffnet kein physisches Mikrofon und ruft keinen Sprachdienst auf.

Das Paket verwendet `AudioContext` des Browsers. Die Seite kann `standardized-audio-context` weiterhin für andere Audiopfade laden. Dieser Ponyfill ist nicht Teil von MicroSharp.

Laufzeit-Async des WebAssembly-Laufzeitsystems bleibt aus. Threads und AOT sind für diesen Pfad nicht eingeschaltet.

Die lokale Paketversion ist an den hier gepackten Quellstand gebunden. Ein gleichlautendes Paket aus einem anderen Cache darf die Prüfung nicht erfüllen. Dafür ist `NUGET_PACKAGES` bei jeder Wiederherstellung ein neues Verzeichnis.

Die Windows-Prüfung des nativen Adapters bleibt ein eigener Lauf. Dieser Stand prüft den Linux-Ladeweg und ein privates virtuelles Aufnahmegerät. Der native Quelltext ist gegenüber der öffentlichen Qualifikation unverändert. Die lokale Bibliotheksänderung betrifft den Browser.

Mit `--nuget-org` bezieht der Harness seine Pakete von NuGet.org. Das angegebene Paketverzeichnis liefert die Referenz für den JavaScript-Hash.
