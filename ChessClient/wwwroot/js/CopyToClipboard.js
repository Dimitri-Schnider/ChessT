"use strict";

window.clipboardCopy = {
    copyText: function (text) {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text).then(function () {
                console.log("Link erfolgreich in die Zwischenablage kopiert! (aus CopyToClipboard.js)");
            }, function (err) {
                console.error("Fehler beim Kopieren des Links (aus CopyToClipboard.js): ", err);
                alert("Fehler beim Kopieren des Links."); // Fallback-Benachrichtigung
            });
        } else {
            console.error("Clipboard API nicht verfügbar. (aus CopyToClipboard.js)");
            alert("Automatisches Kopieren nicht unterstützt. Bitte manuell kopieren."); // Fallback-Benachrichtigung
        }
    }
};