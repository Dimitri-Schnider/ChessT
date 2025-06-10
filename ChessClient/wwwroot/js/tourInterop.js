window.tourInterop = {
    dotNetHelper: null,
    driverObj: null,

    startTour: function (dotNetHelper) {
        this.dotNetHelper = dotNetHelper;

        if (this.driverObj && this.driverObj.isActive()) {
            this.driverObj.destroy();
        }

        this.driverObj = driver.js.driver({
            animate: true,
            allowClose: true,
            showProgress: true,
            nextBtnText: "Weiter →",
            prevBtnText: "← Zurück",
            doneBtnText: "Fertig",
            steps: [
                { element: '.chessboard-container', popover: { title: 'Das Schachbrett', description: 'Das ist das Herzstück des Spiels. Dein Ziel ist es, den gegnerischen König Schachmatt zu setzen.' } },
                { element: '[data-coord-for-debug="e4"]', popover: { title: 'Dein Zug', description: 'Du bewegst deine Figuren wie im normalen Schach. Dieser Zug hat dir das Recht auf eine neue Karte eingebracht!' } },
                { element: '.hand-cards-container', popover: { title: 'Karten erhalten', description: 'Nach Zügen erhältst du mächtige Karten auf deine Hand. Setze sie klug ein! Lass uns diese Karte gleich mal spielen.' } },
                {
                    element: '.hand-cards-container',
                    popover: {
                        title: 'Karten-Aktivierung',
                        description: 'Die Karte wird gespielt und ihre Animation zeigt den Effekt an. In diesem Fall: Du erhältst einen Extra-Zug!',
                        side: 'bottom',
                        align: 'start'
                    }
                },
                { element: '[data-coord-for-debug="f3"]', popover: { title: 'Der Extra-Zug', description: 'Dank der Karte darfst du sofort einen weiteren Zug machen. Wir bewegen den Springer.' } },
                { element: '[data-coord-for-debug="e5"]', popover: { title: 'Zug ausnutzen', description: 'Mit dem zweiten Zug können wir direkt den gegnerischen Bauern schlagen! Die Tour ist nun zu Ende.' } }
            ],

            // KORREKTUR: Die Funktion wird 'async' und wartet mit 'await' auf C#.
            onHighlightStarted: async (element, step, options) => {
                if (this.dotNetHelper) {
                    await this.dotNetHelper.invokeMethodAsync('PrepareUiForTourStep', step.popover.title)
                        .catch(err => console.error("C# UI preparation failed: ", err));
                }
            },

            onDestroyed: () => {
                if (this.dotNetHelper) {
                    this.dotNetHelper.invokeMethodAsync('EndTutorial')
                        .catch(err => console.error("C# EndTutorial failed: ", err));
                }
            }
        });

        this.driverObj.drive();
    }
};