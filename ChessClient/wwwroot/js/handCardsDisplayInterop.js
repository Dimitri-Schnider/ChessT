// File: [SolutionDir]\ChessClient\wwwroot\js\handCardsDisplayInterop.js
window.handCardsDisplayInterop = {
    scrollElementHorizontal: function (element, amount) {
        if (element) {
            element.scrollBy({ left: amount, behavior: 'smooth' });
        }
    },

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

    scrollListeners: new Map(),

    addScrollListener: function (element, dotNetHelper) {
        if (element && dotNetHelper) {
            if (this.scrollListeners.has(element)) {
                const oldListenerInfo = this.scrollListeners.get(element);
                element.removeEventListener('scroll', oldListenerInfo.handler);
                // Nicht dotNetHelper hier disposen, gehört zur Komponente
            }

            let timeoutId;
            const debouncedHandler = () => {
                clearTimeout(timeoutId);
                timeoutId = setTimeout(() => {
                    try {
                        // Prüfen, ob dotNetHelper noch gültig ist, bevor invokeMethodAsync aufgerufen wird
                        // In JS ist das schwer direkt zu prüfen. C# sollte Fehler abfangen.
                        dotNetHelper.invokeMethodAsync('HandleScroll');
                    } catch (e) {
                        console.error("Error invoking dotNetHelper.HandleScroll: ", e);
                        // Evtl. Listener entfernen, wenn dotNetHelper ungültig scheint
                        // this.removeScrollListener(element); // Vorsicht mit Selbstentfernung hier
                    }
                }, 100);
            };

            element.addEventListener('scroll', debouncedHandler);
            // Speichere den handler und den dotNetHelper, um sie später korrekt zu entfernen
            this.scrollListeners.set(element, { handler: debouncedHandler, dotNetHelper: dotNetHelper });
        }
    },

    removeScrollListener: function (element) {
        if (element && this.scrollListeners.has(element)) {
            const listenerInfo = this.scrollListeners.get(element);
            element.removeEventListener('scroll', listenerInfo.handler);
            this.scrollListeners.delete(element);
            // console.log("Removed scroll listener for element:", element);
        }
    }
};