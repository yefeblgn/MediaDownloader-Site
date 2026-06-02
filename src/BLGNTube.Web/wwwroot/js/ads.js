// BLGNTube — Monetag reklam tetikleyici
// Bul tuşu  → zone 11091992
// İndir tuşu → zone 11091994

window.ADS = (function () {
    var _last = {};
    var COOLDOWN = 30000; // aynı slot için 30 sn bekleme

    function canTrigger(slot) {
        var now = Date.now();
        if (_last[slot] && now - _last[slot] < COOLDOWN) return false;
        _last[slot] = now;
        return true;
    }

    function injectZone(zoneId) {
        var s = document.createElement('script');
        s.dataset.zone = String(zoneId);
        s.src = 'https://al5sm.com/tag.min.js?_=' + Date.now();
        (document.body || document.documentElement).appendChild(s);
    }

    return {
        trigger: function (slot) {
            if (!canTrigger(slot)) return;
            if (slot === 'fetch')    injectZone(11091992); // Bul
            if (slot === 'download') injectZone(11091994); // İndir
        }
    };
})();
