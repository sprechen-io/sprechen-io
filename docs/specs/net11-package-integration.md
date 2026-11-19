# .NET 11 und wiederverwendbare Pakete

Die Anwendung und ihre Testprojekte laufen auf .NET 11. Der Browser verwendet MicroSharp.Browser und MicroSharp.Processing in Version 0.3.0-alpha.1. Der unveröffentlichte Kandidat wird aus dem lokalen Paketfeed wiederhergestellt. MicroSharp verwaltet Aufnahme, Lebenszyklus und PCM-Puffer. Sprachregeln und Dialoge gehören zur Anwendung.

Der Sprachdienst verwendet Monstertacle.SharpQrs für seine Kommandos und Abfragen. Die Handler, ihre Lebensdauer und der gRPC-Vertrag gehören zum Sprachdienst. Die formularspezifische Zustands- und Ereignislogik bleibt im Formularlaufzeitprojekt.

Der öffentliche Build soll alle Projekte wiederherstellen und testen. Ein eigener Chromium-Schritt prüft die ausgelieferte WebAssembly-Aufnahme mit einem simulierten Mikrofon und vergleicht das ausgelieferte JavaScript mit dem NuGet-Paket.

SharpQrs unterstützt im Quellprojekt .NET 10 und .NET 11. Die öffentliche NuGet-Version 0.2.0-alpha.1 enthält das mit .NET 11 kompatible net10.0-Asset. Eine separate Prüfung kann die net11.0-Assembly des SharpQrs-CI-Artefakts verwenden.

## Zeitgrenze für eingehende PCM-Blöcke

Beide Aufnahmewege starten nach dem Auslesen des Rückblickpuffers und der Bereitschaftsmeldung eine neue Überwachung für den aktiven Versuch. Die Anwendung verwendet `ChunkArrivalLivenessTimeout` sowohl bis zum ersten Block als auch zwischen Blöcken. Gezählt werden die Live-Aufrufe am Eingang der Blockverarbeitung vor der Segmentierung, einschließlich digitaler Stille. Rückblickdaten setzen die Frist nicht zurück.

Die Frist läuft beim Erreichen der konfigurierten Dauer ab. Ein Block ab diesem Zeitpunkt hebt den erkannten Stillstand nicht auf. Die Anwendung meldet beide Fristüberschreitungen als `SpeechCaptureStreamStalledException`, sofern ihr Aufnahme-Task noch offen ist und die Fehlermeldung den Abschluss gewinnt. Abschluss und Abbruch geben die Überwachung frei. Jeder folgende Versuch beginnt mit einer frischen Frist.
