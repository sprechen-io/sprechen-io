# Mikrofonaufnahme über MicroSharp.Browser

Die Sprechaufnahme im Browser bezieht die rohen PCM-Blöcke aus dem Paket `MicroSharp.Browser` 0.1.0. Die Pakete stammen aus dem relativen Feed `local-feed` neben `nuget.config`. `MicroSharp` und `MicroSharp.*` werden nur dort aufgelöst. Andere Abhängigkeiten kommen von nuget.org. Die Pakete werden nicht in das Repository übernommen.

Das Projekt zielt auf `net11.0`. Ein zusätzliches Ziel `net10.0` gibt es für diese Integration nicht.

## Grenze

MicroSharp liefert die Browserquelle, das Format und eine begrenzte Live-Warteschlange. Die Anwendung behält Berechtigung, Gerätelebenszyklus, Mikrofonsperre, Vorlauf-Ring, Segmentierung und die weitere Sprachverarbeitung.

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

## Grenzen dieser Prüfung

Die Prüfung verwendet eine nachgebildete Browser-Laufzeit und ein synthetisches PCM-Signal. Sie öffnet kein physisches Mikrofon und ruft keinen Sprachdienst auf.

Das Paket verwendet `AudioContext` des Browsers. Die Seite kann `standardized-audio-context` weiterhin für andere Audiopfade laden. Dieser Ponyfill ist nicht Teil von MicroSharp.

Laufzeit-Async des WebAssembly-Laufzeitsystems bleibt aus. Threads und AOT sind für diesen Pfad nicht eingeschaltet.

Die lokale Paketversion 0.1.0 ist an den hier gepackten Quellstand gebunden. Ein gleichlautendes Paket aus einem anderen Cache darf die Prüfung nicht erfüllen. Dafür ist `NUGET_PACKAGES` bei jeder Wiederherstellung ein neues Verzeichnis.
