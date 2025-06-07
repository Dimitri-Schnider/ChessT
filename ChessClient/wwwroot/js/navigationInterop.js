window.navigationInterop = {
    // Flag, das vom C#-Code gesetzt wird, um anzuzeigen, ob ein Spiel aktiv ist.
    isGameActive: false,

    // Der Event-Handler, der vor dem Schliessen der Seite aufgerufen wird.
    beforeUnloadHandler: function (event) {
        // Nur die Warnung anzeigen, wenn ein Spiel aktiv ist.
        if (window.navigationInterop.isGameActive) {
            event.preventDefault();
            // Notwendig für ältere Browser
            event.returnValue = '';
            // Moderne Browser zeigen eine generische Nachricht, aber das ist der Trigger.
            return '';
        }
    },

    // Funktion, die von C# aufgerufen wird, um den Spielstatus zu setzen.
    setGameActiveState: function (isActive) {
        this.isGameActive = isActive;
    },

    // Fügt den Event-Listener hinzu. Wird einmal beim Initialisieren der Chess-Seite aufgerufen.
    addBeforeUnloadListener: function () {
        window.addEventListener('beforeunload', this.beforeUnloadHandler);
    },

    // Entfernt den Event-Listener. Wird aufgerufen, wenn die Chess-Seite zerstört wird.
    removeBeforeUnloadListener: function () {
        window.removeEventListener('beforeunload', this.beforeUnloadHandler);
    }
};