window.chessDnD = {
    interactables: {},
    // Dieses Flag wird im 'drop'-Event gesetzt, basierend auf der .highlight-Klasse des Ziels.
    _wasDropOnHighlightedSquare: false,
    _lastDropzoneElement: null,

    log: function (message, ...args) {
        // console.log(`[DnD JS] ${message}`, ...args);
    },

    // KEINE setDropOutcome oder setMoveValidity Funktion mehr nötig, da C# dies nicht mehr an JS meldet
    // für die sofortige visuelle Reaktion.

    initDraggable: function (element, pieceCoord, dotNetHelper) {
        this.log(`initDraggable START for ${pieceCoord}`, element);
        if (!element) {
            this.log(`initDraggable ABORTED for ${pieceCoord} - element is null`);
            return;
        }

        const dragKey = `drag-${pieceCoord}`;
        if (this.interactables[dragKey]) {
            try { this.interactables[dragKey].unset(); } catch (e) { this.log(`Error unsetting draggable ${dragKey}:`, e); }
            delete this.interactables[dragKey];
        }

        const interactable = interact(element)
            .draggable({
                inertia: false,
                autoScroll: true,
                listeners: {
                    start(event) {
                        const originalElement = event.target;
                        // Flags für aktuellen Drag zurücksetzen
                        window.chessDnD._wasDropOnHighlightedSquare = false;
                        window.chessDnD._lastDropzoneElement = null;

                        originalElement.classList.add('dragging-active-piece');
                        originalElement.setAttribute('data-drag-x', '0');
                        originalElement.setAttribute('data-drag-y', '0');
                        originalElement._initialRect = originalElement.getBoundingClientRect();
                        originalElement.style.zIndex = "1000";
                        try {
                            dotNetHelper.invokeMethodAsync('JsOnDragStart', pieceCoord);
                        } catch (e) {
                            console.error(`[DnD JS DRAG START ${pieceCoord}] ERROR Invoking JsOnDragStart`, e);
                        }
                    },
                    move(event) {
                        const originalElement = event.target;
                        let x = parseFloat(originalElement.getAttribute('data-drag-x')) || 0;
                        let y = parseFloat(originalElement.getAttribute('data-drag-y')) || 0;
                        x += event.dx;
                        y += event.dy;
                        originalElement.style.transform = `translate(${x}px, ${y}px) scale(1.15)`;
                        originalElement.setAttribute('data-drag-x', x);
                        originalElement.setAttribute('data-drag-y', y);
                    },
                    end(event) {
                        const originalElement = event.target;
                        const currentPieceCoord = originalElement.getAttribute('data-piece-coord-original');

                        originalElement.classList.remove('dragging-active-piece');
                        originalElement.style.zIndex = "10";

                        // KORREKTUR: Immer zur Ausgangsposition zurückkehren.
                        // Die finale Position wird bei einem ERFOLGREICHEN Zug durch das Blazor-Rendering bestimmt.
                        // Bei einem FEHLGESCHLAGENEN Zug ist dies die korrekte Endposition.
                        // Dies löst das Desynchronisationsproblem.
                        originalElement.style.transform = 'translate(0px, 0px) scale(1)';

                        originalElement.removeAttribute('data-drag-x');
                        originalElement.removeAttribute('data-drag-y');
                        if (originalElement._initialRect) delete originalElement._initialRect;

                        // Wir müssen dem .NET-Code immer noch mitteilen, ob der Drop auf einem (visuell) gültigen Feld war,
                        // damit der Move-Request ausgelöst werden kann. Die Logik hierfür bleibt gleich.
                        const droppedOnHighlightedSquare = window.chessDnD._wasDropOnHighlightedSquare;

                        if (currentPieceCoord) {
                            dotNetHelper.invokeMethodAsync('JsOnDragEnd', currentPieceCoord, droppedOnHighlightedSquare)
                                .then(() => console.log(`[DnD END ${currentPieceCoord}] .NET JsOnDragEnd call finished.`))
                                .catch(e => console.error(`[DnD END ${currentPieceCoord}] ERROR invoking .NET JsOnDragEnd`, e));
                        }

                        // Flags für den nächsten Drag zurücksetzen
                        window.chessDnD._wasDropOnHighlightedSquare = false;
                        window.chessDnD._lastDropzoneElement = null;
                    }
                }
            });
        this.interactables[dragKey] = interactable;
    },

    initDroppable: function (element, squareCoord, dotNetHelper) {
        this.log(`initDroppable START for ${squareCoord}`, element);
        if (!element) {
            this.log(`initDroppable ABORTED for ${squareCoord} - element is null`);
            return;
        }
        const dropKey = `drop-${squareCoord}`;
        if (this.interactables[dropKey]) {
            this.log(`initDroppable: Unsetting existing interactable for ${dropKey}`);
            try { this.interactables[dropKey].unset(); } catch (e) { this.log(`Error unsetting droppable ${dropKey}:`, e); }
            delete this.interactables[dropKey];
        }

        const interactable = interact(element).dropzone({
            accept: '.square img',
            overlap: 0.5,
            listeners: {
                dragenter(event) {
                    // Visuelles Feedback, wenn über ein Feld gezogen wird, das als legal markiert ist
                    if (event.target.classList.contains('highlight')) {
                        event.target.classList.add('drag-over-target-js');
                    }
                },
                dragleave(event) {
                    event.target.classList.remove('drag-over-target-js');
                },
                drop(event) {
                    const draggedElement = event.relatedTarget; // Das gezogene img-Element
                    const targetSquareElement = event.target;    // Das div-Quadrat, auf dem gedroppt wurde
                    const draggedPieceCoord = draggedElement.getAttribute('data-piece-coord-original');

                    targetSquareElement.classList.remove('drag-over-target-js');

                    // HIER IST DIE ENTSCHEIDENDE PRÜFUNG: Hat das Zielfeld die .highlight-Klasse?
                    window.chessDnD._wasDropOnHighlightedSquare = targetSquareElement.classList.contains('highlight');
                    window.chessDnD._lastDropzoneElement = targetSquareElement; // Für Zentrierung im 'end'-Event merken

                    console.log(`%c[DnD DROP on ${squareCoord}] Piece ${draggedPieceCoord}. Target has .highlight (JS check): ${window.chessDnD._wasDropOnHighlightedSquare}`, "color: orange; font-weight: bold;");

                    // Informiere C#, dass ein physischer Drop stattgefunden hat.
                    // Die C#-Logik (HandleSquareDrop) wird dann entscheiden, ob der Zug serverseitig verarbeitet wird.
                    // Die sofortige visuelle Reaktion (Snap/Revert) hängt NICHT vom Ergebnis dieses Aufrufs ab.
                    if (draggedPieceCoord) {
                        try {
                            dotNetHelper.invokeMethodAsync('JsOnDrop', draggedPieceCoord, squareCoord)
                                .then(() => {
                                    console.log(`[DnD DROP on ${squareCoord}] .NET JsOnDrop for ${draggedPieceCoord} finished processing.`);
                                })
                                .catch(e => {
                                    console.error(`[DnD DROP on ${squareCoord}] ERROR invoking .NET JsOnDrop for ${draggedPieceCoord}`, e);
                                });
                        } catch (e) {
                            console.error(`[DnD DROP on ${squareCoord}] IMMEDIATE ERROR invoking .NET JsOnDrop`, e);
                        }
                    } else {
                        console.warn(`[DnD DROP on ${squareCoord}] Drop event, but no draggedPieceCoord found.`);
                        // Wenn keine Koordinate da ist, kann es kein gültiger Drop auf ein Highlight sein
                        window.chessDnD._wasDropOnHighlightedSquare = false;
                    }
                }
            }
        });
        this.interactables[dropKey] = interactable;
        this.log(`initDroppable END for ${squareCoord}`);
    },

    // Die folgenden Funktionen bleiben wie in deiner Originaldatei oder meiner vorherigen Anpassung,
    // da sie für das grundlegende Management der Interactable-Instanzen zuständig sind.
    setPieceDraggableState: function (element, canDrag) {
        this.log(`setPieceDraggableState for element (coord: ${element ? element.getAttribute('data-piece-coord-original') : 'N/A'}): ${canDrag}`, element);
        if (!element) return;
        try {
            const interactableInstance = interact(element);
            if (interactableInstance && typeof interactableInstance.draggable === 'function') {
                interactableInstance.draggable(canDrag);
            } else {
                this.log(`setPieceDraggableState: No interactable instance or draggable method for element.`);
            }
        } catch (e) { this.log("Fehler setPieceDraggableState", e); }
    },

    setSquareDroppableVisualState: function (element, isHighlighted) {
        // Diese Funktion hat weniger Einfluss auf das 'drag-over-target-js',
        // da dies nun primär im 'dragenter' basierend auf '.highlight' gehandhabt wird.
        // Kann aber für andere visuelle Zustände nützlich sein.
        // this.log(`setSquareDroppableVisualState for ${element ? element.getAttribute('data-coord-for-debug') : 'N/A'}: ${isHighlighted}`, element);
    },

    disposeInteractable: function (element, coord, type) {
        this.log(`disposeInteractable START for type=${type}, coord=${coord}`, element);
        const key = `${type}-${coord}`;
        if (this.interactables[key]) {
            try {
                this.interactables[key].unset();
                this.log(`disposeInteractable: Unset ${key}`);
            } catch (e) { this.log(`Fehler beim this.interactables[${key}].unset():`, e); }
            delete this.interactables[key];
            this.log(`disposeInteractable: Deleted ${key} from tracking.`);
        } else {
            this.log(`disposeInteractable: Interactable ${key} not found for disposing.`);
        }
    }
};