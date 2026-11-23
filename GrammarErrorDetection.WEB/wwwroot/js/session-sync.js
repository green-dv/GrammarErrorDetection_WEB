// Comparte la sesión entre pestañas del mismo navegador SIN guardarla en disco.
// sessionStorage es por pestaña: una pestaña nueva (enlace de invitación, "abrir en pestaña nueva") nacería sin sesión.
// Aquí, cada pestaña con sesión responde por BroadcastChannel (solo mismo origen, solo memoria) y la nueva la copia
// a su propio sessionStorage. El token nunca pasa por localStorage ni por cookies.
(function () {
    var KEY = 'gp.auth.session', CHANNEL = 'gp-session-sync';
    if (typeof BroadcastChannel === 'undefined') { window.gpSessionSync = { request: function () { return Promise.resolve(false); } }; return; }
    var channel = new BroadcastChannel(CHANNEL), pending = null;

    function read() { try { return sessionStorage.getItem(KEY); } catch (e) { return null; } }

    channel.onmessage = function (event) {
        var message = event.data || {};
        if (message.type === 'request') {
            var json = read();
            if (json) channel.postMessage({ type: 'response', json: json });
        } else if (message.type === 'response' && pending && typeof message.json === 'string') {
            pending(message.json);
        }
    };

    window.gpSessionSync = {
        // Devuelve true si copió una sesión de otra pestaña (el llamador debe volver a restaurarla).
        request: function (timeoutMs) {
            if (read()) return Promise.resolve(false);
            return new Promise(function (resolve) {
                var timer = setTimeout(function () { pending = null; resolve(false); }, timeoutMs || 300);
                pending = function (json) {
                    clearTimeout(timer);
                    pending = null;
                    try { sessionStorage.setItem(KEY, json); resolve(true); } catch (e) { resolve(false); }
                };
                channel.postMessage({ type: 'request' });
            });
        }
    };
})();
