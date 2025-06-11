window.tourInterop = {
    dotNetHelper: null,
    driverObj: null,

    startTour: function (dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        if (this.driverObj && this.driverObj.isActive()) {
            this.driverObj.destroy();
        }

        const defaultOverlayOpacity = '0.75';

        this.driverObj = driver.js.driver({
            animate: true,
            allowClose: true,
            showProgress: true,
            nextBtnText: "Weiter →",
            prevBtnText: null,
            doneBtnText: "Fertig",
            steps: [
                { element: '.chessboard-container', popover: { title: 'Das Schachbrett', description: 'Das ist das Herzstück des Spiels. Dein Ziel ist es, den gegnerischen König Schachmatt zu setzen.' } },
                { element: '[data-coord-for-debug="e4"]', popover: { title: 'Dein Zug', description: 'Du bewegst deine Figuren wie im normalen Schach. Dein letzter Zug hat dir das Recht auf eine neue Karte eingebracht!' } },
                { element: '.hand-cards-container', popover: { title: 'Karten erhalten', description: 'Nach 5 Zügen erhältst du mächtige Karten auf deine Hand. Setze sie klug ein! Lass uns diese Karte gleich mal spielen.' } },
                { element: '.hand-cards-container', popover: { title: 'Karten-Aktivierung', description: 'Die Karte wird gespielt und ihre Animation zeigt den Effekt an. In diesem Fall: Du erhältst einen Extra-Zug!', side: 'bottom', align: 'start' }},
                { element: '[data-coord-for-debug="f3"]', popover: { title: 'Der Extra-Zug', description: 'Dank dieser Karte darfst du sofort einen weiteren Zug machen. Wir bewegen den Springer erneut!' } },
                { element: '[data-coord-for-debug="e5"]', popover: { title: 'Zug ausnutzen', description: 'Mit dem zweiten Zug können wir direkt den gegnerischen Bauern schlagen! Nutze die Karten weise!' } }
            ],

            onHighlightStarted: async (element, step, options) => {
                // Wenn der Schritt "Der Extra-Zug" beginnt, wird der Bildschirm
                // SOFORT manuell abgedunkelt.
                if (step.popover.title === 'Der Extra-Zug') {
                    const overlay = document.querySelector('.driver-overlay');
                    if (overlay) {
                        overlay.style.opacity = '0.75';
                    }
                }

                // ERST DANACH wird der C#-Code aufgerufen, der den Springer bewegt.
                if (window.tourInterop.dotNetHelper) {
                    await window.tourInterop.dotNetHelper.invokeMethodAsync('PrepareUiForTourStep', step.popover.title)
                        .catch(err => console.error("C# UI preparation failed: ", err));
                }
            },

            onHighlighted: (element, step, options) => {
                const popover = document.querySelector('.driver-popover');
                if (!popover) return;

                // ***** HIER IST DIE KORREKTUR *****
                const overlay = document.querySelector('.driver-overlay'); // NICHT .driver-page-overlay
                if (!overlay) return;

                if (step.popover.title === 'Karten-Aktivierung') {
                    overlay.style.opacity = '0';
                    popover.style.display = 'none';

                    setTimeout(() => {
                        if (window.tourInterop.driverObj && window.tourInterop.driverObj.isActive()) {
                            window.tourInterop.driverObj.moveNext();
                        }
                    }, 4000);

                } else {
                    overlay.style.opacity = defaultOverlayOpacity;
                    popover.style.display = 'block';
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