# Neue Block Typen

## Normale Blöcke (Keine instanzen)

* Für schnelleres rendern und weil wir viele instanzen haben
  * Keine Instanzen nur ein int array

## Instanz Blöcke (instanzen)

### Entitätsblöcke
(Chunkloader, Fallender Sand etc...)

### Teilblöcke
(Deko, Stühle, Tische, etc....)

### Funktionsblöcke

* Hat ein State
* Muss geupdated werden

Sie werden in 3 Arten unterteilt

1. Aktiv
   * Geladen und ohne spieler in der Simulation aktiv
   * Wann müssen wir den Block laden und entladen?
     * Evtl. mit den Chunks laden
     * Kann länger leben als ein Chunk
       * Wann entladen?
     * Wo werden die aktiven Instanzblöcke gehalten?
2. Passiv
   * Nur mit Spieleraktion
   * Sind Chunkabhängig (Laden/Entladen)
3. Weltverändernd
   * Manipuliert die Welt 
     * normale Blöcke 
     * Entities
     * etc...
   * Chunkabhängig (Laden/Entladen)

## Zu klären / Fragen

| Frage | Antwort |
|-------|--------- |
|Können Entity Components für Blöcke genutzt werden?| Jein, nicht ohne Umbau und besser wäre es eine zwischenebene einzubauen, um eine Trennung von Components zu haben, die für Entities oder Blöcke gedacht ist |
|Wie sinnvoll sind die Chunkabhängikeiten der Blöcke?| |
|Wie wird welcher Blocktyp serialisiert?| Da wir Instanzen für die Funktionsblöcke halten, können diese so serialisiert werden, wie Entities auch schon|
|Vordefinierten State oder dynamisch pro Block?| |
|Von wo wird der Update Call gestartet und mit welchen Infos?| Sollte von der Simulation Component aufgerufen werden. |
|Wo werden die aktiven Instanzblöcke gehalten?| In den SimulationComponents und damit in der Simulation |
|Wie machen wir die ComponentType Unterscheidung?| 2 Lösungen:<br>1. Mehrere Interfaces (IEntityComponent, IFunctionalBlockComponent)<br> 2. Mehrere Basisklassen (EntityComponent, BlockComponent, StateComponent[Basis von Entity und Block]) |