// Globales Interop-Objekt für die Kommunikation zwischen .NET (z.B. Blazor) und JavaScript.
window.handCardsDisplayInterop = {

    /**
     * Scrollt das angegebene Element sanft horizontal.
     * @param {HTMLElement} element Das zu scrollende Element.
     * @param {number} amount Die Distanz in Pixeln.
     */
    scrollElementHorizontal: function (element, amount) {
        if (element) {
            element.scrollBy({ left: amount, behavior: 'smooth' });
        }
    },

    /**
     * Gibt den aktuellen Scroll-Status (Position, Gesamtbreite etc.) des Elements zurück.
     * @param {HTMLElement} element Das zu prüfende Element.
     * @returns {object|null} Ein Objekt mit Scroll-Daten oder null.
     */
    getScrollState: function (element) {
        if (element) {
            return {
                scrollLeft: element.scrollLeft,
                scrollWidth: element.scrollWidth,
                clientWidth: element.clientWidth
            };
        }
        return null; // Wichtig: null zurückgeben, wenn kein Element
    },

    // Speichert aktive Scroll-Listener, um sie später entfernen zu können.
    scrollListeners: new Map(),

    /**
     * Fügt einen performanten (debounced) Scroll-Listener hinzu,
     * der eine .NET-Methode nach dem Scrollen aufruft.
     * @param {HTMLElement} element Das Element, das überwacht wird.
     * @param {object} dotNetHelper Die .NET-Objektreferenz für den Rückruf.
     */
    addScrollListener: function (element, dotNetHelper) {
        if (element && dotNetHelper) {
            // Alten Listener entfernen, falls vorhanden, um Duplikate zu vermeiden.
            if (this.scrollListeners.has(element)) {
                const oldListenerInfo = this.scrollListeners.get(element);
                element.removeEventListener('scroll', oldListenerInfo.handler);
            }

            let timeoutId;
            // "Debouncing" verhindert, dass der Code bei jedem Pixel des Scrollens feuert.
            const debouncedHandler = () => {
                clearTimeout(timeoutId);
                timeoutId = setTimeout(() => {
                    // Ruft die .NET Methode 'HandleScroll' auf.
                    try {
                        dotNetHelper.invokeMethodAsync('HandleScroll');
                    } catch (e) {
                        console.error("Fehler beim Aufruf von dotNetHelper.HandleScroll: ", e);
                    }
                }, 100); // Feuert 100ms nach dem letzten Scroll-Event.
            };

            element.addEventListener('scroll', debouncedHandler);
            // Speichert den Handler, um ihn später gezielt entfernen zu können.
            this.scrollListeners.set(element, { handler: debouncedHandler, dotNetHelper: dotNetHelper });
        }
    },

    /**
     * Entfernt den Scroll-Listener von einem Element, um Memory-Leaks zu verhindern.
     * @param {HTMLElement} element Das Element, von dem der Listener entfernt wird.
     */
    removeScrollListener: function (element) {
        if (element && this.scrollListeners.has(element)) {
            const listenerInfo = this.scrollListeners.get(element);
            element.removeEventListener('scroll', listenerInfo.handler);
            this.scrollListeners.delete(element);
        }
    }
};