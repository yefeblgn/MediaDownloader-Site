(function () {
    'use strict';

    var WALL_ID = '__adb_wall';
    var INTERVAL = 600;

    function createBait() {
        var b = document.createElement('div');
        b.className = 'ad-banner ads adsbox doubleclick ad-placement pub_300x250 text-ad';
        b.style.cssText = 'width:1px!important;height:1px!important;position:absolute!important;left:-9999px!important;top:-9999px!important;';
        document.body.appendChild(b);
        return b;
    }

    function isBlocked(el) {
        if (!el || !el.parentNode) return true;
        var s = window.getComputedStyle(el);
        return (
            el.offsetHeight === 0 ||
            el.offsetWidth  === 0 ||
            el.offsetParent === null ||
            s.display    === 'none' ||
            s.visibility === 'hidden' ||
            s.opacity    === '0'
        );
    }

    function buildWall() {
        var d = document.createElement('div');
        d.id = WALL_ID;
        d.style.cssText = [
            'position:fixed', 'inset:0', 'z-index:2147483647',
            'background:rgba(14,14,18,0.98)',
            'display:flex', 'align-items:center', 'justify-content:center',
            'flex-direction:column', 'gap:1.25rem',
            'font-family:Inter,system-ui,sans-serif',
            'color:#f4f4f5', 'text-align:center', 'padding:2rem'
        ].join(';');
        d.innerHTML =
            '<svg width="56" height="56" viewBox="0 0 24 24" fill="none" stroke="#f43f5e" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">' +
                '<circle cx="12" cy="12" r="10"/><line x1="4.93" y1="4.93" x2="19.07" y2="19.07"/>' +
            '</svg>' +
            '<div style="max-width:420px">' +
                '<h2 style="font-size:1.4rem;font-weight:700;letter-spacing:-0.02em;margin:0 0 0.625rem">' +
                    'Reklam Engelleyici Tespit Edildi' +
                '</h2>' +
                '<p style="font-size:0.9375rem;color:#a1a1aa;line-height:1.65;margin:0">' +
                    'Bu site tamamen ücretsiz ve reklamlarla ayakta duruyor.<br>' +
                    'Lütfen <strong style="color:#f4f4f5">reklam engelleyicini devre dışı bırak</strong> ve sayfayı yenile.' +
                '</p>' +
            '</div>' +
            '<button onclick="location.reload()" ' +
                'style="padding:0.8rem 2.25rem;border-radius:8px;background:#f43f5e;color:#fff;' +
                'font-weight:600;font-size:0.9375rem;border:none;cursor:pointer;' +
                'transition:background .15s" ' +
                'onmouseover="this.style.background=\'#e11d48\'" onmouseout="this.style.background=\'#f43f5e\'">' +
                'Kapattım, Devam Et' +
            '</button>' +
            '<p style="font-size:0.75rem;color:#3f3f46;margin:0">tube.yefeblgn.net</p>';
        return d;
    }

    var _enforcing = false;

    function enforce() {
        if (!document.getElementById(WALL_ID)) {
            document.body.appendChild(buildWall());
        }
        document.documentElement.style.overflow = 'hidden';
        document.body.style.overflow = 'hidden';
    }

    function startEnforcement() {
        if (_enforcing) return;
        _enforcing = true;

        enforce();

        var obs = new MutationObserver(function () {
            if (!document.getElementById(WALL_ID)) enforce();
        });
        obs.observe(document.body, { childList: true });
        obs.observe(document.documentElement, { childList: true });

        setInterval(enforce, INTERVAL);
    }

    function check() {
        var bait = createBait();
        setTimeout(function () {
            var blocked = isBlocked(bait);
            try { bait.parentNode && bait.parentNode.removeChild(bait); } catch (e) {}
            if (blocked) startEnforcement();
        }, 250);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', check);
    } else {
        check();
    }
})();
